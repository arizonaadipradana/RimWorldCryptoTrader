@echo off
setlocal enabledelayedexpansion

echo ================================================
echo    RimWorld Crypto Trader - COMPLETE REBUILD
echo ================================================

echo Step 1: Cleaning RimWorld mod installation...
set "MOD_DIR=D:\SteamLibrary\steamapps\common\RimWorld\Mods\RimWorldCryptoTrader"
if exist "%MOD_DIR%" (
    rmdir /s /q "%MOD_DIR%" 2>nul
    echo ✓ Old mod installation removed
) else (
    echo ✓ No existing mod installation found
)

echo.
echo Step 2: Cleaning ALL build files and caches...
if exist "bin" rmdir /s /q "bin" 2>nul
if exist "obj" rmdir /s /q "obj" 2>nul
if exist "..\..\Assemblies\*.dll" del "..\..\Assemblies\*.dll" 2>nul
if exist "..\..\Assemblies\*.pdb" del "..\..\Assemblies\*.pdb" 2>nul

echo ✓ Clearing NuGet cache...
dotnet nuget locals all --clear > nul 2>&1

echo.
echo Step 3: Force restore NuGet packages...
dotnet restore --verbosity normal --force --no-cache
if %errorlevel% neq 0 (
    echo ❌ ERROR: Failed to restore packages
    pause
    exit /b 1
)
echo ✓ Packages restored successfully

echo.
echo Step 4: Building project (Release)...
dotnet build --configuration Release --no-restore --verbosity normal
if %errorlevel% neq 0 (
    echo ❌ ERROR: Build failed!
    echo.
    echo Common solutions:
    echo 1. Make sure RimWorld is installed at D:\SteamLibrary\steamapps\common\RimWorld
    echo 2. Copy required DLLs to References folder
    echo 3. Check that .NET Framework 4.8 is installed
    echo.
    pause
    exit /b 1
)
echo ✓ Build completed successfully

echo.
echo Step 5: Verifying output files...
set "ERRORS=0"
if exist "..\..\Assemblies\RimWorldCryptoTrader.dll" (
    echo ✓ Main assembly created: RimWorldCryptoTrader.dll
) else (
    echo ❌ Main assembly NOT found
    set "ERRORS=1"
)

if exist "..\..\Assemblies\0Harmony.dll" (
    echo ✓ Harmony library found: 0Harmony.dll
) else (
    echo ⚠️ Harmony library not found (may still work)
)

:: Check for problematic DLLs that should NOT be there
if exist "..\..\Assemblies\System.Net.Http.dll" (
    echo ❌ Found problematic System.Net.Http.dll - removing...
    del "..\..\Assemblies\System.Net.Http.dll" 2>nul
)

if exist "..\..\Assemblies\System.Numerics.dll" (
    echo ❌ Found problematic System.Numerics.dll - removing...
    del "..\..\Assemblies\System.Numerics.dll" 2>nul
)

if exist "..\..\Assemblies\Newtonsoft.Json.dll" (
    echo ❌ Found old Newtonsoft.Json.dll - removing...
    del "..\..\Assemblies\Newtonsoft.Json.dll" 2>nul
)

if "%ERRORS%"=="1" (
    echo ❌ BUILD VERIFICATION FAILED
    pause
    exit /b 1
)

echo.
echo Step 6: Installing to RimWorld...
echo Copying mod to RimWorld mods directory...
xcopy "C:\Users\user\source\repos\RimWorldCryptoTrader" "%MOD_DIR%\" /E /I /Y /Q
if %errorlevel% neq 0 (
    echo ❌ Failed to copy mod to RimWorld
    pause
    exit /b 1
)
echo ✓ Mod installed successfully

echo.
echo ================================================
echo ✅ SUCCESS! Mod rebuilt and installed!
echo ================================================
echo.
echo What changed:
echo ✓ Removed HttpClient (using UnityWebRequest)
echo ✓ Removed Newtonsoft.Json (using custom parser)
echo ✓ Removed System.Numerics dependency
echo ✓ Fixed all TypeLoadExceptions
echo.
echo Next steps:
echo 1. Launch RimWorld
echo 2. Enable 'RimWorldCryptoTrader' in Mods menu  
echo 3. Restart RimWorld
echo 4. Press INSERT in-game to test crypto trading!
echo.
echo No more dependency errors should occur.
echo.
pause