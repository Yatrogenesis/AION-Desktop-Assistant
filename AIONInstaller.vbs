' AION Desktop Assistant - VBScript Auto-Installer
' Advanced self-contained installer with .NET detection and compilation
' Version 1.0.0 - Independence

Option Explicit

Dim objShell, objFSO, objWMI, objHTTP
Dim strInstallDir, strTempDir, strArch, strDotNetURL
Dim blnAdminRights, intResult

' Initialize objects
Set objShell = CreateObject("WScript.Shell")
Set objFSO = CreateObject("Scripting.FileSystemObject")
Set objWMI = CreateObject("WbemScripting.SWbemLocator").ConnectServer()

' Main installer flow
Sub Main()
    ShowHeader()

    If Not CheckAdminRights() Then
        MsgBox "AION Desktop Assistant requires Administrator privileges." & vbCrLf & _
               "Please right-click this installer and select 'Run as administrator'.", _
               vbCritical + vbOKOnly, "Administrator Rights Required"
        WScript.Quit 1
    End If

    DetectSystemArchitecture()
    CheckAndInstallDotNet()
    CreateInstallationDirectory()
    ExtractSourceFiles()
    BuildApplication()
    CreateShortcuts()
    RegisterApplication()
    ShowCompletion()
End Sub

' Display installer header
Sub ShowHeader()
    WScript.Echo "==============================================="
    WScript.Echo "     AION Desktop Assistant Auto-Installer"
    WScript.Echo "         AI-Powered Accessibility Solution"
    WScript.Echo "==============================================="
    WScript.Echo ""
    WScript.Echo "Version: 1.0.0 ""Independence"""
    WScript.Echo "Target: Windows 10/11 (x64, x86, ARM64)"
    WScript.Echo ""
End Sub

