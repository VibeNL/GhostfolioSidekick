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
set COPILOT_MODEL=qwen3.8-27b@iq3_s

set COPILOT_PROVIDER_MAX_PROMPT_TOKENS=105802
set COPILOT_PROVIDER_MAX_OUTPUT_TOKENS=105802

echo [+] Starting Copilot CLI...
call copilot --banner

cmd /k