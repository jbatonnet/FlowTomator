@echo off

set DOTNETFX2=%SystemRoot%\Microsoft.NET\Framework\v2.0.50727
set PATH=%PATH%;%DOTNETFX2%

echo Installing service

InstallUtil /i ..\..\Binaries\FlowTomator.Engine.exe

echo Done
pause