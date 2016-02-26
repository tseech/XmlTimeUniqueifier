@echo off
net stop XmlTimeUniqueifier

if ERRORLEVEL 1 goto error
exit
:error
echo There was a problem stopping the service
pause