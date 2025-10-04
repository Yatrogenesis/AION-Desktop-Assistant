@echo off
echo ===============================================
echo AION Desktop Assistant - Release Build Script
echo ===============================================

:: Check if .NET SDK is available
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET 8 SDK.
    echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK found:
dotnet --version

:: Clean previous builds
echo.
echo Cleaning previous builds...
dotnet clean --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Failed to clean project
    pause
    exit /b 1
)

:: Restore packages
echo.
echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

:: Build Release configuration
echo.
echo Building Release configuration...
dotnet build --configuration Release --no-restore --verbosity normal
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

:: Run tests
echo.
echo Running tests...
dotnet test --configuration Release --no-build --verbosity normal
if %errorlevel% neq 0 (
    echo WARNING: Some tests failed
)

echo.
echo ===============================================
echo Build completed successfully!
echo Output location: bin\Release\net8.0-windows\
echo ===============================================
pause