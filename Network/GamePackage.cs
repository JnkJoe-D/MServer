using System.Buffers;
using SuperSocket.ProtoBase;

namespace GameServer.Network;

/// <summary>
/// 游戏协议包
/// 格式: [Length:4字节][MsgId:2字节][Sequence:4字节][Payload:变长]
/// </summary>
public class GamePackage
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public ushort MsgId { get; set; }

    /// <summary>
    /// 消息序列号（防重放）
    /// </summary>
    public uint Sequence { get; set; }

    /// <summary>
    /// 消息体（Protobuf 序列化后的数据）
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; set; }

    /// <summary>
    /// 头部长度：Length(4) + MsgId(2) + Sequence(4) = 10 字节
    /// </summary>
    public const int HeaderSize = 10;

    /// <summary>
    /// 最大包大小（防止恶意大包）
    /// </summary>
    public const int MaxPackageSize = 64 * 1024; // 64KB
}

/// <summary>
/// 协议解析过滤器
/// 处理粘包/拆包
/// </summary>
public class GamePackageFilter : FixedHeaderPipelineFilter<GamePackage>
{
    /// <summary>
    /// 头部大小：10 字节
    /// </summary>
    public GamePackageFilter() : base(GamePackage.HeaderSize)
    {
    }

    /// <summary>
    /// 从头部获取包体长度
    /// </summary>
    protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        
        // 读取 Length（包含头部的总长度）
        reader.TryReadLittleEndian(out int totalLength);
        
        // 包体长度 = 总长度 - 头部长度
        int bodyLength = totalLength - GamePackage.HeaderSize;
        
        // 安全检查
        if (bodyLength < 0 || totalLength > GamePackage.MaxPackageSize)
        {
            throw new ProtocolException($"Invalid package size: {totalLength}");
        }
        
        return bodyLength;
    }

    /// <summary>
    /// 解析完整包
    /// </summary>
    protected override GamePackage DecodePackage(ref ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        
        // 跳过 Length（已在 GetBodyLengthFromHeader 中读取，无需再读）
        reader.Advance(4);
        
        // 读取 MsgId (2字节)
        reader.TryReadLittleEndian(out short msgIdRaw);
        ushort msgId = (ushort)msgIdRaw;
        
        // 读取 Sequence (4字节)
        reader.TryReadLittleEndian(out int sequenceRaw);
        uint sequence = (uint)sequenceRaw;
        
        // 剩余部分为 Payload
        var payload = buffer.Slice(GamePackage.HeaderSize);
        
        return new GamePackage
        {
            MsgId = msgId,
            Sequence = sequence,
            Payload = payload.ToArray()
        };
    }
}

/// <summary>
/// 协议异常
/// </summary>
public class ProtocolException : Exception
{
    public ProtocolException(string message) : base(message) { }
}
