@echo off
echo ===============================================
echo AION Desktop Assistant - Release Package v1.0.0
echo Creating downloadable installer without .NET SDK
echo ===============================================

echo.
echo Creating release directory...
mkdir release 2>nul

echo Creating source code package...
7z a -tzip release\AION-Desktop-Assistant-Source-v1.0.0.zip ^
    *.cs *.xaml *.csproj *.md *.json *.bat *.ps1 ^
    Services\ docs\ installer\ .github\ ^
    -x!bin -x!obj -x!release -x!publish -x!.git

if %errorlevel% neq 0 (
    echo Creating ZIP with PowerShell...
    powershell -Command "Compress-Archive -Path @('*.cs','*.xaml','*.csproj','*.md','*.json','*.bat','*.ps1','Services','docs','installer','.github') -DestinationPath 'release\AION-Desktop-Assistant-Source-v1.0.0.zip' -CompressionLevel Optimal -Force"
)

echo.
echo Creating installation instructions...
echo # AION Desktop Assistant v1.0.0 - Installation Package > release\INSTALLATION.md
echo. >> release\INSTALLATION.md
echo ## Quick Start Guide >> release\INSTALLATION.md
echo. >> release\INSTALLATION.md
echo 1. **Install .NET 8.0 SDK** >> release\INSTALLATION.md
echo    - Download from: https://dotnet.microsoft.com/download/dotnet/8.0 >> release\INSTALLATION.md
echo    - Choose ".NET 8.0 SDK" for your platform >> release\INSTALLATION.md
echo. >> release\INSTALLATION.md
echo 2. **Build the Application** >> release\INSTALLATION.md
echo    - Extract this ZIP file >> release\INSTALLATION.md
echo    - Run `build.bat` as Administrator >> release\INSTALLATION.md
echo    - Or run: `dotnet build --configuration Release` >> release\INSTALLATION.md
echo. >> release\INSTALLATION.md
echo 3. **Create Executable** >> release\INSTALLATION.md
echo    - Run `publish-all-platforms.ps1` in PowerShell >> release\INSTALLATION.md
echo    - Or run: `dotnet publish --configuration Release --runtime win-x64 --self-contained` >> release\INSTALLATION.md
echo. >> release\INSTALLATION.md
echo 4. **Run Application** >> release\INSTALLATION.md
echo    - Navigate to `bin\Release\net8.0-windows\win-x64\publish\` >> release\INSTALLATION.md
echo    - Run `AionDesktopAssistant.exe` as Administrator >> release\INSTALLATION.md
echo. >> release\INSTALLATION.md
echo ## Features >> release\INSTALLATION.md
echo - 🎤 Complete voice control >> release\INSTALLATION.md
echo - 👁️ Screen reading with OCR >> release\INSTALLATION.md
echo - 🖱️ Mouse and keyboard automation >> release\INSTALLATION.md
echo - ♿ Accessibility for disabled users >> release\INSTALLATION.md
echo. >> release\INSTALLATION.md
echo ## Support >> release\INSTALLATION.md
echo - GitHub: https://github.com/Yatrogenesis/AION-Desktop-Assistant >> release\INSTALLATION.md
echo - Issues: https://github.com/Yatrogenesis/AION-Desktop-Assistant/issues >> release\INSTALLATION.md

echo.
echo Creating version manifest...
echo { > release\version.json
echo   "version": "1.0.0", >> release\version.json
echo   "releaseDate": "%date%", >> release\version.json
echo   "buildType": "source", >> release\version.json
echo   "requiresDotNet": true, >> release\version.json
echo   "dotNetVersion": "8.0", >> release\version.json
echo   "platforms": ["win-x64", "win-x86", "win-arm64"], >> release\version.json
echo   "features": { >> release\version.json
echo     "voiceControl": true, >> release\version.json
echo     "screenReading": true, >> release\version.json
echo     "automation": true, >> release\version.json
echo     "accessibility": true >> release\version.json
echo   } >> release\version.json
echo } >> release\version.json

echo.
echo Creating quick start script...
echo @echo off > release\BUILD-AND-RUN.bat
echo echo ======================================= >> release\BUILD-AND-RUN.bat
echo echo AION Desktop Assistant - Quick Builder >> release\BUILD-AND-RUN.bat
echo echo ======================================= >> release\BUILD-AND-RUN.bat
echo echo. >> release\BUILD-AND-RUN.bat
echo echo Checking for .NET SDK... >> release\BUILD-AND-RUN.bat
echo where dotnet ^>nul 2^>nul >> release\BUILD-AND-RUN.bat
echo if %%errorlevel%% neq 0 ^( >> release\BUILD-AND-RUN.bat
echo     echo ERROR: .NET 8.0 SDK required! >> release\BUILD-AND-RUN.bat
echo     echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0 >> release\BUILD-AND-RUN.bat
echo     pause >> release\BUILD-AND-RUN.bat
echo     exit /b 1 >> release\BUILD-AND-RUN.bat
echo ^) >> release\BUILD-AND-RUN.bat
echo echo .NET SDK found! >> release\BUILD-AND-RUN.bat
echo echo. >> release\BUILD-AND-RUN.bat
echo echo Building application... >> release\BUILD-AND-RUN.bat
echo dotnet build --configuration Release >> release\BUILD-AND-RUN.bat
echo echo. >> release\BUILD-AND-RUN.bat
echo echo Creating executable... >> release\BUILD-AND-RUN.bat
echo dotnet publish --configuration Release --runtime win-x64 --self-contained >> release\BUILD-AND-RUN.bat
echo echo. >> release\BUILD-AND-RUN.bat
echo echo ======================================= >> release\BUILD-AND-RUN.bat
echo echo BUILD COMPLETE! >> release\BUILD-AND-RUN.bat
echo echo ======================================= >> release\BUILD-AND-RUN.bat
echo echo Executable location: >> release\BUILD-AND-RUN.bat
echo echo bin\Release\net8.0-windows\win-x64\publish\AionDesktopAssistant.exe >> release\BUILD-AND-RUN.bat
echo pause >> release\BUILD-AND-RUN.bat

echo.
echo ===============================================
echo RELEASE PACKAGE CREATED SUCCESSFULLY!
echo ===============================================
echo.
echo Files created in release\ directory:
dir release\ /b
echo.
echo Package ready for distribution!
echo Users will need to install .NET 8.0 SDK to build.
echo.
pause