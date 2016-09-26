@echo off

net stop FlowTomator

tasklist /FI "IMAGENAME eq FlowTomator.Service.exe" 2>NUL | find /I /N "FlowTomator.Service.exe">NUL
if "%ERRORLEVEL%"=="0" taskkill /f /im FlowTomator.Service.exe

exit 0