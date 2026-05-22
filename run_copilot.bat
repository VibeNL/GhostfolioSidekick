@echo off
title Copilot CLI + LM Studio Fix
cls

echo ================================
echo   FIXED COPILOT CONFIG
echo ================================
echo.

set COPILOT_OFFLINE=true
set COPILOT_PROVIDER_BASE_URL=http://localhost:1234/v1
set COPILOT_MODEL=qwen3-coder-30b-a3b-instruct

:: IMPORTANT FIX (prevents 18K+ prompts)
set COPILOT_DISABLE_WORKSPACE_CONTEXT=true

:: SAFE LIMITS (these are advisory only)
set COPILOT_PROVIDER_MAX_PROMPT_TOKENS=12000
set COPILOT_PROVIDER_MAX_OUTPUT_TOKENS=1024

echo [+] Starting Copilot CLI...
call copilot --banner

cmd /k