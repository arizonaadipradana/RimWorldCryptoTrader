@echo off
setlocal enabledelayedexpansion

echo ================================================
echo          RimWorld Crypto Trader Mod Build
echo ================================================

echo.
echo Cleaning ALL build files and caches...
if exist "bin" rmdir /s /q "bin" 2>nul
if exist "obj" rmdir /s /q "obj" 2>nul
if exist "..\..\Assemblies\RimWorldCryptoTrader.dll" del "..\..\Assemblies\RimWorldCryptoTrader.dll" 2>nul
if exist "..\..\Assemblies\RimWorldCryptoTrader.pdb" del "..\..\Assemblies\RimWorldCryptoTrader.pdb" 2>nul
if exist "..\..\Assemblies\Newtonsoft.Json.dll" del "..\..\Assemblies\Newtonsoft.Json.dll" 2>nul
if exist "..\..\Assemblies\0Harmony.dll" del "..\..\Assemblies\0Harmony.dll" 2>nul
if exist "..\..\Assemblies\System.Net.Http.dll" del "..\..\Assemblies\System.Net.Http.dll" 2>nul
if exist "..\..\Assemblies\System.Numerics.dll" del "..\..\Assemblies\System.Numerics.dll" 2>nul

echo Clearing NuGet cache...
dotnet nuget locals all --clear

echo.
echo Restoring NuGet packages...
dotnet restore --verbosity normal --force
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    echo Check your internet connection and NuGet configuration
    pause
    exit /b 1
)

echo Restore complete
echo.
echo Building project (Release)...
dotnet build --configuration Release --no-restore --verbosity normal
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Build failed!
    echo.
    echo Common solutions:
    echo 1. Make sure RimWorld is installed
    echo 2. Copy required DLLs to References folder (see References\README.md)
    echo 3. Check that NuGet packages are properly restored
    echo 4. Verify that .NET Framework 4.8 is installed
    echo.
    pause
    exit /b 1
)

echo.
echo Build succeeded
echo.
echo Checking output files...
if exist "..\..\Assemblies\RimWorldCryptoTrader.dll" (
    echo ✓ Main assembly created successfully
) else (
    echo ✗ Main assembly not found
    goto :buildfailed
)

if exist "..\..\Assemblies\Newtonsoft.Json.dll" (
    echo ✓ Newtonsoft.Json.dll copied
) else (
    echo ! Newtonsoft.Json.dll not found - checking alternative locations
)

if exist "..\..\Assemblies\0Harmony.dll" (
    echo ✓ Harmony library copied
) else (
    echo ! 0Harmony.dll not found - checking alternative locations
)

echo.
echo ================================================
echo Build completed successfully!
echo ================================================
echo.
echo Files in Assemblies folder:
if exist "..\..\Assemblies\*.dll" (
    dir "..\..\Assemblies\*.dll" /b
) else (
    echo No DLL files found in Assemblies folder
)

echo.
echo Build completed in !time! seconds
goto :end

:buildfailed
echo.
echo ================================================
echo Build failed - missing output files
echo ================================================
pause
exit /b 1

:end
echo.
pause