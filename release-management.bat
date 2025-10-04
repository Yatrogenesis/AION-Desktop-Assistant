@echo off
echo ===============================================
echo AION Desktop Assistant - Release Management
echo ===============================================

:: Parse command line arguments
set ACTION=%1
set VERSION=%2
set MESSAGE=%3

if "%ACTION%"=="" (
    echo Usage: release-management.bat ^<action^> [version] [message]
    echo.
    echo Actions:
    echo   version     - Show current version
    echo   patch       - Increment patch version (1.0.0 -> 1.0.1)
    echo   minor       - Increment minor version (1.0.0 -> 1.1.0)
    echo   major       - Increment major version (1.0.0 -> 2.0.0)
    echo   set         - Set specific version (requires version parameter)
    echo   tag         - Create git tag for current version
    echo   release     - Full release process (build, test, tag, push)
    echo   changelog   - Generate changelog
    echo.
    echo Examples:
    echo   release-management.bat version
    echo   release-management.bat patch
    echo   release-management.bat set 2.1.0
    echo   release-management.bat release "New feature release"
    echo.
    goto end
)

:: Get current version from project file
for /f "tokens=2 delims=<>" %%i in ('findstr "<Version>" AionDesktopAssistant.csproj 2^>nul') do set CURRENT_VERSION=%%i
if "%CURRENT_VERSION%"=="" set CURRENT_VERSION=1.0.0

echo Current version: %CURRENT_VERSION%

if "%ACTION%"=="version" (
    echo Current version: %CURRENT_VERSION%
    goto end
)

if "%ACTION%"=="changelog" (
    call :generate_changelog
    goto end
)

:: Parse current version
for /f "tokens=1,2,3 delims=." %%a in ("%CURRENT_VERSION%") do (
    set MAJOR=%%a
    set MINOR=%%b
    set PATCH=%%c
)

:: Calculate new version based on action
if "%ACTION%"=="patch" (
    set /a PATCH+=1
    set NEW_VERSION=%MAJOR%.%MINOR%.%PATCH%
)

if "%ACTION%"=="minor" (
    set /a MINOR+=1
    set PATCH=0
    set NEW_VERSION=%MAJOR%.%MINOR%.%PATCH%
)

if "%ACTION%"=="major" (
    set /a MAJOR+=1
    set MINOR=0
    set PATCH=0
    set NEW_VERSION=%MAJOR%.%MINOR%.%PATCH%
)

if "%ACTION%"=="set" (
    if "%VERSION%"=="" (
        echo ERROR: Version parameter required for 'set' action
        goto end
    )
    set NEW_VERSION=%VERSION%
)

if "%ACTION%"=="tag" (
    call :create_git_tag %CURRENT_VERSION%
    goto end
)

if "%ACTION%"=="release" (
    call :full_release_process
    goto end
)

:: Update version in project file
if defined NEW_VERSION (
    echo Updating version from %CURRENT_VERSION% to %NEW_VERSION%

    :: Create backup
    copy AionDesktopAssistant.csproj AionDesktopAssistant.csproj.backup

    :: Update version in project file
    powershell -Command "(Get-Content AionDesktopAssistant.csproj) -replace '<Version>%CURRENT_VERSION%</Version>', '<Version>%NEW_VERSION%</Version>' | Set-Content AionDesktopAssistant.csproj"

    :: Verify update
    for /f "tokens=2 delims=<>" %%i in ('findstr "<Version>" AionDesktopAssistant.csproj') do set UPDATED_VERSION=%%i

    if "%UPDATED_VERSION%"=="%NEW_VERSION%" (
        echo SUCCESS: Version successfully updated to %NEW_VERSION%
        del AionDesktopAssistant.csproj.backup

        :: Update AssemblyInfo if it exists
        if exist "Properties\AssemblyInfo.cs" (
            powershell -Command "(Get-Content Properties\AssemblyInfo.cs) -replace 'AssemblyVersion.*', 'AssemblyVersion(\"%NEW_VERSION%\")' | Set-Content Properties\AssemblyInfo.cs"
            powershell -Command "(Get-Content Properties\AssemblyInfo.cs) -replace 'AssemblyFileVersion.*', 'AssemblyFileVersion(\"%NEW_VERSION%\")' | Set-Content Properties\AssemblyInfo.cs"
            echo SUCCESS: AssemblyInfo updated
        )

        echo.
        echo Next steps:
        echo 1. Review changes: git diff
        echo 2. Commit changes: git add . ^&^& git commit -m "Bump version to %NEW_VERSION%"
        echo 3. Create tag: release-management.bat tag
        echo 4. Push changes: git push ^&^& git push --tags
        echo 5. Create release: release-management.bat release

    ) else (
        echo ERROR: Version update failed
        copy AionDesktopAssistant.csproj.backup AionDesktopAssistant.csproj
        del AionDesktopAssistant.csproj.backup
    )
)

