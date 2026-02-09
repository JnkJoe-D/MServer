# MServer Game Server

基于 .NET 8.0 + SuperSocket 2.0.2 的游戏服务器框架

## 技术栈

- **网络层**: SuperSocket 2.0.2
- **数据库**: MySQL 5.7 + EF Core
- **缓存**: Redis + StackExchange.Redis
- **序列化**: Protobuf
- **认证**: JWT
- **日志**: Serilog
- **监控**: Prometheus

## 快速开始

### 1. 环境要求

- .NET 8.0 SDK
- MySQL 5.7+
- Redis 6.0+

### 2. 数据库初始化

```bash
# 双击运行（Windows）
sql/init.bat

# 或使用命令行
mysql -h 127.0.0.1 -P 23333 -u GameServer -p < sql/init.sql
```

### 3. 配置文件

编辑 `appsettings.json`：

```json
{
  "Server": {
    "Port": 33333
  },
  "Database": {
    "ConnectionString": "Server=127.0.0.1;Port=23333;Database=gameserver;..."
  },
  "Redis": {
    "ConnectionString": "127.0.0.1:6379",
    "Password": "your-password"
  }
}
```

### 4. 运行

```bash
# 开发环境
dotnet run

# 生产环境
ASPNETCORE_ENVIRONMENT=Production ./GameServer
```

## 功能特性

- ✅ TCP 长连接通信
- ✅ Protobuf 序列化
- ✅ 用户注册/登录
- ✅ 角色创建/列表
- ✅ MySQL 持久化
- ✅ Redis 缓存
- ✅ JWT 认证
- ✅ 连接健康检查
- ✅ Prometheus 监控

## 文档

- [框架设计与学习指南](docs/框架设计与学习指南.md)
- [部署指南](docs/部署指南.md)
- [配置管理最佳实践](docs/配置管理最佳实践.md)
- [数据库初始化指南](docs/数据库初始化指南.md)
- [Git使用指南](docs/Git使用指南.md)

## 项目结构

```
MServer/
├── Business/              # 业务逻辑层
├── Data/                  # 数据访问层
├── Handlers/              # 消息处理器
├── Infrastructure/        # 基础设施
├── Network/               # 网络层
├── Protos/                # Protobuf 定义
├── sql/                   # SQL 脚本
└── docs/                  # 文档
```

## 测试

启动后运行连接健康检查，验证 MySQL 和 Redis 连接。

测试用户：
- 用户名: `test`
- 密码: `12345678`

## License

MIT
