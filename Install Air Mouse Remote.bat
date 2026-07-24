@echo off
title Install Air Mouse Remote
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0installer\Install.ps1"
echo.
pause
