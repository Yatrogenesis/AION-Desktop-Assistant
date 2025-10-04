@echo off
echo ===============================================
echo AION Desktop Assistant - Multi-Platform Installer Creator
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

:: Create output directories
echo.
echo Creating output directories...
if not exist "installers" mkdir installers
if not exist "releases" mkdir releases

:: Clean previous builds
echo.
echo Cleaning previous builds...
dotnet clean --configuration Release

:: Restore packages
echo.
echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

:: Get version from project file
echo.
echo Extracting version information...
for /f "tokens=2 delims=<>" %%i in ('findstr "<Version>" AionDesktopAssistant.csproj') do set VERSION=%%i
if "%VERSION%"=="" set VERSION=1.0.0
echo Version: %VERSION%

:: Build Release configuration
echo.
echo Building Release configuration...
dotnet build --configuration Release --no-restore --verbosity normal
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

:: Create self-contained executables for different platforms
echo.
echo ===============================================
echo Creating self-contained executables...
echo ===============================================

:: Windows x64
echo.
echo Creating Windows x64 installer...
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output "releases\win-x64\v%VERSION%" /p:PublishSingleFile=true /p:PublishTrimmed=true
if %errorlevel% equ 0 (
    echo SUCCESS: Windows x64 build successful
    powershell -Command "Compress-Archive -Path 'releases\win-x64\v%VERSION%\*' -DestinationPath 'installers\AION-Desktop-Assistant-v%VERSION%-win-x64.zip' -Force"
    echo SUCCESS: Windows x64 installer created: installers\AION-Desktop-Assistant-v%VERSION%-win-x64.zip
) else (
    echo ERROR: Windows x64 build failed
)

:: Windows x86
echo.
echo Creating Windows x86 installer...
dotnet publish --configuration Release --runtime win-x86 --self-contained true --output "releases\win-x86\v%VERSION%" /p:PublishSingleFile=true /p:PublishTrimmed=true
if %errorlevel% equ 0 (
    echo SUCCESS: Windows x86 build successful
    powershell -Command "Compress-Archive -Path 'releases\win-x86\v%VERSION%\*' -DestinationPath 'installers\AION-Desktop-Assistant-v%VERSION%-win-x86.zip' -Force"
    echo SUCCESS: Windows x86 installer created: installers\AION-Desktop-Assistant-v%VERSION%-win-x86.zip
) else (
    echo ERROR: Windows x86 build failed
)

:: Windows ARM64
echo.
echo Creating Windows ARM64 installer...
dotnet publish --configuration Release --runtime win-arm64 --self-contained true --output "releases\win-arm64\v%VERSION%" /p:PublishSingleFile=true /p:PublishTrimmed=true
if %errorlevel% equ 0 (
    echo SUCCESS: Windows ARM64 build successful
    powershell -Command "Compress-Archive -Path 'releases\win-arm64\v%VERSION%\*' -DestinationPath 'installers\AION-Desktop-Assistant-v%VERSION%-win-arm64.zip' -Force"
    echo SUCCESS: Windows ARM64 installer created: installers\AION-Desktop-Assistant-v%VERSION%-win-arm64.zip
) else (
    echo ERROR: Windows ARM64 build failed
)

:: Linux x64 (for compatibility)
echo.
echo Creating Linux x64 package...
dotnet publish --configuration Release --runtime linux-x64 --self-contained true --output "releases\linux-x64\v%VERSION%" /p:PublishSingleFile=true /p:PublishTrimmed=true
if %errorlevel% equ 0 (
    echo SUCCESS: Linux x64 build successful
    powershell -Command "Compress-Archive -Path 'releases\linux-x64\v%VERSION%\*' -DestinationPath 'installers\AION-Desktop-Assistant-v%VERSION%-linux-x64.zip' -Force"
    echo SUCCESS: Linux x64 package created: installers\AION-Desktop-Assistant-v%VERSION%-linux-x64.zip
) else (
    echo ERROR: Linux x64 build failed
)

:: macOS x64 (for compatibility)
echo.
echo Creating macOS x64 package...
dotnet publish --configuration Release --runtime osx-x64 --self-contained true --output "releases\osx-x64\v%VERSION%" /p:PublishSingleFile=true /p:PublishTrimmed=true
if %errorlevel% equ 0 (
    echo SUCCESS: macOS x64 build successful
    powershell -Command "Compress-Archive -Path 'releases\osx-x64\v%VERSION%\*' -DestinationPath 'installers\AION-Desktop-Assistant-v%VERSION%-osx-x64.zip' -Force"
    echo SUCCESS: macOS x64 package created: installers\AION-Desktop-Assistant-v%VERSION%-osx-x64.zip
) else (
    echo ERROR: macOS x64 build failed
)

