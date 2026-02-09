-- ========================================
-- 游戏服务器数据库初始化脚本
-- MySQL 5.7+
-- ========================================

-- 创建数据库
CREATE DATABASE IF NOT EXISTS `gameserver` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `gameserver`;

-- ========================================
-- 用户表
-- ========================================
CREATE TABLE IF NOT EXISTS `users` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `username` VARCHAR(32) NOT NULL,
    `email` VARCHAR(100),
    `password_hash` VARCHAR(100) NOT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `last_login_at` DATETIME,
    `is_banned` TINYINT(1) DEFAULT 0,
    `banned_until` DATETIME,
    UNIQUE INDEX `idx_username` (`username`),
    UNIQUE INDEX `idx_email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================
-- 角色表
-- ========================================
CREATE TABLE IF NOT EXISTS `players` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `user_id` BIGINT NOT NULL,
    `name` VARCHAR(32) NOT NULL,
    `level` INT DEFAULT 1,
    `exp` BIGINT DEFAULT 0,
    `scene_id` INT DEFAULT 1,
    `pos_x` FLOAT DEFAULT 0,
    `pos_y` FLOAT DEFAULT 0,
    `pos_z` FLOAT DEFAULT 0,
    `gold` BIGINT DEFAULT 0,
    `diamond` BIGINT DEFAULT 0,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `is_deleted` TINYINT(1) DEFAULT 0,
    `deleted_at` DATETIME,
    UNIQUE INDEX `idx_name` (`name`),
    INDEX `idx_user_id` (`user_id`),
    FOREIGN KEY (`user_id`) REFERENCES `users`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================
-- 道具表
-- ========================================
CREATE TABLE IF NOT EXISTS `player_items` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `player_id` BIGINT NOT NULL,
    `item_id` INT NOT NULL,
    `count` INT DEFAULT 1,
    `slot` INT,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_player_id` (`player_id`),
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================
-- 好友关系表
-- ========================================
CREATE TABLE IF NOT EXISTS `friendships` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `player_id` BIGINT NOT NULL,
    `friend_id` BIGINT NOT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_player_id` (`player_id`),
    UNIQUE INDEX `idx_player_friend` (`player_id`, `friend_id`),
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE,
    FOREIGN KEY (`friend_id`) REFERENCES `players`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================
-- 邮件表
-- ========================================
CREATE TABLE IF NOT EXISTS `mails` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `sender_id` BIGINT DEFAULT 0,
    `receiver_id` BIGINT NOT NULL,
    `title` VARCHAR(100) NOT NULL,
    `content` TEXT,
    `attachments` JSON,
    `is_read` TINYINT(1) DEFAULT 0,
    `is_collected` TINYINT(1) DEFAULT 0,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `expire_at` DATETIME,
    INDEX `idx_receiver_id` (`receiver_id`),
    FOREIGN KEY (`receiver_id`) REFERENCES `players`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================
-- 任务表
-- ========================================
CREATE TABLE IF NOT EXISTS `player_quests` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `player_id` BIGINT NOT NULL,
    `quest_id` INT NOT NULL,
    `status` TINYINT DEFAULT 0 COMMENT '0=进行中, 1=已完成, 2=已领奖',
    `progress` JSON,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `completed_at` DATETIME,
    INDEX `idx_player_id` (`player_id`),
    FOREIGN KEY (`player_id`) REFERENCES `players`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================
-- 战斗记录表
-- ========================================
CREATE TABLE IF NOT EXISTS `battle_records` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `room_id` INT,
    `winner_team` INT,
    `duration` INT COMMENT '战斗时长(秒)',
    `player_ids` JSON,
    `replay_data` LONGBLOB,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_created_at` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================
-- 聊天记录表（可选）
-- ========================================
CREATE TABLE IF NOT EXISTS `chat_logs` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `channel` TINYINT NOT NULL COMMENT '0=世界, 1=场景, 2=私聊, 3=队伍, 4=公会',
    `sender_id` BIGINT NOT NULL,
    `receiver_id` BIGINT DEFAULT 0,
    `content` VARCHAR(500) NOT NULL,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_sender_id` (`sender_id`),
    INDEX `idx_created_at` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========================================
-- 初始测试数据
-- ========================================

-- 插入测试用户 (密码: 12345678，BCrypt加密)
INSERT INTO `users` (`username`, `email`, `password_hash`, `created_at`) VALUES
('test', 'test@test.com', '$2a$11$K5gGXvP1ck5C1a5V4oCEI.Z3rX8D5F9E1q2W3e4R5t6Y7u8I9o0P', NOW());

SELECT 'Database initialized successfully!' AS message;
