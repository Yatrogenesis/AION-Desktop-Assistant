@echo off
title AION Desktop Assistant - Auto Installer v1.0.0
color 0B

echo.
echo ===============================================
echo     AION Desktop Assistant Auto-Installer
echo         AI-Powered Accessibility Solution
echo ===============================================
echo.
echo Version: 1.0.0 "Independence"
echo Target: Windows 10/11 (x64, x86, ARM64)
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] This installer requires Administrator privileges!
    echo Please right-click and select "Run as Administrator"
    echo.
    pause
    exit /b 1
)

echo [INFO] Administrator privileges confirmed
echo.

REM Detect system architecture
set ARCH=x64
if defined PROCESSOR_ARCHITEW6432 (
    set ARCH=%PROCESSOR_ARCHITEW6432%
) else (
    set ARCH=%PROCESSOR_ARCHITECTURE%
)

if /i "%ARCH%"=="AMD64" set ARCH=x64
if /i "%ARCH%"=="x86" set ARCH=x86
if /i "%ARCH%"=="ARM64" set ARCH=arm64

echo [INFO] Detected architecture: %ARCH%
echo.

REM Check for .NET 8.0
echo [STEP 1/5] Checking for .NET 8.0 Runtime...
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [WARNING] .NET 8.0 not found. Installing...
    call :InstallDotNet
) else (
    echo [OK] .NET found, checking version...
    for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
    echo [INFO] .NET Version: %DOTNET_VERSION%
)

REM Create installation directory
echo.
echo [STEP 2/5] Creating installation directory...
set INSTALL_DIR=%ProgramFiles%\AION Desktop Assistant
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
echo [OK] Directory created: %INSTALL_DIR%

REM Extract source files
echo.
echo [STEP 3/5] Extracting application files...
if exist "%~dp0AION-Desktop-Assistant-Source-v1.0.0.zip" (
    powershell -Command "Expand-Archive -Path '%~dp0AION-Desktop-Assistant-Source-v1.0.0.zip' -DestinationPath '%INSTALL_DIR%\source' -Force"
    echo [OK] Source files extracted
) else (
    echo [ERROR] Source package not found!
    echo Please ensure AION-Desktop-Assistant-Source-v1.0.0.zip is in the same directory
    pause
    exit /b 1
)

REM Build application
echo.
echo [STEP 4/5] Building self-contained executable...
cd /d "%INSTALL_DIR%\source"

echo [BUILD] Restoring NuGet packages...
dotnet restore >nul 2>&1

echo [BUILD] Compiling Release build...
dotnet build --configuration Release --verbosity quiet >nul 2>&1

echo [BUILD] Creating self-contained executable for %ARCH%...
dotnet publish --configuration Release --runtime win-%ARCH% --self-contained true ^
    /p:PublishSingleFile=true ^
    /p:PublishTrimmed=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:EnableCompressionInSingleFile=true ^
    /p:DebugType=none ^
    /p:DebugSymbols=false ^
    --output "%INSTALL_DIR%" ^
    --verbosity quiet >nul 2>&1

if exist "%INSTALL_DIR%\AionDesktopAssistant.exe" (
    echo [OK] Executable created successfully
) else (
    echo [ERROR] Build failed! Check if .NET 8.0 SDK is installed
    pause
    exit /b 1
)

REM Create shortcuts and registry entries
echo.
echo [STEP 5/5] Creating shortcuts and registry entries...

REM Desktop shortcut
echo [SHORTCUT] Creating desktop shortcut...
powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\AION Desktop Assistant.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\AionDesktopAssistant.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.IconLocation = '%INSTALL_DIR%\icon.ico'; $Shortcut.Description = 'AI-Powered Desktop Accessibility Assistant'; $Shortcut.Save()"

REM Start menu shortcut
echo [SHORTCUT] Creating start menu shortcut...
if not exist "%ProgramData%\Microsoft\Windows\Start Menu\Programs\AION Technologies" mkdir "%ProgramData%\Microsoft\Windows\Start Menu\Programs\AION Technologies"
powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%ProgramData%\Microsoft\Windows\Start Menu\Programs\AION Technologies\AION Desktop Assistant.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\AionDesktopAssistant.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.IconLocation = '%INSTALL_DIR%\icon.ico'; $Shortcut.Description = 'AI-Powered Desktop Accessibility Assistant'; $Shortcut.Save()"

