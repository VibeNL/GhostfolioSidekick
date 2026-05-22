@echo off
title GitHub Copilot Standalone CLI - Gemma 4 E4B (128K Context)
cls

echo =======================================================
echo   CONFIGURING ENVIRONMENT FOR STANDALONE COPILOT CLI
echo   HARDWARE: NVIDIA RTX 5080 (16GB VRAM)
echo   MODEL: Google Gemma 4 E4B (128K Context)
echo =======================================================
echo.

:: 1. Activeer offline BYOK-modus
set COPILOT_OFFLINE=true

:: 2. Koppel de API-server van LM Studio
set COPILOT_PROVIDER_BASE_URL=http://localhost:1234/v1

:: 3. De modelnaam (Zorg dat dit exact matcht met LM Studio)
set COPILOT_MODEL=gemma-4-e4b

:: 4. DE OPLOSSING: Handmatige override voor de onbekende catalogus-fout
set COPILOT_PROVIDER_MAX_PROMPT_TOKENS=131072
set COPILOT_PROVIDER_MAX_OUTPUT_TOKENS=8192

:: 5. Context- en limietvariabelen voor de LLM-backend
set COPILOT_CONTEXT_LENGTH=131072
set COPILOT_MAX_TOKENS=8192
set LLM_MAX_TOKENS=8192

:: 6. CUDA-optimalisaties voor jouw RTX 5080
set CUDA_VISIBLE_DEVICES=0
set GGML_CUDA_FORCE_MMQ=1

echo [+] Environment Variables Registered Successfully!
echo     - Endpoint: %COPILOT_PROVIDER_BASE_URL%
echo     - Catalog Overrides: Enabled (128K Prompt / 8K Output)
echo.
echo =======================================================
echo   LAUNCHING COPILOT STANDALONE CLI...
echo =======================================================
echo.

:: Start de standalone copilot tool op via call
call copilot --banner

echo.
echo [+] Copilot sessie beëindigd.
cmd /k
