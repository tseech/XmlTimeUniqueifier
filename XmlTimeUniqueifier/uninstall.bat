@echo off
XmlTimeUniqueifier.exe -uninstall

if ERRORLEVEL 1 goto error
echo The service was uninstalled
pause
exit
:error
echo There was a problem uninstalling the service
pause