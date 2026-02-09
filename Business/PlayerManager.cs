using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using GameServer.Network;
using GameServer.Infrastructure;
using SuperSocket.Server.Abstractions.Session;

namespace GameServer.Business;

/// <summary>
/// 玩家管理器
/// 管理所有在线玩家
/// </summary>
public class PlayerManager
{
    private readonly ILogger<PlayerManager> _logger;
    
    /// <summary>
    /// 在线玩家：PlayerId -> PlayerState
    /// </summary>
    private readonly ConcurrentDictionary<long, PlayerState> _onlinePlayers = new();
    
    /// <summary>
    /// 会话映射：SessionId -> PlayerId
    /// </summary>
    private readonly ConcurrentDictionary<string, long> _sessionToPlayer = new();

    public PlayerManager(ILogger<PlayerManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 玩家上线
    /// </summary>
    public void AddPlayer(PlayerState player)
    {
        if (_onlinePlayers.TryAdd(player.PlayerId, player))
        {
            _sessionToPlayer[player.Session.SessionID] = player.PlayerId;
            Metrics.OnlinePlayers.Inc();
            _logger.LogInformation("玩家上线: {PlayerId} {PlayerName}", player.PlayerId, player.Name);
        }
    }

    /// <summary>
    /// 玩家下线
    /// </summary>
    public PlayerState? RemovePlayer(long playerId)
    {
        if (_onlinePlayers.TryRemove(playerId, out var player))
        {
            _sessionToPlayer.TryRemove(player.Session.SessionID, out _);
            Metrics.OnlinePlayers.Dec();
            _logger.LogInformation("玩家下线: {PlayerId} {PlayerName}", playerId, player.Name);
            return player;
        }
        return null;
    }

    /// <summary>
    /// 根据会话ID移除玩家
    /// </summary>
    public PlayerState? RemovePlayerBySession(string sessionId)
    {
        if (_sessionToPlayer.TryRemove(sessionId, out var playerId))
        {
            return RemovePlayer(playerId);
        }
        return null;
    }

    /// <summary>
    /// 获取玩家
    /// </summary>
    public PlayerState? GetPlayer(long playerId)
    {
        _onlinePlayers.TryGetValue(playerId, out var player);
        return player;
    }

    /// <summary>
    /// 根据会话获取玩家
    /// </summary>
    public PlayerState? GetPlayerBySession(string sessionId)
    {
        if (_sessionToPlayer.TryGetValue(sessionId, out var playerId))
        {
            return GetPlayer(playerId);
        }
        return null;
    }

    /// <summary>
    /// 检查玩家是否在线
    /// </summary>
    public bool IsOnline(long playerId)
    {
        return _onlinePlayers.ContainsKey(playerId);
    }

    /// <summary>
    /// 获取在线人数
    /// </summary>
    public int GetOnlineCount()
    {
        return _onlinePlayers.Count;
    }

    /// <summary>
    /// 获取所有在线玩家
    /// </summary>
    public IEnumerable<PlayerState> GetAllPlayers()
    {
        return _onlinePlayers.Values;
    }

    /// <summary>
    /// 广播消息给所有在线玩家
    /// </summary>
    public async Task BroadcastAsync(ushort msgId, Google.Protobuf.IMessage message, Func<PlayerState, bool>? filter = null)
    {
        var tasks = new List<Task>();
        foreach (var player in _onlinePlayers.Values)
        {
            if (filter == null || filter(player))
            {
                tasks.Add(player.Session.SendMessageAsync(msgId, message).AsTask());
            }
        }
        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// 在线玩家状态
/// </summary>
public class PlayerState
{
    /// <summary>
    /// 玩家ID
    /// </summary>
    public long PlayerId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 玩家名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 等级
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 经验
    /// </summary>
    public long Exp { get; set; }

    /// <summary>
    /// 当前场景ID
    /// </summary>
    public int SceneId { get; set; }

    /// <summary>
    /// 位置
    /// </summary>
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }

    /// <summary>
    /// 当前房间ID（0表示不在房间）
    /// </summary>
    public int RoomId { get; set; }

    /// <summary>
    /// 网络会话
    /// </summary>
    public IAppSession Session { get; set; } = null!;

    /// <summary>
    /// 转换为 Protobuf 消息
    /// </summary>
    public Protocol.PlayerInfo ToProto()
    {
        return new Protocol.PlayerInfo
        {
            PlayerId = PlayerId,
            Name = Name,
            Level = Level,
            Exp = Exp,
            SceneId = SceneId,
            Position = new Protocol.Vector3
            {
                X = PosX,
                Y = PosY,
                Z = PosZ
            }
        };
    }
}
