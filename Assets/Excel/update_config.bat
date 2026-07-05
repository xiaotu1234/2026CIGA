@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PYTHON_CMD="
set "CODEX_PYTHON=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"

if exist "%CODEX_PYTHON%" call :TryPythonExe "%CODEX_PYTHON%"
if not defined PYTHON_CMD call :TryPythonCommand py -3
if not defined PYTHON_CMD call :TryPythonCommand python

if not defined PYTHON_CMD (
  echo [Config] No usable Python environment with openpyxl was found.
  echo [Config] Falling back to PowerShell exporter.
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%update_config.ps1" %*
  set "EXIT_CODE=%ERRORLEVEL%"
  goto :HandleExit
)

%PYTHON_CMD% "%SCRIPT_DIR%update_config.py" %*
set "EXIT_CODE=%ERRORLEVEL%"

:HandleExit
if not "%EXIT_CODE%"=="0" (
  echo.
  echo [Config] Export failed.
  echo [Config] If the Python exporter is preferred, install Python and run:
  echo python -m pip install openpyxl
  pause
  exit /b %EXIT_CODE%
)

echo.
echo [Config] Export complete.
pause
exit /b 0

:TryPythonExe
"%~1" -c "import openpyxl" >nul 2>nul
if %ERRORLEVEL% EQU 0 set "PYTHON_CMD="%~1""
exit /b 0

:TryPythonCommand
%* -c "import openpyxl" >nul 2>nul
if %ERRORLEVEL% EQU 0 set "PYTHON_CMD=%*"
exit /b 0
