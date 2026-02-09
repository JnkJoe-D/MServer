# Git 版本控制指南（TortoiseGit）

## 当前状态

您已经使用 TortoiseGit 在 `D:\DesKtop\Project\MServer` 目录创建了本地仓库。

---

## 后续操作步骤

### 1️⃣ 配置 .gitignore（已完成）

✅ 已创建 `.gitignore` 文件，自动排除：
- 编译输出（bin/、obj/）
- IDE 配置（.vs/、.idea/）
- 发布文件（publish/）
- 日志文件（logs/）
- **敏感配置文件**（appsettings.Production.json）

---

### 2️⃣ 第一次提交（初始化提交）

#### 方式 A：使用 TortoiseGit（推荐，图形化）

**步骤 1：添加文件到暂存区**

1. 在 `MServer` 目录右键
2. 选择 **TortoiseGit** → **Add...**
3. 勾选所有文件（.gitignore 会自动排除不需要的）
4. 点击 **OK**

**步骤 2：提交到本地仓库**

1. 在 `MServer` 目录右键
2. 选择 **Git Commit -> "master"...**
3. 填写提交信息：
   ```
   Initial commit: MServer game server framework
   
   - Network layer with SuperSocket 2.0.2
   - MySQL + Redis integration
   - JWT authentication
   - Message handlers for login/register
   - Connection health check
   ```
4. 勾选所有要提交的文件
5. 点击 **Commit**

#### 方式 B：使用命令行

```bash
cd D:\DesKtop\Project\MServer

# 添加所有文件
git add .

# 提交
git commit -m "Initial commit: MServer game server framework"
```

---

### 3️⃣ 关联远程仓库（GitHub/Gitee）

#### 在 GitHub/Gitee 创建仓库

1. 登录 GitHub 或 Gitee
2. 点击 **New repository**
3. 仓库名：`MServer`
4. **不要**勾选 "Initialize this repository with a README"
5. 点击 **Create repository**

#### 关联远程仓库（TortoiseGit）

**步骤 1：复制远程仓库地址**

GitHub 示例：
```
https://github.com/your-username/MServer.git
```

Gitee 示例：
```
https://gitee.com/your-username/MServer.git
```

**步骤 2：添加远程仓库**

1. 在 `MServer` 目录右键
2. 选择 **TortoiseGit** → **Settings**
3. 左侧选择 **Git** → **Remote**
4. 点击 **Add New/Save**
5. Remote: `origin`
6. URL: 粘贴远程仓库地址
7. 点击 **Add New/Save**
8. 点击 **OK**

#### 关联远程仓库（命令行）

```bash
# GitHub
git remote add origin https://github.com/your-username/MServer.git

# Gitee
git remote add origin https://gitee.com/your-username/MServer.git

# 验证
git remote -v
```

---

### 4️⃣ 推送到远程仓库

#### 使用 TortoiseGit

1. 在 `MServer` 目录右键
2. 选择 **TortoiseGit** → **Push...**
3. Remote: `origin`
4. Ref: `master` → `master`
5. 勾选 **Force** 如果需要（第一次不需要）
6. 点击 **OK**
7. 输入 GitHub/Gitee 账号密码（或使用 Token）

#### 使用命令行

```bash
# 第一次推送（设置上游分支）
git push -u origin master

# 后续推送
git push
```

**如果使用 Token 认证（GitHub 2021年后必须）**：

```bash
# 使用 Personal Access Token
git push https://your-token@github.com/your-username/MServer.git
```

---

## 日常使用流程

### 📝 修改代码后提交

#### TortoiseGit 方式

1. **查看状态**
   - 右键 → **TortoiseGit** → **Check for modifications**

2. **提交更改**
   - 右键 → **Git Commit -> "master"...**
   - 填写提交信息（描述此次修改）
   - 选择要提交的文件
   - 点击 **Commit**

3. **推送到远程**
   - 右键 → **TortoiseGit** → **Push...**
   - 点击 **OK**

#### 命令行方式

```bash
# 1. 查看状态
git status

# 2. 添加文件
git add .                    # 添加所有修改
git add Program.cs           # 添加单个文件

# 3. 提交
git commit -m "Fix: Redis password configuration"

# 4. 推送
git push
```

---

## 常用 Git 操作

### 📋 查看历史

**TortoiseGit**：
- 右键 → **TortoiseGit** → **Show log**

**命令行**：
```bash
git log
git log --oneline --graph --all  # 图形化显示
```

### ↩️ 撤销修改

**未暂存的修改（未 git add）**

TortoiseGit：
- 右键文件 → **TortoiseGit** → **Revert...**

命令行：
```bash
git checkout -- Program.cs    # 撤销单个文件
git checkout -- .             # 撤销所有修改
```

**已暂存的修改（已 git add）**

```bash
git reset HEAD Program.cs     # 取消暂存
git checkout -- Program.cs    # 撤销修改
```

**已提交的修改（已 git commit）**

```bash
git reset --soft HEAD~1       # 撤销提交，保留修改
git reset --hard HEAD~1       # 撤销提交，丢弃修改（危险！）
```

### 🔄 拉取远程更新

**TortoiseGit**：
- 右键 → **TortoiseGit** → **Pull...**

**命令行**：
```bash
git pull origin master
```

### 🌿 分支管理

**创建新分支**

