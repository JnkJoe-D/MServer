using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameServer.Data;

/// <summary>
/// Redis 服务
/// </summary>
public class RedisService : IDisposable
{
    private readonly ILogger<RedisService> _logger;
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService(ILogger<RedisService> logger, string connectionString, int database = 0, string? password = null)
    {
        _logger = logger;
        
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 3;
        options.ConnectTimeout = 5000;
        
        // 设置密码
        if (!string.IsNullOrEmpty(password))
        {
            options.Password = password;
        }
        
        _redis = ConnectionMultiplexer.Connect(options);
        _db = _redis.GetDatabase(database);
        
        _logger.LogInformation("Redis连接成功: {Endpoint}, Database={Database}", connectionString, database);
    }

    // ========== 基础操作 ==========

    /// <summary>
    /// 设置字符串
    /// </summary>
    public async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        return await _db.StringSetAsync(key, value, expiry);
    }

    /// <summary>
    /// 获取字符串
    /// </summary>
    public async Task<string?> GetAsync(string key)
    {
        return await _db.StringGetAsync(key);
    }

    /// <summary>
    /// 删除键
    /// </summary>
    public async Task<bool> DeleteAsync(string key)
    {
        return await _db.KeyDeleteAsync(key);
    }

    /// <summary>
    /// 键是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    /// <summary>
    /// 设置过期时间
    /// </summary>
    public async Task<bool> ExpireAsync(string key, TimeSpan expiry)
    {
        return await _db.KeyExpireAsync(key, expiry);
    }

    // ========== JSON 操作 ==========

    /// <summary>
    /// 设置对象（JSON序列化）
    /// </summary>
    public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        return await SetAsync(key, json, expiry);
    }

    /// <summary>
    /// 获取对象（JSON反序列化）
    /// </summary>
    public async Task<T?> GetObjectAsync<T>(string key)
    {
        var json = await GetAsync(key);
        if (string.IsNullOrEmpty(json))
            return default;
        return JsonSerializer.Deserialize<T>(json);
    }

    // ========== Hash 操作 ==========

    /// <summary>
    /// 设置Hash字段
    /// </summary>
    public async Task<bool> HashSetAsync(string key, string field, string value)
    {
        return await _db.HashSetAsync(key, field, value);
    }

    /// <summary>
    /// 获取Hash字段
    /// </summary>
    public async Task<string?> HashGetAsync(string key, string field)
    {
        return await _db.HashGetAsync(key, field);
    }

    /// <summary>
    /// 删除Hash字段
    /// </summary>
    public async Task<bool> HashDeleteAsync(string key, string field)
    {
        return await _db.HashDeleteAsync(key, field);
    }

    /// <summary>
    /// 获取Hash所有字段
    /// </summary>
    public async Task<Dictionary<string, string>> HashGetAllAsync(string key)
    {
        var entries = await _db.HashGetAllAsync(key);
        return entries.ToDictionary(
            e => e.Name.ToString(),
            e => e.Value.ToString()
        );
    }

    /// <summary>
    /// 获取Hash长度
    /// </summary>
    public async Task<long> HashLengthAsync(string key)
    {
        return await _db.HashLengthAsync(key);
    }

    // ========== Sorted Set 操作（排行榜）==========

    /// <summary>
    /// 添加/更新排行榜分数
    /// </summary>
    public async Task<bool> SortedSetAddAsync(string key, string member, double score)
    {
        return await _db.SortedSetAddAsync(key, member, score);
    }

    /// <summary>
    /// 获取排行榜（降序）
    /// </summary>
    public async Task<List<(string member, double score)>> SortedSetRangeByRankAsync(
        string key, long start = 0, long stop = -1, bool descending = true)
    {
        var entries = await _db.SortedSetRangeByRankWithScoresAsync(
            key, start, stop, descending ? Order.Descending : Order.Ascending);
        return entries.Select(e => (e.Element.ToString(), e.Score)).ToList();
    }

    /// <summary>
    /// 获取成员排名（降序从0开始）
    /// </summary>
    public async Task<long?> SortedSetRankAsync(string key, string member, bool descending = true)
    {
        return await _db.SortedSetRankAsync(key, member, 
            descending ? Order.Descending : Order.Ascending);
    }

    /// <summary>
    /// 获取成员分数
    /// </summary>
    public async Task<double?> SortedSetScoreAsync(string key, string member)
    {
        return await _db.SortedSetScoreAsync(key, member);
    }

    // ========== List 操作（队列/聊天）==========

    /// <summary>
    /// 左侧推入
    /// </summary>
    public async Task<long> ListLeftPushAsync(string key, string value)
    {
        return await _db.ListLeftPushAsync(key, value);
    }

    /// <summary>
    /// 右侧弹出
    /// </summary>
    public async Task<string?> ListRightPopAsync(string key)
    {
        return await _db.ListRightPopAsync(key);
    }

    /// <summary>
    /// 获取列表范围
    /// </summary>
    public async Task<List<string>> ListRangeAsync(string key, long start = 0, long stop = -1)
    {
        var values = await _db.ListRangeAsync(key, start, stop);
        return values.Select(v => v.ToString()).ToList();
    }

    /// <summary>
    /// 裁剪列表
    /// </summary>
    public async Task ListTrimAsync(string key, long start, long stop)
    {
        await _db.ListTrimAsync(key, start, stop);
    }

    // ========== 分布式锁 ==========

    /// <summary>
    /// 尝试获取锁
    /// </summary>
    public async Task<bool> TryAcquireLockAsync(string key, string value, TimeSpan expiry)
    {
        return await _db.StringSetAsync(key, value, expiry, When.NotExists);
    }

    /// <summary>
    /// 释放锁
    /// </summary>
    public async Task<bool> ReleaseLockAsync(string key, string value)
    {
        var script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";
        var result = await _db.ScriptEvaluateAsync(script, 
            new RedisKey[] { key }, new RedisValue[] { value });
        return (int)result == 1;
    }

    public void Dispose()
    {
        _redis.Dispose();
    }
}
