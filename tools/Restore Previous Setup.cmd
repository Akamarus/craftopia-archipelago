@echo off
title Restore Previous Craftopia Setup
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Mod.ps1" -Action Restore
echo.
pause
