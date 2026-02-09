using Google.Protobuf;
using SuperSocket.Server.Abstractions.Session;

namespace GameServer.Network;

/// <summary>
/// 消息处理器接口
/// </summary>
public interface IMessageHandler
{
    Task HandleAsync(IAppSession session, ReadOnlyMemory<byte> payload);
}

/// <summary>
/// 消息处理器基类
/// 自动反序列化 Protobuf 消息
/// <typeparam name="TRequest">必须是Protobuf生成的消息类，并且有无参构造函数</typeparam>
/// </summary>
public abstract class MessageHandler<TRequest> : IMessageHandler
    where TRequest : IMessage<TRequest>, new()
{
    private static readonly MessageParser<TRequest> Parser = new MessageParser<TRequest>(() => new TRequest());

    public async Task HandleAsync(IAppSession session, ReadOnlyMemory<byte> payload)
    {
        var request = Parser.ParseFrom(payload.Span);
        await HandleAsync(session, request);
    }

    /// <summary>
    /// 处理解析后的消息
    /// </summary>
    protected abstract Task HandleAsync(IAppSession session, TRequest request);
}

/// <summary>
/// 需要认证的消息处理器基类
/// </summary>
public abstract class AuthenticatedMessageHandler<TRequest> : MessageHandler<TRequest>
    where TRequest : IMessage<TRequest>, new()
{
    protected sealed override async Task HandleAsync(IAppSession session, TRequest request)
    {
        var gameData = session.GetGameData();
        
        if (!gameData.IsAuthenticated)
        {
            await session.SendErrorAsync((int)Protocol.ErrorCode.NotAuthenticated, "请先登录");
            return;
        }

        await HandleAuthenticatedAsync(session, request);
    }

    /// <summary>
    /// 处理已认证的消息
    /// </summary>
    protected abstract Task HandleAuthenticatedAsync(IAppSession session, TRequest request);
}