:: Create release info file
echo.
echo Creating release information...
echo AION Desktop Assistant v%VERSION% > "installers\RELEASE-v%VERSION%.txt"
echo Build Date: %DATE% %TIME% >> "installers\RELEASE-v%VERSION%.txt"
echo. >> "installers\RELEASE-v%VERSION%.txt"
echo Available Platforms: >> "installers\RELEASE-v%VERSION%.txt"
echo - Windows x64 (Recommended) >> "installers\RELEASE-v%VERSION%.txt"
echo - Windows x86 (32-bit compatibility) >> "installers\RELEASE-v%VERSION%.txt"
echo - Windows ARM64 (ARM processors) >> "installers\RELEASE-v%VERSION%.txt"
echo - Linux x64 (Cross-platform) >> "installers\RELEASE-v%VERSION%.txt"
echo - macOS x64 (Cross-platform) >> "installers\RELEASE-v%VERSION%.txt"
echo. >> "installers\RELEASE-v%VERSION%.txt"
echo Features: >> "installers\RELEASE-v%VERSION%.txt"
echo - Comprehensive accessibility support >> "installers\RELEASE-v%VERSION%.txt"
echo - Advanced OCR text recognition >> "installers\RELEASE-v%VERSION%.txt"
echo - Voice synthesis and recognition >> "installers\RELEASE-v%VERSION%.txt"
echo - Screen capture and automation >> "installers\RELEASE-v%VERSION%.txt"
echo - Mouse and keyboard automation >> "installers\RELEASE-v%VERSION%.txt"
echo - Window management >> "installers\RELEASE-v%VERSION%.txt"
echo - Structured logging with Serilog >> "installers\RELEASE-v%VERSION%.txt"
echo - Comprehensive test coverage >> "installers\RELEASE-v%VERSION%.txt"

:: Create installation instructions
echo.
echo Creating installation instructions...
echo AION Desktop Assistant - Installation Instructions > "installers\INSTALL-INSTRUCTIONS.txt"
echo ================================================= >> "installers\INSTALL-INSTRUCTIONS.txt"
echo. >> "installers\INSTALL-INSTRUCTIONS.txt"
echo 1. Download the appropriate installer for your platform: >> "installers\INSTALL-INSTRUCTIONS.txt"
echo    - Windows 64-bit: AION-Desktop-Assistant-v%VERSION%-win-x64.zip >> "installers\INSTALL-INSTRUCTIONS.txt"
echo    - Windows 32-bit: AION-Desktop-Assistant-v%VERSION%-win-x86.zip >> "installers\INSTALL-INSTRUCTIONS.txt"
echo    - Windows ARM64: AION-Desktop-Assistant-v%VERSION%-win-arm64.zip >> "installers\INSTALL-INSTRUCTIONS.txt"
echo. >> "installers\INSTALL-INSTRUCTIONS.txt"
echo 2. Extract the downloaded ZIP file to your desired location >> "installers\INSTALL-INSTRUCTIONS.txt"
echo. >> "installers\INSTALL-INSTRUCTIONS.txt"
echo 3. Run AionDesktopAssistant.exe to start the application >> "installers\INSTALL-INSTRUCTIONS.txt"
echo. >> "installers\INSTALL-INSTRUCTIONS.txt"
echo 4. The application will create log files in the 'logs' directory >> "installers\INSTALL-INSTRUCTIONS.txt"
echo. >> "installers\INSTALL-INSTRUCTIONS.txt"
echo System Requirements: >> "installers\INSTALL-INSTRUCTIONS.txt"
echo - Windows 10 or later (Windows 11 recommended) >> "installers\INSTALL-INSTRUCTIONS.txt"
echo - .NET 8 Runtime (included in self-contained builds) >> "installers\INSTALL-INSTRUCTIONS.txt"
echo - Minimum 4GB RAM >> "installers\INSTALL-INSTRUCTIONS.txt"
echo - Screen reader compatibility >> "installers\INSTALL-INSTRUCTIONS.txt"
echo. >> "installers\INSTALL-INSTRUCTIONS.txt"
echo Support: >> "installers\INSTALL-INSTRUCTIONS.txt"
echo - Check logs directory for troubleshooting >> "installers\INSTALL-INSTRUCTIONS.txt"
echo - Application supports voice feedback and accessibility features >> "installers\INSTALL-INSTRUCTIONS.txt"

:: Run tests if requested
echo.
echo Running tests...
dotnet test --configuration Release --no-build --verbosity normal
if %errorlevel% neq 0 (
    echo WARNING: Some tests failed, but installers were created
)

:: Display summary
echo.
echo ===============================================
echo Installer Creation Summary
echo ===============================================
echo Version: %VERSION%
echo Build Date: %DATE% %TIME%
echo.
echo Created installers:
dir /b installers\*.zip 2>nul | findstr . >nul && (
    echo SUCCESS: Installer packages:
    for %%f in (installers\*.zip) do echo   - %%f
) || (
    echo ERROR: No installer packages were created
)
echo.
echo Additional files:
echo   - installers\RELEASE-v%VERSION%.txt
echo   - installers\INSTALL-INSTRUCTIONS.txt
echo.
echo ===============================================
echo Multi-platform installer creation completed!
echo ===============================================
pause