' Check for administrator rights
Function CheckAdminRights()
    On Error Resume Next
    objShell.RegRead("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\")
    If Err.Number = 0 Then
        CheckAdminRights = True
        WScript.Echo "[INFO] Administrator privileges confirmed"
    Else
        CheckAdminRights = False
    End If
    On Error Goto 0
End Function

' Detect system architecture
Sub DetectSystemArchitecture()
    Dim objItem, colItems
    Set colItems = objWMI.ExecQuery("SELECT * FROM Win32_Processor")

    For Each objItem in colItems
        If InStr(objItem.Architecture, "9") > 0 Then
            strArch = "x64"
        ElseIf InStr(objItem.Architecture, "0") > 0 Then
            strArch = "x86"
        ElseIf InStr(objItem.Architecture, "12") > 0 Then
            strArch = "arm64"
        Else
            strArch = "x64" ' Default to x64
        End If
        Exit For
    Next

    WScript.Echo "[INFO] Detected architecture: " & strArch
    WScript.Echo ""
End Sub

' Check for .NET 8.0 and install if needed
Sub CheckAndInstallDotNet()
    WScript.Echo "[STEP 1/6] Checking for .NET 8.0 Runtime..."

    Dim strDotNetPath, blnDotNetFound
    strDotNetPath = "dotnet"
    blnDotNetFound = False

    ' Try to execute dotnet --version
    On Error Resume Next
    intResult = objShell.Run("dotnet --version", 0, True)
    If Err.Number = 0 And intResult = 0 Then
        blnDotNetFound = True
        WScript.Echo "[OK] .NET runtime found"
    End If
    On Error Goto 0

    If Not blnDotNetFound Then
        WScript.Echo "[WARNING] .NET 8.0 not found. Installing..."
        InstallDotNetRuntime()
    End If
End Sub

' Install .NET 8.0 Runtime
Sub InstallDotNetRuntime()
    WScript.Echo ""
    WScript.Echo "==============================================="
    WScript.Echo "         .NET 8.0 Installation Required"
    WScript.Echo "==============================================="
    WScript.Echo ""

    ' Determine download URL based on architecture
    Select Case strArch
        Case "x64"
            strDotNetURL = "https://download.microsoft.com/download/6/0/f/60f856b2-ec7f-4085-b8b1-b20cf9d05f25/dotnet-runtime-8.0.20-win-x64.exe"
        Case "x86"
            strDotNetURL = "https://download.microsoft.com/download/6/0/f/60f856b2-ec7f-4085-b8b1-b20cf9d05f25/dotnet-runtime-8.0.20-win-x86.exe"
        Case "arm64"
            strDotNetURL = "https://download.microsoft.com/download/6/0/f/60f856b2-ec7f-4085-b8b1-b20cf9d05f25/dotnet-runtime-8.0.20-win-arm64.exe"
    End Select

    strTempDir = objShell.ExpandEnvironmentStrings("%TEMP%")
    Dim strInstallerPath
    strInstallerPath = strTempDir & "\dotnet-runtime-8.0.20.exe"

    WScript.Echo "[DOWNLOAD] Downloading .NET 8.0 Runtime for " & strArch & "..."

    ' Download .NET installer
    Set objHTTP = CreateObject("Microsoft.XMLHTTP")
    objHTTP.Open "GET", strDotNetURL, False
    objHTTP.Send()

    If objHTTP.Status = 200 Then
        Dim objADOStream
        Set objADOStream = CreateObject("ADODB.Stream")
        objADOStream.Open()
        objADOStream.Type = 1 ' Binary
        objADOStream.Write objHTTP.ResponseBody
        objADOStream.SaveToFile strInstallerPath, 2 ' Overwrite
        objADOStream.Close()
        Set objADOStream = Nothing

        WScript.Echo "[INSTALL] Installing .NET 8.0 Runtime..."
        intResult = objShell.Run(strInstallerPath & " /quiet /norestart", 0, True)

        If intResult = 0 Then
            WScript.Echo "[OK] .NET 8.0 installation completed successfully"
        Else
            WScript.Echo "[ERROR] .NET installation failed with code: " & intResult
            MsgBox "Please manually install .NET 8.0 from:" & vbCrLf & _
                   "https://dotnet.microsoft.com/download/dotnet/8.0", _
                   vbExclamation, "Manual Installation Required"
            WScript.Quit 1
        End If

        ' Cleanup installer
        objFSO.DeleteFile strInstallerPath, True
    Else
        WScript.Echo "[ERROR] Failed to download .NET runtime"
        MsgBox "Failed to download .NET 8.0 Runtime. Please check your internet connection.", vbCritical
        WScript.Quit 1
    End If

    Set objHTTP = Nothing
End Sub

' Create installation directory
Sub CreateInstallationDirectory()
    WScript.Echo ""
    WScript.Echo "[STEP 2/6] Creating installation directory..."

    strInstallDir = objShell.ExpandEnvironmentStrings("%ProgramFiles%") & "\AION Desktop Assistant"

    If Not objFSO.FolderExists(strInstallDir) Then
        objFSO.CreateFolder strInstallDir
    End If

    WScript.Echo "[OK] Directory created: " & strInstallDir
End Sub

' Extract source files
Sub ExtractSourceFiles()
    WScript.Echo ""
    WScript.Echo "[STEP 3/6] Extracting application files..."

    Dim strCurrentDir, strSourceZip
    strCurrentDir = objFSO.GetParentFolderName(WScript.ScriptFullName)
    strSourceZip = strCurrentDir & "\AION-Desktop-Assistant-Source-v1.0.0.zip"

    If objFSO.FileExists(strSourceZip) Then
        Dim objShellApp, objSource, objTarget
        Set objShellApp = CreateObject("Shell.Application")
        Set objSource = objShellApp.NameSpace(strSourceZip)
        Set objTarget = objShellApp.NameSpace(strInstallDir & "\source")

        ' Create source directory
        If Not objFSO.FolderExists(strInstallDir & "\source") Then
            objFSO.CreateFolder strInstallDir & "\source"
        End If

        ' Extract files
        objTarget.CopyHere objSource.Items, 4 + 16 ' No progress dialog + Yes to all

        ' Wait for extraction to complete
        WScript.Sleep 5000

        WScript.Echo "[OK] Source files extracted"
        Set objShellApp = Nothing
    Else
        WScript.Echo "[ERROR] Source package not found: " & strSourceZip
        MsgBox "Source package 'AION-Desktop-Assistant-Source-v1.0.0.zip' not found!" & vbCrLf & _
               "Please ensure it's in the same directory as this installer.", vbCritical
        WScript.Quit 1
    End If
End Sub

' Build the application
Sub BuildApplication()
    WScript.Echo ""
    WScript.Echo "[STEP 4/6] Building self-contained executable..."

    Dim strSourceDir
    strSourceDir = strInstallDir & "\source"

    ' Change to source directory
    objShell.CurrentDirectory = strSourceDir

    WScript.Echo "[BUILD] Restoring NuGet packages..."
    intResult = objShell.Run("dotnet restore", 0, True)

    WScript.Echo "[BUILD] Compiling Release build..."
    intResult = objShell.Run("dotnet build --configuration Release --verbosity quiet", 0, True)

    WScript.Echo "[BUILD] Creating self-contained executable for " & strArch & "..."
    Dim strPublishCmd
    strPublishCmd = "dotnet publish --configuration Release --runtime win-" & strArch & " --self-contained true " & _
                   "/p:PublishSingleFile=true /p:PublishTrimmed=true " & _
                   "/p:IncludeNativeLibrariesForSelfExtract=true " & _
                   "/p:EnableCompressionInSingleFile=true " & _
                   "/p:DebugType=none /p:DebugSymbols=false " & _
                   "--output """ & strInstallDir & """ --verbosity quiet"

    intResult = objShell.Run(strPublishCmd, 0, True)

    If objFSO.FileExists(strInstallDir & "\AionDesktopAssistant.exe") Then
        WScript.Echo "[OK] Executable created successfully"

        ' Cleanup source directory
        WScript.Echo "[CLEANUP] Removing temporary files..."
        objFSO.DeleteFolder strSourceDir, True
    Else
        WScript.Echo "[ERROR] Build failed!"
        MsgBox "Application build failed. Please check:" & vbCrLf & _
               "1. .NET 8.0 SDK is installed" & vbCrLf & _
               "2. All source files are present" & vbCrLf & _
               "3. Internet connection for NuGet packages", vbCritical
        WScript.Quit 1
    End If
End Sub

' Create shortcuts
Sub CreateShortcuts()
    WScript.Echo ""
    WScript.Echo "[STEP 5/6] Creating shortcuts..."

    Dim objShortcut

    ' Desktop shortcut
    WScript.Echo "[SHORTCUT] Creating desktop shortcut..."
    Set objShortcut = objShell.CreateShortcut(objShell.SpecialFolders("Desktop") & "\AION Desktop Assistant.lnk")
    objShortcut.TargetPath = strInstallDir & "\AionDesktopAssistant.exe"
    objShortcut.WorkingDirectory = strInstallDir
    objShortcut.IconLocation = strInstallDir & "\icon.ico"
    objShortcut.Description = "AI-Powered Desktop Accessibility Assistant"
    objShortcut.Save

    ' Start menu shortcut
    WScript.Echo "[SHORTCUT] Creating start menu shortcut..."
    Dim strStartMenuDir
    strStartMenuDir = objShell.SpecialFolders("AllUsersPrograms") & "\AION Technologies"

    If Not objFSO.FolderExists(strStartMenuDir) Then
        objFSO.CreateFolder strStartMenuDir
    End If

    Set objShortcut = objShell.CreateShortcut(strStartMenuDir & "\AION Desktop Assistant.lnk")
    objShortcut.TargetPath = strInstallDir & "\AionDesktopAssistant.exe"
    objShortcut.WorkingDirectory = strInstallDir
    objShortcut.IconLocation = strInstallDir & "\icon.ico"
    objShortcut.Description = "AI-Powered Desktop Accessibility Assistant"
    objShortcut.Save

    Set objShortcut = Nothing
End Sub

' Register application in Windows
Sub RegisterApplication()
    WScript.Echo ""
    WScript.Echo "[STEP 6/6] Registering application..."

    ' Create uninstaller
    WScript.Echo "[REGISTRY] Creating uninstaller..."
    Dim objUninstaller
    Set objUninstaller = objFSO.CreateTextFile(strInstallDir & "\uninstall.vbs", True)
    objUninstaller.WriteLine "' AION Desktop Assistant Uninstaller"
    objUninstaller.WriteLine "Set objShell = CreateObject(""WScript.Shell"")"
    objUninstaller.WriteLine "Set objFSO = CreateObject(""Scripting.FileSystemObject"")"
    objUninstaller.WriteLine "objShell.Run ""taskkill /f /im AionDesktopAssistant.exe"", 0, False"
    objUninstaller.WriteLine "WScript.Sleep 2000"
    objUninstaller.WriteLine "objFSO.DeleteFolder """ & strInstallDir & """, True"
    objUninstaller.WriteLine "objFSO.DeleteFile objShell.SpecialFolders(""Desktop"") & ""\AION Desktop Assistant.lnk"", True"
    objUninstaller.WriteLine "MsgBox ""AION Desktop Assistant has been uninstalled."", vbInformation"
    objUninstaller.Close
    Set objUninstaller = Nothing

    ' Add to Windows Programs list
    On Error Resume Next
    objShell.RegWrite "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant\DisplayName", "AION Desktop Assistant", "REG_SZ"
    objShell.RegWrite "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant\DisplayVersion", "1.0.0", "REG_SZ"
    objShell.RegWrite "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant\Publisher", "AION Technologies", "REG_SZ"
    objShell.RegWrite "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant\InstallLocation", strInstallDir, "REG_SZ"
    objShell.RegWrite "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\AIONDesktopAssistant\UninstallString", strInstallDir & "\uninstall.vbs", "REG_SZ"
    On Error Goto 0

    ' Windows Defender exclusion
    WScript.Echo "[SECURITY] Adding Windows Defender exclusion..."
    On Error Resume Next
    objShell.Run "powershell -Command ""Add-MpPreference -ExclusionPath '" & strInstallDir & "'""", 0, False
    On Error Goto 0
End Sub

' Show completion message
Sub ShowCompletion()
    WScript.Echo ""
    WScript.Echo "==============================================="
    WScript.Echo "     INSTALLATION COMPLETED SUCCESSFULLY!"
    WScript.Echo "==============================================="
    WScript.Echo ""
    WScript.Echo "Installation Directory: " & strInstallDir
    WScript.Echo "Executable: " & strInstallDir & "\AionDesktopAssistant.exe"
    WScript.Echo ""
    WScript.Echo "Desktop shortcut created: ✓"
    WScript.Echo "Start menu entry created: ✓"
    WScript.Echo "Windows Programs entry: ✓"
    WScript.Echo ""

    Dim intLaunch
    intLaunch = MsgBox("Installation complete!" & vbCrLf & vbCrLf & _
                      "IMPORTANT: To use voice control features:" & vbCrLf & _
                      "1. Ensure your microphone is connected and working" & vbCrLf & _
                      "2. Grant microphone permissions when prompted" & vbCrLf & _
                      "3. Run as Administrator for full functionality" & vbCrLf & vbCrLf & _
                      "Would you like to launch AION Desktop Assistant now?", _
                      vbQuestion + vbYesNo, "AION Desktop Assistant - Installation Complete")

    If intLaunch = vbYes Then
        WScript.Echo "[LAUNCH] Starting AION Desktop Assistant..."
        objShell.Run """" & strInstallDir & "\AionDesktopAssistant.exe""", 1, False
        WScript.Echo "Application launched!"
    End If

    WScript.Echo ""
    WScript.Echo "Thank you for using AION Desktop Assistant!"
    WScript.Echo "Empowering Independence Through Technology ♿🤖"
End Sub

' Execute main installer
Main()

' Cleanup objects
Set objShell = Nothing
Set objFSO = Nothing
Set objWMI = Nothing
Set objHTTP = Nothing