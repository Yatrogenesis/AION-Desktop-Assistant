# AION Desktop Assistant - Multi-Platform Build Script
# Builds self-contained executables for all supported platforms

$version = "1.0.0"
$projectName = "AionDesktopAssistant"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "AION Desktop Assistant Build v$version" -ForegroundColor Cyan
Write-Host "Multi-Platform Self-Contained Build" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Check for .NET SDK
$dotnetVersion = dotnet --version 2>$null
if (-not $?) {
    Write-Host "ERROR: .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
}

Write-Host "`n.NET SDK Version: $dotnetVersion" -ForegroundColor Green

# Define platforms
$platforms = @(
    @{
        Name = "Windows x64"
        Runtime = "win-x64"
        OutputDir = "publish\win-x64"
    },
    @{
        Name = "Windows x86"
        Runtime = "win-x86"
        OutputDir = "publish\win-x86"
    },
    @{
        Name = "Windows ARM64"
        Runtime = "win-arm64"
        OutputDir = "publish\win-arm64"
    }
)

# Clean previous builds
Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "publish") {
    Remove-Item -Path "publish" -Recurse -Force
}
if (Test-Path "release") {
    Remove-Item -Path "release" -Recurse -Force
}

# Create directories
New-Item -ItemType Directory -Force -Path "publish" | Out-Null
New-Item -ItemType Directory -Force -Path "release" | Out-Null

# Build for each platform
foreach ($platform in $platforms) {
    Write-Host "`n=====================================" -ForegroundColor Cyan
    Write-Host "Building for $($platform.Name)" -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan

    $outputPath = $platform.OutputDir
    $runtime = $platform.Runtime

    # Build command with all optimizations
    $buildArgs = @(
        "publish",
        "--configuration", "Release",
        "--runtime", $runtime,
        "--self-contained", "true",
        "/p:PublishSingleFile=true",
        "/p:PublishTrimmed=true",
        "/p:IncludeNativeLibrariesForSelfExtract=true",
        "/p:EnableCompressionInSingleFile=true",
        "/p:DebugType=none",
        "/p:DebugSymbols=false",
        "--output", $outputPath
    )

    Write-Host "Executing: dotnet $($buildArgs -join ' ')" -ForegroundColor Gray

    & dotnet $buildArgs

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Build successful for $($platform.Name)" -ForegroundColor Green

        # Copy additional files
        Write-Host "Copying additional files..." -ForegroundColor Yellow
        Copy-Item "README.md" -Destination "$outputPath\" -Force
        Copy-Item "icon.ico" -Destination "$outputPath\" -Force
        Copy-Item "version.json" -Destination "$outputPath\" -Force

        # Create tessdata directory if needed
        if (Test-Path "tessdata") {
            Copy-Item "tessdata" -Destination "$outputPath\" -Recurse -Force
        }

        # Create ZIP package
        $zipName = "$projectName-v$version-$($runtime).zip"
        $zipPath = "release\$zipName"

        Write-Host "Creating ZIP package: $zipName" -ForegroundColor Yellow

        # Use PowerShell compression
        Compress-Archive -Path "$outputPath\*" -DestinationPath $zipPath -CompressionLevel Optimal -Force

        Write-Host "✓ Package created: $zipPath" -ForegroundColor Green

        # Calculate hash
        $hash = Get-FileHash $zipPath -Algorithm SHA256
        Write-Host "SHA256: $($hash.Hash)" -ForegroundColor Gray

    } else {
        Write-Host "✗ Build failed for $($platform.Name)" -ForegroundColor Red
    }
}

# Create portable installer script
$installerScript = @'
@echo off
title AION Desktop Assistant - Portable Installer
echo =====================================
echo AION Desktop Assistant Installation
echo =====================================
echo.
echo This will install AION Desktop Assistant to:
echo %ProgramFiles%\AION Desktop Assistant
echo.
pause

set INSTALL_DIR=%ProgramFiles%\AION Desktop Assistant

echo Creating installation directory...
mkdir "%INSTALL_DIR%" 2>nul

echo Copying files...
xcopy /E /Y /I *.* "%INSTALL_DIR%"

echo Creating desktop shortcut...
powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\AION Desktop Assistant.lnk'); $Shortcut.TargetPath = '%INSTALL_DIR%\AionDesktopAssistant.exe'; $Shortcut.IconLocation = '%INSTALL_DIR%\icon.ico'; $Shortcut.Save()"

echo.
echo =====================================
echo Installation Complete!
echo =====================================
echo.
echo Desktop shortcut created.
echo Run as Administrator for full functionality.
echo.
pause
'@

$installerScript | Out-File -FilePath "release\install-portable.bat" -Encoding ASCII

# Create version manifest
$manifest = @{
    Version = $version
    BuildDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    Platforms = $platforms | ForEach-Object { $_.Runtime }
    Files = Get-ChildItem "release\*.zip" | ForEach-Object {
        @{
            Name = $_.Name
            Size = $_.Length
            Hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
        }
    }
}

$manifest | ConvertTo-Json -Depth 10 | Out-File -FilePath "release\manifest.json" -Encoding UTF8

# Summary
Write-Host "`n=====================================" -ForegroundColor Cyan
Write-Host "BUILD COMPLETE!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan

Write-Host "`nPackages created:" -ForegroundColor Yellow
Get-ChildItem "release\*.zip" | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  - $($_.Name) ($size MB)" -ForegroundColor White
}

Write-Host "`nRelease directory: $(Resolve-Path 'release')" -ForegroundColor Yellow
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "  1. Test each platform executable" -ForegroundColor White
Write-Host "  2. Upload to GitHub Releases" -ForegroundColor White
Write-Host "  3. Update documentation" -ForegroundColor White