REM Uninstaller
echo [REGISTRY] Creating uninstaller...
echo @echo off > "%INSTALL_DIR%\uninstall.bat"
echo echo Uninstalling AION Desktop Assistant... >> "%INSTALL_DIR%\uninstall.bat"
echo taskkill /f /im AionDesktopAssistant.exe 2^>nul >> "%INSTALL_DIR%\uninstall.bat"
echo rd /s /q "%INSTALL_DIR%" >> "%INSTALL_DIR%\uninstall.bat"
echo del "%USERPROFILE%\Desktop\AION Desktop Assistant.lnk" 2^>nul >> "%INSTALL_DIR%\uninstall.bat"
echo del "%ProgramData%\Microsoft\Windows\Start Menu\Programs\AION Technologies\AION Desktop Assistant.lnk" 2^>nul >> "%INSTALL_DIR%\uninstall.bat"
echo echo AION Desktop Assistant has been uninstalled. >> "%INSTALL_DIR%\uninstall.bat"

REM Add to Windows Programs list
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant" /v "DisplayName" /t REG_SZ /d "AION Desktop Assistant" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant" /v "DisplayVersion" /t REG_SZ /d "1.0.0" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant" /v "Publisher" /t REG_SZ /d "AION Technologies" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant" /v "InstallLocation" /t REG_SZ /d "%INSTALL_DIR%" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant" /v "UninstallString" /t REG_SZ /d "%INSTALL_DIR%\uninstall.bat" /f >nul 2>&1

REM Windows Defender exclusion for better performance
echo [SECURITY] Adding Windows Defender exclusion...
powershell -Command "Add-MpPreference -ExclusionPath '%INSTALL_DIR%'" >nul 2>&1

REM Cleanup source files
echo [CLEANUP] Removing temporary files...
rd /s /q "%INSTALL_DIR%\source" >nul 2>&1

echo.
echo ===============================================
echo     INSTALLATION COMPLETED SUCCESSFULLY!
echo ===============================================
echo.
echo Installation Directory: %INSTALL_DIR%
echo Executable: %INSTALL_DIR%\AionDesktopAssistant.exe
echo.
echo Desktop shortcut created: ✓
echo Start menu entry created: ✓
echo Windows Programs entry: ✓
echo.
echo [IMPORTANT] To use voice control features:
echo 1. Ensure your microphone is connected and working
echo 2. Grant microphone permissions when prompted
echo 3. Run the application as Administrator for full functionality
echo.
echo Voice Commands Available:
echo - "Read screen" - Read visible text
echo - "Click [element]" - Click on UI elements
echo - "Switch to [window]" - Change active window
echo - "Type [text]" - Input text by voice
echo - "Take screenshot" - Capture screen
echo - "Help" - Get commands list
echo.

REM Ask to launch application
set /p LAUNCH="Launch AION Desktop Assistant now? (Y/N): "
if /i "%LAUNCH%"=="Y" (
    echo.
    echo [LAUNCH] Starting AION Desktop Assistant...
    start "" "%INSTALL_DIR%\AionDesktopAssistant.exe"
    echo.
    echo Application launched! Check the desktop for the window.
)

echo.
echo Thank you for using AION Desktop Assistant!
echo Empowering Independence Through Technology ♿🤖
echo.
pause
exit /b 0

:InstallDotNet
echo.
echo ===============================================
echo         .NET 8.0 Installation Required
echo ===============================================
echo.
echo AION Desktop Assistant requires .NET 8.0 Runtime.
echo.
echo Downloading .NET 8.0 Runtime installer...
powershell -Command "Invoke-WebRequest -Uri 'https://download.microsoft.com/download/6/0/f/60f856b2-ec7f-4085-b8b1-b20cf9d05f25/dotnet-runtime-8.0.20-win-%ARCH%.exe' -OutFile '%TEMP%\dotnet-runtime-8.0.20.exe'"

if exist "%TEMP%\dotnet-runtime-8.0.20.exe" (
    echo Installing .NET 8.0 Runtime...
    "%TEMP%\dotnet-runtime-8.0.20.exe" /quiet /norestart

    echo Waiting for installation to complete...
    timeout /t 30 /nobreak >nul

    REM Refresh PATH
    call :RefreshPath

    echo [OK] .NET 8.0 installation completed
    del "%TEMP%\dotnet-runtime-8.0.20.exe" >nul 2>&1
) else (
    echo [ERROR] Failed to download .NET runtime
    echo Please manually install .NET 8.0 from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
goto :eof

:RefreshPath
REM Refresh environment variables
for /f "tokens=2*" %%a in ('reg query "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment" /v PATH 2^>nul') do set SysPath=%%b
for /f "tokens=2*" %%a in ('reg query "HKCU\Environment" /v PATH 2^>nul') do set UserPath=%%b
set PATH=%SysPath%;%UserPath%
goto :eof