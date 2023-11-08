# CreateProcessAsSystemUser
A library for creating processes(with GUI) as System user in the currently interactive user session on windows. The System user has elevated privileges and can perform tasks that regular users cannot. Running a process with System privileges within a user's interactive session could lead to security vulnerabilities if not handled carefully. So, this should be done with caution and for legitimate reasons. 

This allows the processes such as windows services(which runs in a different session) to create processes with GUI within a user's interactive session. The processes must have admin privileges for this to work properly.

# Usage 
using ProcessLauncher;

LaunchHelper.StartProcessAsSystemUser(@"C:\Windows\System32\notepad.exe");
