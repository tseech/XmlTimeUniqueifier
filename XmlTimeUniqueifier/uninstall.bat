@echo off
c:\windows\microsoft.net\framework\v4.0.30319\installutil.exe /u XmlTimeUniqueifier.exe

if ERRORLEVEL 1 goto error
echo The service was uninstalled
pause
exit
:error
echo There was a problem uninstalling the service
pause