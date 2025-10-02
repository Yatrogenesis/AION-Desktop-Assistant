@echo off
echo ===================================
echo AION Desktop Assistant Build Script
echo ===================================

echo.
echo Checking for .NET SDK...
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK found!
dotnet --version

echo.
echo Building Release version...
dotnet build --configuration Release

if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Publishing self-contained executable...
dotnet publish --configuration Release --runtime win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true

if %errorlevel% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)

echo.
echo ===================================
echo BUILD SUCCESSFUL!
echo ===================================
echo.
echo Executable location:
echo bin\Release\net8.0-windows\win-x64\publish\AionDesktopAssistant.exe
echo.
pause