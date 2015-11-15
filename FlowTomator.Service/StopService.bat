@echo off

net stop FlowTomator
taskkill /f /im FlowTomator.Service.exe

exit 0