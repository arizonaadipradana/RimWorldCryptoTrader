@echo off
echo Installing RimWorldCryptoTrader to RimWorld...

set "SOURCE_DIR=C:\Users\user\source\repos\RimWorldCryptoTrader"
set "TARGET_DIR=D:\SteamLibrary\steamapps\common\RimWorld\Mods\RimWorldCryptoTrader"

echo Copying mod files...
if exist "%TARGET_DIR%" rmdir /s /q "%TARGET_DIR%"
xcopy "%SOURCE_DIR%" "%TARGET_DIR%\" /E /I /Y

echo.
echo ================================================
echo Mod installed successfully!
echo ================================================
echo.
echo Location: %TARGET_DIR%
echo.
echo Next steps:
echo 1. Launch RimWorld
echo 2. Go to Mods menu
echo 3. Enable 'RimWorldCryptoTrader'
echo 4. Restart RimWorld
echo 5. Press INSERT in-game to trade crypto!
echo.
pause