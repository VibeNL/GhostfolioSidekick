@echo off
title Copilot CLI + LM Studio Fix
cls

echo ================================
echo   FIXED COPILOT CONFIG
echo ================================
echo.

set COPILOT_OFFLINE=true
set COPILOT_PROVIDER_BASE_URL=http://localhost:1234/v1
set COPILOT_MODEL=gemma-4-e4b-it

echo [+] Starting Copilot CLI...
call copilot --banner

cmd /k