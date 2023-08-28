using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProcessLauncher
{
    public static class LaunchHelper
    {
        #region constants
        private const UInt32 MAXIMUM_ALLOWED = 0x02000000;
        private const UInt32 TOKEN_DUPLICATE = 0x0002;
        private const UInt32 NORMAL_PRIORITY_CLASS = 0x00000020;
        private const UInt32 CREATE_NEW_CONSOLE = 0x00000010;
        #endregion

        #region structs
        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public String lpReserved;
            public String lpDesktop;
            public String lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        private enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3,
        }

        private enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }
        #endregion

        #region DllImports
        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            uint processId);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle,
            UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        private static extern bool DuplicateTokenEx(
            IntPtr ExistingTokenHandle,
            uint dwDesiredAccess,
            IntPtr lpThreadAttributes,
            int TokenType,
            int ImpersonationLevel,
            ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            String lpApplicationName,
            String lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandle,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            String lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);
        #endregion

        public static void StartProcessAsSystemUser(string appPath, string cmdLine="")
        {
            IntPtr winlogonHandle = IntPtr.Zero;
            IntPtr winlogonToken = IntPtr.Zero;
            IntPtr duplicatedToken = IntPtr.Zero;
            int errorCode;
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            try
            {
                uint activeSessionId = WTSGetActiveConsoleSessionId();
                uint winlogonPid = 0;
                Process[] processes = Process.GetProcessesByName("winlogon");
                foreach (Process p in processes)
                {
                    if ((uint)p.SessionId == activeSessionId)
                    {
                        winlogonPid = (uint)p.Id;
                    }
                }
                winlogonHandle = OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);              
                if (!OpenProcessToken(winlogonHandle, TOKEN_DUPLICATE, out winlogonToken))
                {
                    errorCode = Marshal.GetLastWin32Error();
                    throw new Exception($"OpenProcessToken failed with error code : {errorCode}");
                }               
                if (!DuplicateTokenEx(winlogonToken, MAXIMUM_ALLOWED, IntPtr.Zero, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                    (int)TOKEN_TYPE.TokenPrimary, ref duplicatedToken))
                {
                    errorCode = Marshal.GetLastWin32Error();
                    throw new Exception($"DuplicateTokenEx failed with error code : {errorCode}");
                }
                STARTUPINFO si = new STARTUPINFO();
                si.cb = (int)Marshal.SizeOf(si);
                si.lpDesktop = @"winsta0\default";
                uint dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;
                if (!CreateProcessAsUser(duplicatedToken, appPath, cmdLine, IntPtr.Zero, IntPtr.Zero, false, dwCreationFlags,
                    IntPtr.Zero, null, ref si, out pi))
                {
                    errorCode = Marshal.GetLastWin32Error();
                    throw new Exception($"CreateProcessAsUser failed with error code : {errorCode}");
                }
            }
            finally
            {
                if (winlogonToken != IntPtr.Zero)
                {
                    CloseHandle(winlogonToken);
                }
                if(duplicatedToken != IntPtr.Zero)
                {
                    CloseHandle(duplicatedToken);
                }
                if(winlogonHandle != IntPtr.Zero)
                {
                    CloseHandle(winlogonHandle);
                }
                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);
            }
        }
    }

}
