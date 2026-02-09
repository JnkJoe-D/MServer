using Microsoft.EntityFrameworkCore;
using GameServer.Data.Entities;

namespace GameServer.Data;

/// <summary>
/// 游戏数据库上下文
/// </summary>
public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    // ========== 用户相关 ==========
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Player> Players { get; set; } = null!;
    
    // ========== 背包相关 ==========
    public DbSet<PlayerItem> PlayerItems { get; set; } = null!;
    
    // ========== 社交相关 ==========
    public DbSet<Friendship> Friendships { get; set; } = null!;
    public DbSet<Mail> Mails { get; set; } = null!;
    
    // ========== 任务相关 ==========
    public DbSet<PlayerQuest> PlayerQuests { get; set; } = null!;
    
    // ========== 战斗相关 ==========
    public DbSet<BattleRecord> BattleRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Player
        modelBuilder.Entity<Player>(entity =>
        {
            entity.ToTable("players");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PlayerItem
        modelBuilder.Entity<PlayerItem>(entity =>
        {
            entity.ToTable("player_items");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlayerId);
        });

        // Friendship
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.ToTable("friendships");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlayerId);
            entity.HasIndex(e => new { e.PlayerId, e.FriendId }).IsUnique();
        });

        // Mail
        modelBuilder.Entity<Mail>(entity =>
        {
            entity.ToTable("mails");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReceiverId);
        });

        // PlayerQuest
        modelBuilder.Entity<PlayerQuest>(entity =>
        {
            entity.ToTable("player_quests");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlayerId);
        });

        // BattleRecord
        modelBuilder.Entity<BattleRecord>(entity =>
        {
            entity.ToTable("battle_records");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
