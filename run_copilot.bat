@echo off
title Copilot CLI + LM Studio Fix
cls

echo ================================
echo   FIXED COPILOT CONFIG
echo ================================
echo.

set COPILOT_OFFLINE=true
set COPILOT_PROVIDER_BASE_URL=http://localhost:1234/v1
set COPILOT_MODEL=qwen/qwen3-coder-30b

set COPILOT_PROVIDER_MAX_PROMPT_TOKENS=65536
set COPILOT_PROVIDER_MAX_OUTPUT_TOKENS=65536

echo [+] Starting Copilot CLI...
call copilot --banner

cmd /k