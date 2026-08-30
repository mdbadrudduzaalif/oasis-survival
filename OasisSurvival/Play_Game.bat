@echo off
title Oasis Survival - Game & Server Launcher
cd /d "%~dp0"

echo ===================================================
echo   OASIS SURVIVAL - MASTER GAME LAUNCHER
echo ===================================================
echo.

:: 1. Check if Server is already running on port 5000
netstat -ano | findstr ":5000" >nul
if %ERRORLEVEL% EQU 0 (
    echo [OK] Backend API Server is already running on port 5000.
) else (
    echo [STARTING] Launching Backend Web API Server in background...
    if exist "..\Server" (
        start "Oasis Backend Server" /min cmd /c "cd /d ..\Server && dotnet run --urls http://0.0.0.0:5000"
    ) else if exist "Server" (
        start "Oasis Backend Server" /min cmd /c "cd /d Server && dotnet run --urls http://0.0.0.0:5000"
    ) else if exist "..\Start_Server.bat" (
        start "Oasis Backend Server" /min cmd /c "call ..\Start_Server.bat"
    )
    timeout /t 2 >nul
)

:: 2. Launch Game Executable
echo [LAUNCHING] Starting Oasis Survival.exe...
echo.

if exist "Oasis Survival.exe" (
    start "" "Oasis Survival.exe"
) else if exist "OasisSurvival\Oasis Survival.exe" (
    start "" "OasisSurvival\Oasis Survival.exe"
) else (
    echo [ERROR] 'Oasis Survival.exe' not found!
    echo Please build the game from Unity first (Build -> Build Standalone Windows).
    echo.
    pause
)
