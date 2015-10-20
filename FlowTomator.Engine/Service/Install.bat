@echo off

set DOTNETFX2=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319
set PATH=%PATH%;%DOTNETFX2%

echo Installing service

InstallUtil /i ..\..\Binaries\FlowTomator.Engine.exe

echo Done
pause