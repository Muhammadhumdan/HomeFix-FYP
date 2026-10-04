@echo off
REM Creates EF Core migrations (for your FYP report) and rebuilds the database from them.
REM WARNING: this DELETES the HomeFixDb database and recreates it. Demo data is re-added on next API start.
REM Close Visual Studio's running API first (press the red stop button).
cd /d "%~dp0HomeFix.API"
dotnet tool install --global dotnet-ef 2>nul
dotnet ef migrations add InitialCreate
if errorlevel 1 goto fail
dotnet ef database drop --force
dotnet ef database update
if errorlevel 1 goto fail
echo.
echo Done. Migrations folder created. Now start the API (F5) - demo data will be added automatically.
pause
exit /b 0
:fail
echo.
echo Something failed. Copy the red text above and send it to Claude.
pause
