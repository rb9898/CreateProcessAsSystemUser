using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProcessLauncher
{
    public static class LaunchHelper
    {
        #region constants
        public const UInt32 MAXIMUM_ALLOWED = 0x02000000;
        public const UInt32 TOKEN_DUPLICATE = 0x0002;
        public const UInt32 NORMAL_PRIORITY_CLASS = 0x00000020;
        public const UInt32 CREATE_NEW_CONSOLE = 0x00000010;
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
        public static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            uint processId);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle,
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

        public static void StartProcessAsSystemUser(string appPath,string cmdLine)
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
            IntPtr winlogonHandle = OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);
            IntPtr winlogonToken = IntPtr.Zero;
            if (!OpenProcessToken(winlogonHandle, TOKEN_DUPLICATE, out winlogonToken))
            {
                CloseHandle(winlogonHandle);
                throw new Exception("OpenProcessToken failed");
            }
            IntPtr duplicatedToken = IntPtr.Zero;
            if(!DuplicateTokenEx(winlogonToken,MAXIMUM_ALLOWED,IntPtr.Zero,(int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                (int)TOKEN_TYPE.TokenPrimary,ref duplicatedToken))
            {
                CloseHandle(winlogonToken);
                throw new Exception("DuplicateTokenEx failed");
            }
            STARTUPINFO si = new STARTUPINFO();
            si.cb = (int)Marshal.SizeOf(si);
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            si.lpDesktop = @"winsta0\default";
            uint dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;
            if(!CreateProcessAsUser(duplicatedToken, appPath, cmdLine, IntPtr.Zero, IntPtr.Zero, false, dwCreationFlags,
                IntPtr.Zero, null, ref si, out pi))
            {
                throw new Exception("CreateProcessAsUser failed");
            }
            
        }
    }

}