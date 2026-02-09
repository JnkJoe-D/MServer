using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GameServer.Infrastructure;
using SuperSocket.Server.Abstractions.Session;

namespace GameServer.Network;

/// <summary>
/// 消息网关
/// 负责消息路由和分发
/// </summary>
public class MessageGateway
{
    private readonly ILogger<MessageGateway> _logger;

    /// <summary>
    /// 消息处理器类型映射：MsgId -> Handler Type
    /// 注意：这里存储的是 Type，而不是实例，避免生命周期问题
    /// 存储已注册的消息处理器类型，实际处理时通过 DI 创建实例
    /// </summary>
    private readonly Dictionary<ushort, Type> _handlerTypes = new();
    /// <summary>
    /// DI容器提供者，用于创建DI容器(IServiceCollection)中的处理器实例
    /// </summary>

    private readonly IServiceProvider _serviceProvider;

    public MessageGateway(ILogger<MessageGateway> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 注册消息处理器类型
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="msgId">消息 ID</param>
    public void RegisterHandler<THandler>(ushort msgId) where THandler : IMessageHandler
    {
        var handlerType = typeof(THandler);
        
        if (_handlerTypes.ContainsKey(msgId))
        {
            _logger.LogWarning("消息处理器重复注册: 0x{MsgId:X4}", msgId);
        }
        
        _handlerTypes[msgId] = handlerType;
        _logger.LogDebug("注册消息处理器: 0x{MsgId:X4} -> {Handler}", msgId, handlerType.Name);
    }

    /// <summary>
    /// 路由消息，分发消息到对应的处理器
    /// </summary>
    public async Task RouteMessageAsync(IAppSession session, GamePackage package)
    {
        var gameData = session.GetGameData();
        
        // 心跳消息特殊处理（不需要认证）
        if (package.MsgId == MsgId.Heartbeat)
        {
            await HandleHeartbeatAsync(session);
            return;
        }

        // 验证序列号（登录消息除外）
        if (package.MsgId != MsgId.Login && package.MsgId != MsgId.Register)
        {
            if (!gameData.ValidateSequence(package.Sequence))
            {
                _logger.LogWarning("消息重放攻击: Session={SessionId}, MsgId=0x{MsgId:X4}, Seq={Seq}",
                    session.SessionID, package.MsgId, package.Sequence);
                return;
            }
        }

        // 查找处理器类型
        if (!_handlerTypes.TryGetValue(package.MsgId, out var handlerType))
        {
            _logger.LogWarning("未知消息ID: 0x{MsgId:X4}", package.MsgId);
            await session.SendErrorAsync((int)Protocol.ErrorCode.UnknownError, "未知消息");
            return;
        }

        try
        {
            // ⭐ 关键修复：每次处理消息时创建新的 Scope
            // 确保 Scoped 服务（如 DbContext）的生命周期正确
            using var scope = _serviceProvider.CreateScope();
            
            // 从新的 Scope 中解析处理器实例
            var handler = scope.ServiceProvider.GetRequiredService(handlerType) as IMessageHandler;
            
            if (handler == null)
            {
                _logger.LogError("无法解析处理器: {HandlerType}", handlerType.Name);
                return;
            }
            
            // 执行处理器
            await handler.HandleAsync(session, package.Payload);
            
            Metrics.MessageCount.WithLabels(package.MsgId.ToString("X4")).Inc();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息处理异常: MsgId=0x{MsgId:X4}", package.MsgId);
            Metrics.MessageErrors.WithLabels(package.MsgId.ToString("X4")).Inc();
            await session.SendErrorAsync((int)Protocol.ErrorCode.UnknownError, "服务器内部错误");
        }
    }

    /// <summary>
    /// 处理心跳
    /// </summary>
    private async Task HandleHeartbeatAsync(IAppSession session)
    {
        var response = new Protocol.S2C_Heartbeat
        {
            ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await session.SendMessageAsync(MsgId.Heartbeat, response);
    }
}