TortoiseGit：
- 右键 → **TortoiseGit** → **Create Branch...**

命令行：
```bash
git checkout -b feature/new-handler
```

**切换分支**

TortoiseGit：
- 右键 → **TortoiseGit** → **Switch/Checkout...**

命令行：
```bash
git checkout master
```

**合并分支**

```bash
git checkout master
git merge feature/new-handler
```

---

## 提交信息规范（推荐）

使用 **约定式提交**（Conventional Commits）：

```
<类型>: <简短描述>

[可选的详细描述]

[可选的关联信息]
```

### 类型

| 类型 | 说明 | 示例 |
|------|------|------|
| `feat` | 新功能 | `feat: Add player inventory system` |
| `fix` | 修复 Bug | `fix: Redis connection password not applied` |
| `docs` | 文档更新 | `docs: Add deployment guide` |
| `refactor` | 重构 | `refactor: Extract message routing logic` |
| `perf` | 性能优化 | `perf: Optimize database query` |
| `test` | 测试 | `test: Add unit tests for AuthService` |
| `chore` | 杂项 | `chore: Update dependencies` |

### 示例

```bash
# 好的提交信息
git commit -m "feat: Add connection health check on startup"
git commit -m "fix: MySQL table name mismatch in init.sql"
git commit -m "docs: Create Git usage guide for TortoiseGit"

# 不好的提交信息
git commit -m "update"
git commit -m "修复bug"
git commit -m "aaa"
```

---

## .gitignore 说明

### 已排除的敏感文件

⚠️ **重要**：以下文件**不会**被提交到 Git 仓库：

| 文件 | 原因 |
|------|------|
| `appsettings.Production.json` | 包含生产环境密码 |
| `appsettings.Development.json` | 可能包含本地配置 |
| `appsettings.Staging.json` | 可能包含测试环境密码 |

### 保留的文件

✅ **会被提交**的配置文件：

- `appsettings.json`（默认配置，不包含真实密码）

### 管理敏感配置

**方式 1：使用环境变量（推荐）**

生产环境不提交配置文件，改用环境变量：

```bash
export Database__ConnectionString="Server=..."
export Redis__Password="..."
```

**方式 2：使用 Git 加密**

安装 `git-crypt` 或 `BlackBox` 加密敏感文件。

**方式 3：分离配置**

将生产配置保存在服务器本地，不纳入版本控制。

---

## 快捷操作（TortoiseGit）

### 右键菜单常用项

| 菜单项 | 快捷键 | 说明 |
|--------|--------|------|
| **Git Commit** | - | 提交更改 |
| **Push** | - | 推送到远程 |
| **Pull** | - | 从远程拉取 |
| **Show log** | - | 查看历史 |
| **Diff** | - | 查看文件差异 |
| **Revert** | - | 撤销修改 |
| **Check for modifications** | - | 查看修改状态 |

---

## 常见问题

### Q1: 提示 "remote rejected"

**原因**：远程仓库有您本地没有的提交。

**解决**：

```bash
git pull origin master --rebase
git push origin master
```

### Q2: 提示需要用户名密码

**GitHub（Token 认证）**：

1. GitHub → Settings → Developer settings → Personal access tokens
2. Generate new token (classic)
3. 勾选 `repo` 权限
4. 复制 Token（只显示一次！）
5. 推送时输入 Token 作为密码

**Gitee（密码认证）**：

直接使用 Gitee 账号密码。

### Q3: 文件冲突

**TortoiseGit**：

1. Pull 时提示冲突
2. 右键冲突文件 → **TortoiseGit** → **Edit conflicts**
3. 手动解决冲突
4. 标记为已解决
5. 提交

**命令行**：

```bash
git pull
# 提示冲突
# 手动编辑冲突文件，删除冲突标记
git add .
git commit -m "Merge: Resolve conflicts"
git push
```

### Q4: 意外提交了敏感文件

**从历史中完全删除**：

```bash
# 安装 BFG Repo-Cleaner
# 删除文件
bfg --delete-files appsettings.Production.json

# 或使用 git filter-branch（复杂）
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch appsettings.Production.json" \
  --prune-empty --tag-name-filter cat -- --all

# 强制推送
git push origin --force --all
```

⚠️ **注意**：如果已提交敏感信息，应立即：
1. 修改所有暴露的密码
2. 从 Git 历史中删除文件
3. 强制推送覆盖远程历史

---

## 总结

### 首次设置（一次性）

```bash
# 1. 已完成：创建本地仓库（TortoiseGit → Create repository）
# 2. 已完成：创建 .gitignore
# 3. 第一次提交
右键 → Git Commit → 填写信息 → Commit
# 4. 关联远程仓库
TortoiseGit → Settings → Remote → 添加 origin
# 5. 推送
TortoiseGit → Push → OK
```

### 日常工作流

```bash
# 1. 修改代码
# ...

# 2. 提交
右键 → Git Commit → 填写信息 → Commit

# 3. 推送
右键 → Push → OK

# 4. 拉取（协作时）
右键 → Pull → OK
```

### 推荐工具

| 工具 | 说明 |
|------|------|
| **TortoiseGit** | ✅ 您已安装，图形化操作 |
| **VS Code** | 内置 Git 支持，轻量级 |
| **GitHub Desktop** | GitHub 官方客户端 |
| **GitKraken** | 漂亮的 Git 图形工具 |
