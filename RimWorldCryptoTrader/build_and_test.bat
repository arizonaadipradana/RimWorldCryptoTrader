@echo off
echo ================================
echo Building RimWorld CryptoTrader Mod
echo ================================

echo Step 1: Cleaning previous builds...
call clean_mod.bat
if errorlevel 1 (
    echo ERROR: Clean failed
    pause
    exit /b 1
)

echo Step 2: Building mod...
call build.bat
if errorlevel 1 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo Step 3: Installing mod...
call install_mod.bat
if errorlevel 1 (
    echo ERROR: Install failed
    pause
    exit /b 1
)

echo ================================
echo BUILD SUCCESSFUL!
echo ================================
echo.
echo IMPORTANT CHANGES MADE:
echo - Changed keybind from F9 to DELETE key
echo - Added enhanced debug conflict prevention
echo - Added error handling to prevent crashes
echo.
echo To test:
echo 1. Start RimWorld
echo 2. Load a game/start new game
echo 3. Press DELETE key to open trading terminal
echo.
echo If you still get errors, check the log for [CryptoTrader] messages
echo ================================
pause
