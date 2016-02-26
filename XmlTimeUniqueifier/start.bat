@echo off
net start XmlTimeUniqueifier

if ERRORLEVEL 1 goto error
exit
:error
echo There was a problem starting the service
pause