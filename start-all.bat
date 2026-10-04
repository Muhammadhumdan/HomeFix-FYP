@echo off
REM Starts the backend (API) and the website, then opens the browser.
REM Do NOT use this while Visual Studio is already running the API (port 5080 would be busy).
cd /d "%~dp0"
if not exist HomeFix.Client\node_modules (
  echo Installing website packages for the first time, please wait...
  pushd HomeFix.Client
  call npm.cmd install
  popd
)
start "HomeFix API (backend)" cmd /k "dotnet run --project HomeFix.API --launch-profile http"
start "HomeFix Website (frontend)" cmd /k "cd HomeFix.Client && npm.cmd run dev"
echo Waiting for both to start...
timeout /t 20 >nul
start http://localhost:5173
