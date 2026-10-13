# Dot-source (`. "$PSScriptRoot/quiet-start.ps1"`), then Start-Quiet in place of Start-Process for test runs of the game.
# Richard, 3 Oct: test windows opening (shown or hidden) took the focus from the game he was playing. Start-Quiet creates
# the process with STARTF_USESHOWWINDOW: SW_SHOWNOACTIVATE (shown, not activated), SW_SHOWMINNOACTIVE (-WindowStyle
# Minimized) or SW_HIDE (-WindowStyle Hidden, as before), so its first window doesn't take the foreground. For the first
# seconds it also hands the foreground back to the window that had it, if the game took it anyway (Unity can activate
# its own window as it starts).
# Returns a System.Diagnostics.Process, like Start-Process -PassThru.
if(-not ('LittleRoad.QuietStart' -as [type])) {
Add-Type -TypeDefinition @'
using System;using System.Runtime.InteropServices;using System.Text;
namespace LittleRoad {
public static class QuietStart {
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct STARTUPINFO{public int cb;public string lpReserved,lpDesktop,lpTitle;public int dwX,dwY,dwXSize,dwYSize,dwXCountChars,dwYCountChars,dwFillAttribute,dwFlags;public short wShowWindow,cbReserved2;public IntPtr lpReserved2,hStdInput,hStdOutput,hStdError;}
    [StructLayout(LayoutKind.Sequential)] struct PROCESS_INFORMATION{public IntPtr hProcess,hThread;public int dwProcessId,dwThreadId;}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool CreateProcess(string app,StringBuilder cmd,IntPtr pa,IntPtr ta,bool inherit,uint flags,IntPtr env,string dir,ref STARTUPINFO si,out PROCESS_INFORMATION pi);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint a,uint b,bool attach);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr h);
    public static int Start(string exe,string args,string dir,short show){
        var si=new STARTUPINFO();si.cb=Marshal.SizeOf(si);si.dwFlags=1;si.wShowWindow=show;
        PROCESS_INFORMATION pi;var cmd=new StringBuilder("\""+exe+"\" "+args);
        if(!CreateProcess(exe,cmd,IntPtr.Zero,IntPtr.Zero,false,0,IntPtr.Zero,dir,ref si,out pi))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        CloseHandle(pi.hThread);CloseHandle(pi.hProcess);return pi.dwProcessId;}
    public static int OwnerOfForeground(){uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);return (int)pid;}
    // Back to `previous`: attach to the current foreground thread's input so Windows lets us set it.
    public static void GiveBack(IntPtr previous){
        uint pid;uint fg=GetWindowThreadProcessId(GetForegroundWindow(),out pid),me=GetCurrentThreadId();
        AttachThreadInput(me,fg,true);try{BringWindowToTop(previous);SetForegroundWindow(previous);}finally{AttachThreadInput(me,fg,false);}
    }
}}
'@
}
function Start-Quiet {
    param([Parameter(Mandatory)][string]$FilePath,[object[]]$ArgumentList=@(),[string]$WindowStyle='Normal',[switch]$PassThru,[string]$WorkingDirectory)
    $exe=[IO.Path]::GetFullPath($FilePath);$dir=if($WorkingDirectory){$WorkingDirectory}else{Split-Path $exe}
    $argLine=($ArgumentList|ForEach-Object{[string]$_}) -join ' '
    # Every test window is muted (Richard, 14 Oct: "super important that everything be muted"). --silent zeroes the game's
    # master volume (AudioDirector.Silent); audio still runs and can be captured. Added here, so no tool can forget it.
    if([IO.Path]::GetFileName($exe) -eq 'Little Road Unity.exe' -and $argLine -notmatch '(^|\s)--(audio-capture-)?silent(\s|$)'){$argLine='--silent '+$argLine}
    $before=[LittleRoad.QuietStart]::GetForegroundWindow()
    $show=switch($WindowStyle){'Hidden'{0}'Minimized'{7}default{4}}
    $id=[LittleRoad.QuietStart]::Start($exe,$argLine,$dir,[int16]$show)
    $proc=[Diagnostics.Process]::GetProcessById($id);[void]$proc.Handle   # (opened now, so ExitCode reads after it exits)
    # Watch for 10 s: if the new process's window took the foreground anyway, hand it back.
    $until=(Get-Date).AddSeconds(10)
    while((Get-Date) -lt $until -and -not $proc.HasExited){
        if([LittleRoad.QuietStart]::OwnerOfForeground() -eq $id -and $before -ne [IntPtr]::Zero){[LittleRoad.QuietStart]::GiveBack($before)}
        Start-Sleep -Milliseconds 200
    }
    return $proc
}
