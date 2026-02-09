using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GameServer.Data;

namespace GameServer.Infrastructure;

/// <summary>
/// 连接健康检查服务
/// 启动时测试 MySQL 和 Redis 连接
/// </summary>
public class ConnectionHealthCheck : IHostedService
{
    private readonly ILogger<ConnectionHealthCheck> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConnectionHealthCheck(
        ILogger<ConnectionHealthCheck> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("开始连接健康检查...");
        _logger.LogInformation("========================================");

        var allSuccess = true;

        // 测试 MySQL 连接
        allSuccess &= await CheckMySqlAsync();

        // 测试 Redis 连接
        allSuccess &= await CheckRedisAsync();

        _logger.LogInformation("========================================");
        if (allSuccess)
        {
            _logger.LogInformation("✅ 所有连接健康检查通过！");
        }
        else
        {
            _logger.LogWarning("⚠️ 部分连接健康检查失败，请查看上方日志");
        }
        _logger.LogInformation("========================================");
    }

    /// <summary>
    /// 测试 MySQL 连接
    /// </summary>
    private async Task<bool> CheckMySqlAsync()
    {
        try
        {
            _logger.LogInformation("测试 MySQL 连接...");

            // 创建 Scope 以获取 DbContext
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

            // 测试查询（检查数据库是否可达）
            var canConnect = await db.Database.CanConnectAsync();

            if (canConnect)
            {
                // 尝试查询用户表数量
                var userCount = await db.Users.CountAsync();
                _logger.LogInformation("✅ MySQL 连接成功！当前用户数: {Count}", userCount);
                return true;
            }
            else
            {
                _logger.LogError("❌ MySQL 连接失败：无法连接到数据库");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MySQL 连接测试异常");
            return false;
        }
    }

    /// <summary>
    /// 测试 Redis 连接
    /// </summary>
    private async Task<bool> CheckRedisAsync()
    {
        try
        {
            _logger.LogInformation("测试 Redis 连接...");

            var redis = _serviceProvider.GetRequiredService<RedisService>();

            // 测试写入
            var testKey = "health:check:startup";
            var testValue = $"test_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var writeSuccess = await redis.SetAsync(testKey, testValue, TimeSpan.FromSeconds(10));

            if (!writeSuccess)
            {
                _logger.LogError("❌ Redis 写入失败");
                return false;
            }

            // 测试读取
            var readValue = await redis.GetAsync(testKey);
            if (readValue != testValue)
            {
                _logger.LogError("❌ Redis 读取失败或数据不匹配");
                return false;
            }

            // 测试删除
            var deleteSuccess = await redis.DeleteAsync(testKey);

            _logger.LogInformation("✅ Redis 连接成功！写入/读取/删除测试通过");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Redis 连接测试异常");
            return false;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
