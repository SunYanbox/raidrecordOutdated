@echo off
setlocal enabledelayedexpansion

:: 避免模组文件被占用
taskkill -F -T -IM SPT.Server.exe

:: 设置源路径和目标路径
set "SOURCE_PATH=E:\A_EFT_MOD_LEARN\spt4.0.0\suntion-raidrecord\raidrecord\raidrecord-v0.5.1\db"
set "DEST_PATH=F:\EFT_4_0_0\SPT\user\mods\raidrecord-v0.5.1-0.5.1\db"

:: 复制目录
echo copy folder now...
xcopy "%SOURCE_PATH%\locals" "%DEST_PATH%\locals\" /E /I /H /Y
@REM copy "%SOURCE_PATH%\locals\*" "%DEST_PATH%\locals\"


set "SOURCE_PATH=E:\A_EFT_MOD_LEARN\spt4.0.0\suntion-raidrecord\raidrecord\raidrecord-v0.5.1\bin\Release\raidrecord-v0.5.1-0.5.1\raidrecord-v0.5.1.dll"
set "DEST_PATH=F:\EFT_4_0_0\SPT\user\mods\raidrecord-v0.5.1-0.5.1\"
copy "%SOURCE_PATH%" "%DEST_PATH%\"
set "SOURCE_PATH=E:\A_EFT_MOD_LEARN\spt4.0.0\suntion-raidrecord\raidrecord\raidrecord-v0.5.1\bin\Release\raidrecord-v0.5.1-0.5.1\raidrecord-v0.5.1.pdb"
copy "%SOURCE_PATH%" "%DEST_PATH%\"

set "SOURCE_PATH=E:\A_EFT_MOD_LEARN\spt4.0.0\suntion-raidrecord\raidrecord\raidrecord-v0.5.1\config.json"
copy "%SOURCE_PATH%" "%DEST_PATH%\"

echo copy finash!
pause