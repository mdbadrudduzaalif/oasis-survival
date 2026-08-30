@echo off
title Oasis Survival - Backend Web API Server
cd /d "%~dp0"
cd /d "Server"

echo ===================================================
echo   Oasis Survival: 3-Tier Web API Server (.NET)
echo   Listening on: http://localhost:5000
echo ===================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 goto NoDotnet

echo Starting Server...
echo.
dotnet run --project OasisShooterServer.csproj
goto End

:NoDotnet
echo [ERROR] .NET SDK dotnet command was not found in PATH.
echo Please install .NET 8 or .NET 10 SDK to run the server.
echo.

:End
echo.
echo ===================================================
echo Server process stopped.
echo ===================================================
pause
