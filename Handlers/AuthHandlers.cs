using Microsoft.Extensions.Logging;
using GameServer.Network;
using GameServer.Data;
using GameServer.Protocol;
using Microsoft.EntityFrameworkCore;
using SuperSocket.Server.Abstractions.Session;

namespace GameServer.Handlers;

/// <summary>
/// 登录消息处理器
/// </summary>
public class LoginHandler : MessageHandler<C2S_Login>
{
    private readonly ILogger<LoginHandler> _logger;
    private readonly Business.AuthService _authService;
    private readonly GameDbContext _db;
    private readonly Business.PlayerManager _playerManager;

    public LoginHandler(
        ILogger<LoginHandler> logger,
        Business.AuthService authService,
        GameDbContext db,
        Business.PlayerManager playerManager)
    {
        _logger = logger;
        _authService = authService;
        _db = db;
        _playerManager = playerManager;
    }

    protected override async Task HandleAsync(IAppSession session, C2S_Login request)
    {
        _logger.LogInformation("登录请求: Username={Username}", request.Username);

        var gameData = session.GetGameData();

        // 调用认证服务
        var (success, message, token, user) = await _authService.LoginAsync(
            request.Username, request.Password);

        if (!success || user == null || token == null)
        {
            await session.SendMessageAsync(MsgId.Login, new S2C_Login
            {
                Code = (int)ErrorCode.LoginFailed,
                Message = message
            });
            Infrastructure.Metrics.LoginCount.WithLabels("failed").Inc();
            return;
        }

        // 标记会话已认证
        gameData.IsAuthenticated = true;
        gameData.UserId = user.Id;
        gameData.Token = token;

        // 查询角色列表（返回第一个角色，或者null）
        var player = await _db.Players
            .Where(p => p.UserId == user.Id && !p.IsDeleted)
            .FirstOrDefaultAsync();

        PlayerInfo? playerInfo = null;
        if (player != null)
        {
            gameData.PlayerId = player.Id;
            gameData.PlayerName = player.Name;
            gameData.CurrentSceneId = player.SceneId;

            playerInfo = new PlayerInfo
            {
                PlayerId = player.Id,
                Name = player.Name,
                Level = player.Level,
                Exp = player.Exp,
                SceneId = player.SceneId,
                Position = new Vector3
                {
                    X = player.PosX,
                    Y = player.PosY,
                    Z = player.PosZ
                }
            };

            // 添加到在线玩家
            _playerManager.AddPlayer(new Business.PlayerState
            {
                PlayerId = player.Id,
                UserId = user.Id,
                Name = player.Name,
                Level = player.Level,
                Exp = player.Exp,
                SceneId = player.SceneId,
                PosX = player.PosX,
                PosY = player.PosY,
                PosZ = player.PosZ,
                Session = session
            });
        }

        await session.SendMessageAsync(MsgId.Login, new S2C_Login
        {
            Code = (int)ErrorCode.Success,
            Message = "登录成功",
            Token = token,
            Player = playerInfo
        });

        Infrastructure.Metrics.LoginCount.WithLabels("success").Inc();
        _logger.LogInformation("登录成功: UserId={UserId}, PlayerId={PlayerId}", 
            user.Id, player?.Id ?? 0);
    }
}

/// <summary>
/// 注册消息处理器
/// </summary>
public class RegisterHandler : MessageHandler<C2S_Register>
{
    private readonly ILogger<RegisterHandler> _logger;
    private readonly Business.AuthService _authService;

    public RegisterHandler(ILogger<RegisterHandler> logger, Business.AuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    protected override async Task HandleAsync(IAppSession session, C2S_Register request)
    {
        _logger.LogInformation("注册请求: Username={Username}", request.Username);

        var (success, message) = await _authService.RegisterAsync(
            request.Username, request.Password, request.Email);

        await session.SendMessageAsync(MsgId.Register, new S2C_Register
        {
            Code = success ? (int)ErrorCode.Success : (int)ErrorCode.RegisterFailed,
            Message = message
        });
    }
}

/// <summary>
/// 创建角色处理器
/// </summary>
public class CreatePlayerHandler : AuthenticatedMessageHandler<C2S_CreatePlayer>
{
    private readonly ILogger<CreatePlayerHandler> _logger;
    private readonly GameDbContext _db;

