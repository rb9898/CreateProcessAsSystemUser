@echo off
%windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe "bin\Debug\Demo Service.exe"
sc start DemoService