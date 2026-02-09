namespace GameServer.Data.Entities;

/// <summary>
/// 用户账号
/// </summary>
public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsBanned { get; set; }
    public DateTime? BannedUntil { get; set; }
}

/// <summary>
/// 玩家角色
/// </summary>
public class Player
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public long Exp { get; set; }
    public int SceneId { get; set; } = 1;
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public long Gold { get; set; }
    public long Diamond { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// 玩家道具
/// </summary>
public class PlayerItem
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int ItemId { get; set; }
    public int Count { get; set; } = 1;
    public int Slot { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 好友关系
/// </summary>
public class Friendship
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public long FriendId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 邮件
/// </summary>
public class Mail
{
    public long Id { get; set; }
    public long SenderId { get; set; }       // 0表示系统邮件
    public long ReceiverId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Attachments { get; set; } = string.Empty;  // JSON格式
    public bool IsRead { get; set; }
    public bool IsCollected { get; set; }    // 附件是否已领取
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpireAt { get; set; }
}

/// <summary>
/// 玩家任务
/// </summary>
public class PlayerQuest
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int QuestId { get; set; }
    public int Status { get; set; }          // 0=进行中, 1=已完成, 2=已领奖
    public string Progress { get; set; } = string.Empty;  // JSON格式
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 战斗记录
/// </summary>
public class BattleRecord
{
    public long Id { get; set; }
    public int RoomId { get; set; }
    public int WinnerTeam { get; set; }
    public int Duration { get; set; }        // 战斗时长（秒）
    public string PlayerIds { get; set; } = string.Empty;  // JSON数组
    public byte[]? ReplayData { get; set; }  // 回放数据
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