    public CreatePlayerHandler(ILogger<CreatePlayerHandler> logger, GameDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    protected override async Task HandleAuthenticatedAsync(IAppSession session, C2S_CreatePlayer request)
    {
        var gameData = session.GetGameData();
        _logger.LogInformation("创建角色请求: UserId={UserId}, Name={Name}", gameData.UserId, request.Name);

        // 验证角色名
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2 || request.Name.Length > 12)
        {
            await session.SendMessageAsync(MsgId.CreatePlayer, new S2C_CreatePlayer
            {
                Code = (int)ErrorCode.InvalidParams,
                Message = "角色名长度需要2-12个字符"
            });
            return;
        }

        // 检查角色名是否存在
        var exists = await _db.Players.AnyAsync(p => p.Name == request.Name);
        if (exists)
        {
            await session.SendMessageAsync(MsgId.CreatePlayer, new S2C_CreatePlayer
            {
                Code = (int)ErrorCode.UsernameExists,
                Message = "角色名已存在"
            });
            return;
        }

        // 检查角色数量（每个账号最多3个）
        var count = await _db.Players.CountAsync(p => p.UserId == gameData.UserId && !p.IsDeleted);
        if (count >= 3)
        {
            await session.SendMessageAsync(MsgId.CreatePlayer, new S2C_CreatePlayer
            {
                Code = (int)ErrorCode.InvalidParams,
                Message = "每个账号最多拥有3个角色"
            });
            return;
        }

        // 创建角色
        var player = new Data.Entities.Player
        {
            UserId = gameData.UserId,
            Name = request.Name,
            Level = 1,
            Exp = 0,
            SceneId = 1,  // 出生场景
            PosX = 0,
            PosY = 0,
            PosZ = 0,
            CreatedAt = DateTime.UtcNow
        };

        _db.Players.Add(player);
        await _db.SaveChangesAsync();

        await session.SendMessageAsync(MsgId.CreatePlayer, new S2C_CreatePlayer
        {
            Code = (int)ErrorCode.Success,
            Message = "创建成功",
            Player = new PlayerInfo
            {
                PlayerId = player.Id,
                Name = player.Name,
                Level = player.Level,
                Exp = player.Exp,
                SceneId = player.SceneId,
                Position = new Vector3 { X = 0, Y = 0, Z = 0 }
            }
        });

        _logger.LogInformation("角色创建成功: PlayerId={PlayerId}, Name={Name}", player.Id, player.Name);
    }
}

/// <summary>
/// 获取角色列表处理器
/// </summary>
public class GetPlayerListHandler : AuthenticatedMessageHandler<C2S_GetPlayerList>
{
    private readonly GameDbContext _db;

    public GetPlayerListHandler(GameDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleAuthenticatedAsync(IAppSession session, C2S_GetPlayerList request)
    {
        var gameData = session.GetGameData();
        
        var players = await _db.Players
            .Where(p => p.UserId == gameData.UserId && !p.IsDeleted)
            .Select(p => new PlayerInfo
            {
                PlayerId = p.Id,
                Name = p.Name,
                Level = p.Level,
                Exp = p.Exp,
                SceneId = p.SceneId,
                Position = new Vector3 { X = p.PosX, Y = p.PosY, Z = p.PosZ }
            })
            .ToListAsync();

        var response = new S2C_GetPlayerList { Code = (int)ErrorCode.Success };
        response.Players.AddRange(players);

        await session.SendMessageAsync(MsgId.GetPlayerList, response);
    }
}
