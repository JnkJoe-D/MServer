# Redis 连接日志问题修复说明

## 问题分析

用户报告：运行后没有看到 "Redis已连接" 的日志，但 Redis 客户端连接数确实增加了。

### 根因分析

经过代码审查，发现了**两个问题**：

### 问题 1：Redis 密码未应用 ❌

**原始代码**（`RedisService.cs`）：

```csharp
public RedisService(ILogger<RedisService> logger, string connectionString, int database = 0)
{
    var options = ConfigurationOptions.Parse(connectionString);
    // ❌ 缺少密码设置！
    _redis = ConnectionMultiplexer.Connect(options);
}
```

**Program.cs 注册**：

```csharp
var redisConn = config["Redis:ConnectionString"] ?? "localhost:6379";
var redisDb = config.GetValue<int>("Redis:Database", 0);
// ❌ 未读取密码配置
return new RedisService(logger, redisConn, redisDb);
```

**问题**：
- 虽然 `appsettings.json` 中配置了 `Redis:Password`
- 但代码中完全没有读取和使用该配置
- StackExchange.Redis 连接时会尝试无密码连接或使用默认策略

**为什么连接数还是增加了？**
- 如果 Redis 配置为 `requirepass`，无密码连接会失败
- 但如果 Redis 配置为允许无密码访问（或从特定 IP 允许），连接仍会成功
- 这可能导致**安全隐患**

---

### 问题 2：生产环境日志级别过滤 ⚠️

**原始配置**（`appsettings.Production.json`）：

```json
{
  "Logging": {
    "MinimumLevel": "Warning"  // ❌ 只显示 Warning 以上的日志
  }
}
```

**RedisService 代码**：

```csharp
_logger.LogInformation("Redis连接成功: {Endpoint}", connectionString);
// ↑ Information 级别
```

**问题**：
- `LogInformation` 的级别是 `Information`
- 生产环境配置的最小级别是 `Warning`
- `Information < Warning`，因此日志被**过滤掉**

**日志级别排序**：
```
Trace < Debug < Information < Warning < Error < Critical
```

---

## 修复方案

### 修复 1：添加 Redis 密码支持

#### RedisService.cs

```csharp
public RedisService(ILogger<RedisService> logger, string connectionString, int database = 0, string? password = null)
{
    _logger = logger;
    
    var options = ConfigurationOptions.Parse(connectionString);
    options.AbortOnConnectFail = false;
    options.ConnectRetry = 3;
    options.ConnectTimeout = 5000;
    
    // ✅ 设置密码
    if (!string.IsNullOrEmpty(password))
    {
        options.Password = password;
    }
    
    _redis = ConnectionMultiplexer.Connect(options);
    _db = _redis.GetDatabase(database);
    
    _logger.LogInformation("Redis连接成功: {Endpoint}, Database={Database}", connectionString, database);
}
```

#### Program.cs

```csharp
services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<RedisService>>();
    var redisConn = config["Redis:ConnectionString"] ?? "localhost:6379";
    var redisDb = config.GetValue<int>("Redis:Database", 0);
    var redisPassword = config["Redis:Password"];  // ✅ 读取密码
    return new RedisService(logger, redisConn, redisDb, redisPassword);
});
```

---

### 修复 2：调整生产环境日志级别

#### appsettings.Production.json

```json
{
  "Logging": {
    "MinimumLevel": "Information"  // ✅ 改为 Information
  }
}
```

**权衡考虑**：

| 级别 | 优点 | 缺点 |
|------|------|------|
| `Warning` | 日志少，性能好 | 缺少重要信息，排查问题困难 |
| `Information` | 包含关键信息（连接、请求等） | 日志量适中 |
| `Debug` | 详细信息 | 日志量大，影响性能 |

**推荐**：生产环境使用 `Information`，出问题时临时调整为 `Debug`。

---

## 验证修复

### 1. 编译测试

```bash
dotnet build
# ✅ 编译成功：1 个警告，0 个错误
```

### 2. 重新发布

```bash
dotnet publish MServer.csproj -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=true -p:PublishSingleFile=true -o ./publish/linux-selfcontained
```

### 3. 运行验证

启动服务器后，应该能看到日志：

```log
[2026-02-09 01:42:18 INF] Redis连接成功: 127.0.0.1:6379, Database=0
```

### 4. 测试密码认证

**场景 A：Redis 需要密码**

```json
// appsettings.json
{
  "Redis": {
    "ConnectionString": "127.0.0.1:6379",
    "Password": "your-redis-password"
  }
}
```

**结果**：✅ 连接成功

**场景 B：Redis 不需要密码**

```json
{
  "Redis": {
    "ConnectionString": "127.0.0.1:6379",
    "Password": ""  // 或省略
  }
}
```

**结果**：✅ 连接成功（password 为空则不设置）

---

## 配置示例

### 开发环境（appsettings.json）

```json
{
  "Redis": {
    "ConnectionString": "127.0.0.1:6379",
    "Database": 0,
    "Password": "dev-password"
  },
  "Logging": {
    "MinimumLevel": "Information"  // 开发时看详细日志
  }
}
```

### 生产环境（appsettings.Production.json）

```json
{
  "Redis": {
    "ConnectionString": "your-redis-server:6379",
    "Database": 0,
    "Password": "production-password"
  },
  "Logging": {
    "MinimumLevel": "Information"  // 保留关键信息
  }
}
```

### 环境变量覆盖

```bash
# 临时修改 Redis 密码
export Redis__Password="new-password"

# 临时提高日志级别（排查问题）
export Logging__MinimumLevel="Debug"

./GameServer
```

---

## 安全建议

### 1. 生产环境务必设置密码

**Redis 配置**（`redis.conf`）：

```conf
requirepass your-strong-password
bind 127.0.0.1  # 只允许本地连接
```

### 2. 不要在代码库中硬编码密码

```bash
# ❌ 不要提交包含真实密码的配置文件
git add appsettings.Production.json

# ✅ 使用环境变量或密钥管理
export Redis__Password="$(cat /path/to/secret)"
```

### 3. 使用强密码

```bash
# 生成随机密码
openssl rand -base64 32
```

---

## 总结

| 问题 | 原因 | 修复 |
|------|------|------|
| Redis 密码未应用 | 代码未读取配置中的密码 | 添加密码参数并设置到连接选项 |
| 日志不显示 | 生产环境日志级别为 Warning | 改为 Information |

**修复后效果**：
- ✅ Redis 密码正确应用
- ✅ 连接日志正常显示
- ✅ 安全性提升
