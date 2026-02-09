using Microsoft.Extensions.Logging;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using GameServer.Data;
using GameServer.Data.Entities;

namespace GameServer.Business;

/// <summary>
/// 认证服务
/// </summary>
public class AuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly GameDbContext _db;
    private readonly RedisService _redis;
    private readonly JwtService _jwt;

    public AuthService(
        ILogger<AuthService> logger,
        GameDbContext db,
        RedisService redis,
        JwtService jwt)
    {
        _logger = logger;
        _db = db;
        _redis = redis;
        _jwt = jwt;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    public async Task<(bool success, string message)> RegisterAsync(string username, string password, string email)
    {
        // 验证用户名
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 20)
        {
            return (false, "用户名长度需要3-20个字符");
        }

        // 验证密码
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return (false, "密码长度至少8个字符");
        }

        // 检查用户名是否存在
        var exists = await _db.Users.AnyAsync(u => u.Username == username);
        if (exists)
        {
            return (false, "用户名已存在");
        }

        // 检查邮箱是否存在
        if (!string.IsNullOrEmpty(email))
        {
            var emailExists = await _db.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                return (false, "邮箱已被注册");
            }
        }

        // 创建用户
        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("用户注册成功: {Username}", username);
        return (true, "注册成功");
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<(bool success, string message, string? token, User? user)> LoginAsync(
        string username, string password)
    {
        // 查找用户
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return (false, "用户名或密码错误", null, null);
        }

        // 检查封禁
        if (user.IsBanned)
        {
            if (user.BannedUntil == null || user.BannedUntil > DateTime.UtcNow)
            {
                return (false, "账号已被封禁", null, null);
            }
            else
            {
                // 封禁已过期，自动解封
                user.IsBanned = false;
                user.BannedUntil = null;
            }
        }

        // 验证密码
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return (false, "用户名或密码错误", null, null);
        }

        // 生成 Token
        var token = _jwt.GenerateToken(user.Id, user.Username);

        // 更新登录时间
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // 缓存 Session
        await _redis.SetAsync($"session:token:{token}", user.Id.ToString(), TimeSpan.FromDays(7));

        _logger.LogInformation("用户登录成功: {Username}", username);
        return (true, "登录成功", token, user);
    }

    /// <summary>
    /// 验证 Token
    /// </summary>
    public async Task<(bool valid, long userId)> ValidateTokenAsync(string token)
    {
        // 先从 Redis 查询
        var cachedUserId = await _redis.GetAsync($"session:token:{token}");
        if (!string.IsNullOrEmpty(cachedUserId) && long.TryParse(cachedUserId, out var userId))
        {
            return (true, userId);
        }

        // Redis 没有，验证 JWT
        var (valid, claims) = _jwt.ValidateToken(token);
        if (!valid)
        {
            return (false, 0);
        }

        // 刷新 Redis 缓存
        if (claims.TryGetValue("sub", out var sub) && long.TryParse(sub, out userId))
        {
            await _redis.SetAsync($"session:token:{token}", userId.ToString(), TimeSpan.FromDays(7));
            return (true, userId);
        }

        return (false, 0);
    }

    /// <summary>
    /// 登出
    /// </summary>
    public async Task LogoutAsync(string token)
    {
        await _redis.DeleteAsync($"session:token:{token}");
    }
}
