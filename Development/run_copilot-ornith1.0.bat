@echo off

cd..

title Copilot CLI + LM Studio Fix
cls

echo ================================
echo   FIXED COPILOT CONFIG
echo ================================
echo.

set COPILOT_OFFLINE=true
set COPILOT_PROVIDER_BASE_URL=http://localhost:1234/v1
set COPILOT_MODEL=ornith-1.0-35b

set COPILOT_PROVIDER_MAX_PROMPT_TOKENS=262144
set COPILOT_PROVIDER_MAX_OUTPUT_TOKENS=262144

echo [+] Starting Copilot CLI...
call copilot --banner

cmd /k