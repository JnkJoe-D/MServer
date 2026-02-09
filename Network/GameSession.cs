using System.Net;
using SuperSocket.Server.Abstractions.Session;
using Google.Protobuf;

namespace GameServer.Network;

/// <summary>
/// 游戏会话扩展数据
/// 存储在 IAppSession 的 DataContext 中
/// </summary>
public class GameSessionData
{
    /// <summary>
    /// 用户ID（登录前为0）
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 当前角色ID
    /// </summary>
    public long PlayerId { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已认证
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// 认证Token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 最后一个消息序列号（防重放）
    /// </summary>
    public uint LastSequence { get; set; }

    /// <summary>
    /// 当前场景ID
    /// </summary>
    public int CurrentSceneId { get; set; }

    /// <summary>
    /// 当前房间ID
    /// </summary>
    public int CurrentRoomId { get; set; }

    /// <summary>
    /// 检查消息序列号（防重放）
    /// </summary>
    public bool ValidateSequence(uint sequence)
    {
        if (sequence <= LastSequence)
            return false;
        LastSequence = sequence;
        return true;
    }
}

/// <summary>
/// IAppSession 扩展方法
/// </summary>
public static class GameSessionExtensions
{
    /// <summary>
    /// 获取游戏会话数据
    /// </summary>
    public static GameSessionData GetGameData(this IAppSession session)
    {
        var data = session.DataContext as GameSessionData;
        if (data == null)
        {
            data = new GameSessionData();
            session.DataContext = data;
        }
        return data;
    }

    /// <summary>
    /// 发送 Protobuf 消息
    /// </summary>
    public static async ValueTask SendMessageAsync(this IAppSession session, ushort msgId, IMessage message)
    {
        var payload = message.ToByteArray();
        await session.SendPackageAsync(msgId, payload);
    }

    /// <summary>
    /// 发送原始包
    /// </summary>
    public static async ValueTask SendPackageAsync(this IAppSession session, ushort msgId, byte[] payload)
    {
        int totalLength = GamePackage.HeaderSize + payload.Length;
        var buffer = new byte[totalLength];

        // 写入 Length (4字节，小端)
        BitConverter.TryWriteBytes(buffer.AsSpan(0, 4), totalLength);
        // 写入 MsgId (2字节，小端)
        BitConverter.TryWriteBytes(buffer.AsSpan(4, 2), msgId);
        // 写入 Sequence (4字节，小端) - 响应使用0
        BitConverter.TryWriteBytes(buffer.AsSpan(6, 4), 0u);
        // 写入 Payload
        payload.CopyTo(buffer, GamePackage.HeaderSize);

        await session.SendAsync(new ReadOnlyMemory<byte>(buffer));
    }

    /// <summary>
    /// 发送错误响应
    /// </summary>
    public static async ValueTask SendErrorAsync(this IAppSession session, int code, string message = "")
    {
        var response = new Protocol.CommonResponse
        {
            Code = code,
            Message = message
        };
        await session.SendMessageAsync(MsgId.Error, response);
    }
}
