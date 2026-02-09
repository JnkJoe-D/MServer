@echo off
REM ========================================
REM MySQL 数据库初始化脚本（Windows）
REM ========================================

echo ========================================
echo MySQL 数据库初始化
echo ========================================
echo.

REM 从 appsettings.json 读取配置（简化版，手动设置）
set DB_HOST=127.0.0.1
set DB_PORT=23333
set DB_USER=GameServer
set DB_NAME=gameserver

echo 数据库信息:
echo   主机: %DB_HOST%:%DB_PORT%
echo   用户: %DB_USER%
echo   数据库: %DB_NAME%
echo.

REM 提示输入密码
set /p DB_PASS=请输入数据库密码: 

echo.
echo 正在执行 SQL 脚本...
echo.

REM 执行 SQL 脚本
mysql -h %DB_HOST% -P %DB_PORT% -u %DB_USER% -p%DB_PASS% < "%~dp0init.sql"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo ✅ 数据库初始化成功！
    echo ========================================
    echo.
    echo 测试用户信息:
    echo   用户名: test
    echo   密码: 12345678
    echo.
) else (
    echo.
    echo ========================================
    echo ❌ 数据库初始化失败！
    echo ========================================
    echo.
    echo 请检查:
    echo   1. MySQL 服务是否运行
    echo   2. 连接信息是否正确
    echo   3. 用户是否有创建数据库的权限
    echo.
)

pause
