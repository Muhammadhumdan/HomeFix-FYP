@echo off
REM Deletes the HomeFixDb database so the next API start recreates it with fresh demo data.
REM Use this before your presentation to get back to the starting state.
REM Close the running API first (red stop button in Visual Studio).
cd /d "%~dp0HomeFix.API"
dotnet tool install --global dotnet-ef 2>nul
dotnet ef database drop --force
echo.
echo Database deleted. Start the API again (F5) to recreate it with fresh demo data.
pause
