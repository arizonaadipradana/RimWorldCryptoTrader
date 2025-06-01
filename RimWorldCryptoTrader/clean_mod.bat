@echo off
echo ================================================
echo      Cleaning RimWorld Mod Installation
echo ================================================

set "MOD_DIR=D:\SteamLibrary\steamapps\common\RimWorld\Mods\RimWorldCryptoTrader"

echo.
echo Cleaning existing mod installation...
if exist "%MOD_DIR%" (
    echo Removing old mod files...
    rmdir /s /q "%MOD_DIR%" 2>nul
    echo Old mod files removed.
) else (
    echo No existing mod installation found.
)

echo.
echo RimWorld mod directory cleaned successfully!
echo You can now run build.bat to rebuild and install.
echo.
pause