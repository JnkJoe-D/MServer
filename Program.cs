using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SuperSocket;
using SuperSocket.Server;
using SuperSocket.Server.Host;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Abstractions.Host;
using GameServer.Network;
using GameServer.Data;
using GameServer.Business;
using GameServer.Handlers;
using GameServer.Infrastructure;

namespace GameServer;

class Program
{
    static async Task Main(string[] args)
    {
        // 配置 Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Async(a => a.File(
                path: "logs/server-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
            .CreateLogger();

        try
        {
            Log.Information("========================================");
            Log.Information("游戏服务器启动中...");
            Log.Information("========================================");

            var hostBuilder = SuperSocketHostBuilder
                .Create<GamePackage, GamePackageFilter>();
            
            hostBuilder.UseSerilog();
            
            hostBuilder.ConfigureAppConfiguration((hostCtx, configApp) =>
            {
                configApp
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{hostCtx.HostingEnvironment.EnvironmentName}.json", optional: true)
                    .AddEnvironmentVariables();
            });

            // ⭐ 构建临时配置对象，用于后续读取服务器配置
            var tempProvider = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();
            
            hostBuilder.ConfigureServices((hostCtx, services) =>
            {
                var config = hostCtx.Configuration;

                // 数据库
                var connectionString = config["Database:ConnectionString"];
                services.AddDbContext<GameDbContext>(options =>
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

                // Redis
                services.AddSingleton(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<RedisService>>();
                    var redisConn = config["Redis:ConnectionString"] ?? "localhost:6379";
                    var redisDb = config.GetValue<int>("Redis:Database", 0);
                    var redisPassword = config["Redis:Password"]; // 读取密码
                    return new RedisService(logger, redisConn, redisDb, redisPassword);
                });

                // 业务服务
                services.AddSingleton<JwtService>();
                services.AddScoped<AuthService>();
                services.AddSingleton<PlayerManager>();

                // 消息网关（Singleton）
                services.AddSingleton<MessageGateway>();

                // ⭐ 消息处理器（Scoped）
                // 注意：这里注册为 Scoped，确保每次消息处理时都有独立的实例
                // 这样可以保证 DbContext 等 Scoped 服务的生命周期正确
                services.AddScoped<LoginHandler>();
                services.AddScoped<RegisterHandler>();
                services.AddScoped<CreatePlayerHandler>();
                services.AddScoped<GetPlayerListHandler>();

                // 注册处理器到网关
                services.AddHostedService<MessageHandlerRegistration>();
                
                // ⭐ 连接健康检查（启动时测试 MySQL 和 Redis）
                services.AddHostedService<ConnectionHealthCheck>();
            });

            // 包处理
            hostBuilder.UsePackageHandler(async (session, package) =>
            {
                var gateway = session.Server.ServiceProvider.GetRequiredService<MessageGateway>();
                await gateway.RouteMessageAsync(session, package);
            });
            
            // 会话事件
            hostBuilder.UseSessionHandler(
                onConnected: session =>
                {
                    Metrics.ActiveConnections.Inc();
                    Metrics.ConnectionsTotal.WithLabels("connect").Inc();
                    Log.Information("客户端连接: {SessionId} {RemoteEndPoint}",
                        session.SessionID, session.RemoteEndPoint);
                    return ValueTask.CompletedTask;
                },
                onClosed: (session, reason) =>
                {
                    var gameData = session.GetGameData();
                    
                    if (gameData.PlayerId > 0)
                    {
                        var playerManager = session.Server.ServiceProvider.GetRequiredService<PlayerManager>();
                        var player = playerManager.RemovePlayer(gameData.PlayerId);
                        if (player != null)
                        {
                            Log.Information("玩家断开连接: {PlayerId} {PlayerName}",
                                player.PlayerId, player.Name);
                        }
                    }

                    Metrics.ActiveConnections.Dec();
                    Metrics.ConnectionsTotal.WithLabels("disconnect").Inc();
                    Log.Information("客户端断开: {SessionId} Reason={Reason}",
                        session.SessionID, reason);
                    return ValueTask.CompletedTask;
                });

            // 服务器配置（从配置文件读取）
            hostBuilder.ConfigureSuperSocket(options =>
            {
                // 使用之前构建的临时配置对象
                options.Name = tempProvider["Server:Name"] ?? "GameServer";
                options.AddListener(new ListenOptions
                {
                    Ip = tempProvider["Server:Ip"] ?? "Any",
                    Port = tempProvider.GetValue<int>("Server:Port", 33333),
                    BackLog = tempProvider.GetValue<int>("Server:BackLog", 100)
                });
            });

            var host = hostBuilder.Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "服务器启动失败");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

/// <summary>
/// 消息处理器注册服务
/// </summary>
public class MessageHandlerRegistration : IHostedService
{
    private readonly MessageGateway _gateway;

    public MessageHandlerRegistration(MessageGateway gateway)
    {
        _gateway = gateway;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // ⭐ 关键修复：注册处理器类型，而不是实例
        // 这样 Gateway 只持有 Type 信息，不会持有 Scoped 的实例
        _gateway.RegisterHandler<LoginHandler>(MsgId.Login);
        _gateway.RegisterHandler<RegisterHandler>(MsgId.Register);
        _gateway.RegisterHandler<CreatePlayerHandler>(MsgId.CreatePlayer);
        _gateway.RegisterHandler<GetPlayerListHandler>(MsgId.GetPlayerList);

        Log.Information("消息处理器注册完成");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
