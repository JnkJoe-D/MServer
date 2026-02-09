# 游戏服务器框架开发记录

## 2026-02-07: 基础框架搭建完成

### 概述

成功搭建了游戏服务器框架，完整实现了网络层、协议层、数据层、业务层和处理器层。

### 项目结构

```
D:\DesKtop\Project\SuperSocket\
├── Network/                    # 网络层
│   ├── TcpServer.cs           # TCP 服务器
│   ├── GameSession.cs         # 客户端会话管理
│   ├── GamePackage.cs         # 协议包定义
│   ├── MsgId.cs               # 消息 ID 定义
│   ├── MessageHandler.cs      # 消息处理器基类
│   └── MessageGateway.cs      # 消息路由网关
├── Protos/                    # 协议定义
│   ├── common.proto           # 通用消息
│   ├── world.proto            # 世界/场景消息
│   └── room.proto             # 房间/战斗消息
├── Data/                      # 数据层
│   ├── GameDbContext.cs       # EF Core 数据库上下文
│   ├── Entities/Entities.cs   # 数据库实体
│   └── RedisService.cs        # Redis 服务封装
├── Business/                  # 业务层
│   ├── PlayerManager.cs       # 在线玩家管理
│   ├── AuthService.cs         # 认证服务
│   └── JwtService.cs          # JWT 令牌服务
├── Handlers/                  # 消息处理器
│   └── AuthHandlers.cs        # 认证相关处理器
├── Infrastructure/            # 基础设施
│   └── Metrics.cs             # Prometheus 监控指标
├── sql/                       # 数据库脚本
│   └── init.sql               # 初始化脚本
├── Program.cs                 # 程序入口
└── appsettings.json           # 配置文件
```

### 核心技术栈

| 组件 | 技术 |
|------|------|
| 框架 | .NET 8.0 |
| 协议 | Protobuf 3.27 |
| 数据库 | MySQL (Pomelo EF Core) |
| 缓存 | Redis (StackExchange.Redis) |
| 日志 | Serilog |
| 监控 | prometheus-net |
| 认证 | JWT + BCrypt |

---

## 2026-02-07: 使用 SuperSocket 2.0.2 重建网络层

### 变更原因

用户更新 SuperSocket 为 2.0.2 统一包，要求重新使用 SuperSocket 构建网络层。

### 核心改动

| 文件 | 变更 |
|------|------|
| `GameSession.cs` | 改为 `GameSessionData` + `IAppSession` 扩展方法模式 |
| `GamePackage.cs` | 使用 `FixedHeaderPipelineFilter` 处理粘包/拆包 |
| `MessageGateway.cs` | 简化为直接使用 `IAppSession` |
| `MessageHandler.cs` | 更新为使用 `IAppSession` |
| `AuthHandlers.cs` | 使用 `session.GetGameData()` 访问业务数据 |
| `PlayerManager.cs` | `PlayerState.Session` 改为 `IAppSession` |
| `Program.cs` | 使用 `SuperSocketHostBuilder` 配置服务器 |

### 编译结果

```
✅ 编译成功
   0 个警告
   0 个错误
   输出: D:\DesKtop\Project\SuperSocket\bin\Debug\net8.0\GameServer.dll
```

### SuperSocket 2.0.2 关键 API

```csharp
// 创建服务器
var hostBuilder = SuperSocketHostBuilder
    .Create<GamePackage, GamePackageFilter>();

// 包处理
hostBuilder.UsePackageHandler(async (session, package) => { ... });

// 会话事件
hostBuilder.UseSessionHandler(
    onConnected: session => { ... },
    onClosed: (session, reason) => { ... });

// 服务器配置
hostBuilder.ConfigureSuperSocket(options => { ... });

// 会话扩展数据
var gameData = session.GetGameData();  // 存储在 DataContext
await session.SendMessageAsync(msgId, message);  // 扩展方法
```

### 技术说明

SuperSocket 2.0.2 使用 `IAppSession.DataContext` 存储业务数据，通过扩展方法提供便捷的 API。这种模式比继承 `AppSession` 更灵活。

---
