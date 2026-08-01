' Black Browser — Portable YouTube Desktop Launcher
Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
parentDir = fso.GetParentFolderName(scriptDir)
exePath = fso.BuildPath(parentDir, "Black.exe")

If Not fso.FileExists(exePath) Then
    exePath = fso.BuildPath(scriptDir, "Black.exe")
End If

If fso.FileExists(exePath) Then
    WshShell.Run """" & exePath & """ ""https://www.youtube.com""", 1, False
Else
    MsgBox "Black.exe executable not found in " & exePath, 16, "Black Browser Portable Launcher"
End If