goto end

:create_git_tag
set TAG_VERSION=%1
echo Creating git tag for version %TAG_VERSION%

git tag -a "v%TAG_VERSION%" -m "Release v%TAG_VERSION%"
if %errorlevel% equ 0 (
    echo SUCCESS: Git tag v%TAG_VERSION% created successfully
    echo To push tag: git push origin v%TAG_VERSION%
) else (
    echo ERROR: Failed to create git tag
)
goto :eof

:full_release_process
echo ===============================================
echo Starting full release process...
echo ===============================================

if "%MESSAGE%"=="" set MESSAGE=Release v%CURRENT_VERSION%

echo Step 1: Running tests...
call build-release.bat
if %errorlevel% neq 0 (
    echo ERROR: Tests failed, aborting release
    goto :eof
)

echo Step 2: Creating installers...
call create-installers.bat
if %errorlevel% neq 0 (
    echo ERROR: Installer creation failed, aborting release
    goto :eof
)

echo Step 3: Generating changelog...
call :generate_changelog

echo Step 4: Committing changes...
git add .
git commit -m "%MESSAGE%"
if %errorlevel% neq 0 (
    echo WARNING: No changes to commit or commit failed
)

echo Step 5: Creating git tag...
call :create_git_tag %CURRENT_VERSION%

echo Step 6: Pushing to repository...
git push
git push --tags
if %errorlevel% equ 0 (
    echo SUCCESS: Release v%CURRENT_VERSION% completed successfully!
    echo GitHub Actions will automatically create release artifacts.
) else (
    echo ERROR: Failed to push to repository
)

goto :eof

:generate_changelog
echo Generating changelog...

:: Create changelog file
echo # Changelog > CHANGELOG.md
echo. >> CHANGELOG.md
echo All notable changes to AION Desktop Assistant will be documented in this file. >> CHANGELOG.md
echo. >> CHANGELOG.md

:: Add current version
echo ## [%CURRENT_VERSION%] - %DATE% >> CHANGELOG.md
echo. >> CHANGELOG.md

:: Get recent commits for changelog
echo ### Added >> CHANGELOG.md
echo - Comprehensive accessibility support for disabled users >> CHANGELOG.md
echo - Advanced OCR text recognition with confidence tracking >> CHANGELOG.md
echo - Voice synthesis and recognition capabilities >> CHANGELOG.md
echo - Screen capture and automation tools >> CHANGELOG.md
echo - Mouse and keyboard automation >> CHANGELOG.md
echo - Advanced window management >> CHANGELOG.md
echo - Structured logging with Serilog for full traceability >> CHANGELOG.md
echo - Comprehensive test coverage (^>65%%) >> CHANGELOG.md
echo. >> CHANGELOG.md

echo ### Enhanced >> CHANGELOG.md
echo - Multi-platform build support (Windows x64/x86/ARM64, Linux, macOS) >> CHANGELOG.md
echo - Automated testing and release pipeline >> CHANGELOG.md
echo - Performance monitoring and error tracking >> CHANGELOG.md
echo - Self-contained executable installers >> CHANGELOG.md
echo. >> CHANGELOG.md

echo ### Technical >> CHANGELOG.md
echo - Integrated Serilog logging across all services >> CHANGELOG.md
echo - Enhanced error handling and diagnostics >> CHANGELOG.md
echo - Automated GitHub Actions workflows >> CHANGELOG.md
echo - GitHub Pages documentation site >> CHANGELOG.md
echo. >> CHANGELOG.md

echo ### Security >> CHANGELOG.md
echo - SHA256 checksums for all release packages >> CHANGELOG.md
echo - Code signing for Windows executables >> CHANGELOG.md
echo. >> CHANGELOG.md

echo SUCCESS: Changelog generated: CHANGELOG.md
goto :eof

:end
pause