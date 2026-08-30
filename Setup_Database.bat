@echo off
title Oasis Survival - Database Setup
cd /d "%~dp0"

echo ===================================================
echo   Oasis Survival: Database Setup (SQL Server)
echo ===================================================
echo.

set "SQL_FILE=Assets\Database\OasisShooterDB.sql"
if not exist "%SQL_FILE%" set "SQL_FILE=OasisShooterDB.sql"

if not exist "%SQL_FILE%" goto NoFile

echo Found SQL Script: %SQL_FILE%
echo.

where sqlcmd >nul 2>nul
if errorlevel 1 goto NoSqlcmd

echo 1. Attempting connection to localhost\SQLEXPRESS...
sqlcmd -S "localhost\SQLEXPRESS" -E -C -i "%SQL_FILE%"
if not errorlevel 1 goto SuccessExpress
sqlcmd -S "localhost\SQLEXPRESS" -E -i "%SQL_FILE%"
if not errorlevel 1 goto SuccessExpress

echo.
echo 2. Trying default local instance (localhost)...
sqlcmd -S "localhost" -E -C -i "%SQL_FILE%"
if not errorlevel 1 goto SuccessLocal
sqlcmd -S "localhost" -E -i "%SQL_FILE%"
if not errorlevel 1 goto SuccessLocal

echo.
echo 3. Trying Visual Studio instance ((localdb)\MSSQLLocalDB)...
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -C -i "%SQL_FILE%"
if not errorlevel 1 goto SuccessLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i "%SQL_FILE%"
if not errorlevel 1 goto SuccessLocalDB

goto Failed

:SuccessExpress
echo.
echo [SUCCESS] Database OasisShooterDB configured on localhost\SQLEXPRESS!
goto Done

:SuccessLocal
echo.
echo [SUCCESS] Database OasisShooterDB configured on localhost!
goto Done

:SuccessLocalDB
echo.
echo [SUCCESS] Database OasisShooterDB configured on (localdb)\MSSQLLocalDB!
goto Done

:NoFile
echo [ERROR] Could not find OasisShooterDB.sql!
goto Done

:NoSqlcmd
echo [INFO] sqlcmd command-line utility is not in PATH.
echo To set up your database manually:
echo 1. Open SQL Server Management Studio (SSMS).
echo 2. Open and execute: %SQL_FILE%
echo.
goto Done

:Failed
echo.
echo ===================================================
echo [INFO] Automatic setup could not find an active SQL instance.
echo To configure manually:
echo 1. Open SQL Server Management Studio (SSMS).
echo 2. Connect to your SQL Server.
echo 3. Open "%SQL_FILE%" and click Execute.
echo ===================================================
echo.

:Done
echo.
pause
