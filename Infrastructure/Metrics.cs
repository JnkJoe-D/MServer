using Prometheus;

namespace GameServer.Infrastructure;

/// <summary>
/// 监控指标定义
/// </summary>
public static class Metrics
{
    // ========== 连接指标 ==========
    
    /// <summary>
    /// 在线玩家数
    /// </summary>
    public static readonly Gauge OnlinePlayers = Prometheus.Metrics
        .CreateGauge("game_online_players", "当前在线玩家数");

    /// <summary>
    /// 活跃连接数
    /// </summary>
    public static readonly Gauge ActiveConnections = Prometheus.Metrics
        .CreateGauge("game_active_connections", "当前活跃连接数");

    /// <summary>
    /// 连接总数
    /// </summary>
    public static readonly Counter ConnectionsTotal = Prometheus.Metrics
        .CreateCounter("game_connections_total", "连接总数", new CounterConfiguration
        {
            LabelNames = new[] { "type" }  // connect, disconnect
        });

    // ========== 消息指标 ==========
    
    /// <summary>
    /// 消息处理次数
    /// </summary>
    public static readonly Counter MessageCount = Prometheus.Metrics
        .CreateCounter("game_message_total", "消息处理次数", new CounterConfiguration
        {
            LabelNames = new[] { "msg_id" }
        });

    /// <summary>
    /// 消息处理错误数
    /// </summary>
    public static readonly Counter MessageErrors = Prometheus.Metrics
        .CreateCounter("game_message_errors_total", "消息处理错误数", new CounterConfiguration
        {
            LabelNames = new[] { "msg_id" }
        });

    // ========== 业务指标 ==========
    
    /// <summary>
    /// 登录次数
    /// </summary>
    public static readonly Counter LoginCount = Prometheus.Metrics
        .CreateCounter("game_login_total", "登录次数", new CounterConfiguration
        {
            LabelNames = new[] { "result" }  // success, failed
        });

    /// <summary>
    /// 房间数量
    /// </summary>
    public static readonly Gauge RoomCount = Prometheus.Metrics
        .CreateGauge("game_room_count", "当前房间数");

    /// <summary>
    /// 匹配队列长度
    /// </summary>
    public static readonly Gauge MatchQueueLength = Prometheus.Metrics
        .CreateGauge("game_match_queue_length", "匹配队列长度", new GaugeConfiguration
        {
            LabelNames = new[] { "type" }  // 1v1, 3v3, 5v5
        });
}
