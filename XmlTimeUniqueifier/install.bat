@echo off
c:\windows\microsoft.net\framework\v4.0.30319\installutil.exe XmlTimeUniqueifier.exe

if ERRORLEVEL 1 goto error
echo The service has been installed
echo To complete installation open component services and find "XmlTimeUniqueifier":
echo   1) Set Startup Type to Automatic
echo   2) Set Log On As to a user with permissions to access the Inbox files share
echo   3) Verify the configuration file (XmlTimeUniqueifier.exe.xml) contains the correct information
echo   4) Start the service - The log file may be checked for errors
pause
exit
:error
echo There was a problem installing the service
pause