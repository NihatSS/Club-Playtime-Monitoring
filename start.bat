@echo off
cd /d "%~dp0"
title Club Playtime - Starting...
color 0A

echo =========================================
echo        Club Playtime - Starting All
echo =========================================
echo.

:: =========================================
:: Step 0: Kill any existing processes
:: =========================================
echo [1/4] Cleaning up old processes...

:: Kill any existing DiscordBot process
for /f "tokens=2" %%a in ('tasklist ^| findstr /I "ClubPlaytime.DiscordBot"') do (
    taskkill /F /PID %%a >nul 2>&1
)

:: Kill process on API port (5121)
for /f "tokens=5" %%a in ('netstat -ano ^| findstr /C:":5121 "') do (
    taskkill /F /PID %%a >nul 2>&1
)

:: Kill process on frontend port (5173)
for /f "tokens=5" %%a in ('netstat -ano ^| findstr /C:":5173 "') do (
    taskkill /F /PID %%a >nul 2>&1
)

timeout /t 1 /nobreak >nul
echo       Ports 5121 and 5173 are free, DiscordBot stopped.
echo.

:: =========================================
:: Step 1: Start the Backend API
:: =========================================
echo [2/4] Starting Backend API on http://localhost:5121...
start "ClubPlaytime-API" cmd /c "cd /d "%~dp0ClubPlaytime.Api" && dotnet run --no-launch-profile --urls "http://localhost:5121" || pause"
echo       Waiting for backend...
timeout /t 6 /nobreak >nul

netstat -ano | findstr /C:":5121 " >nul
if %errorlevel% equ 0 (
    echo       Backend is running!
) else (
    echo       Waiting a bit more...
    timeout /t 8 /nobreak >nul
)
echo.

:: =========================================
:: Step 2: Start the Frontend (Vite)
:: =========================================
echo [3/4] Starting Frontend on http://localhost:5173...
start "ClubPlaytime-Frontend" cmd /c "cd /d "%~dp0Client" && npx vite --port 5173 || pause"
echo       Frontend starting...
echo.

:: =========================================
:: Step 3: Start the Discord Bot
:: =========================================
echo [4/4] Starting Discord Bot...
start "ClubPlaytime-DiscordBot" cmd /c "cd /d "%~dp0ClubPlaytime.DiscordBot" && dotnet run --no-launch-profile || pause"
echo       Discord bot starting...
echo.

:: =========================================
:: Done
:: =========================================
echo =========================================
echo   All services starting!
echo.
echo   Frontend: http://localhost:5173
echo   API:      http://localhost:5121
echo   Swagger:  http://localhost:5121/swagger
echo.
echo   Close this window to keep everything
echo   running, or close each service window
echo   individually to stop that service.
echo =========================================
echo.
echo Press any key to open the frontend in your browser...
pause >nul

start http://localhost:5173
