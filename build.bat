@echo off
setlocal EnableExtensions EnableDelayedExpansion

REM ================================
REM Configuration
REM ================================
set SOLUTION=HarpyEngine.slnx
set PROJECT_NAME=Sandbox
set TEST_PROJECT=Engine.Tests\Engine.Tests.csproj
set CONFIGURATION=Release
set RUNTIME=win-x64
set OUTPUT_DIR=.\publish
set ASSET_DIR=.\assets
set ENABLE_COVERAGE=true

echo ==========================================
echo   Harpy CI Build Pipeline Starting
echo ==========================================

:: [1/6] Clean
echo [1/6] Cleaning...
if exist %OUTPUT_DIR% rd /s /q %OUTPUT_DIR%
dotnet clean %SOLUTION% -c %CONFIGURATION% -v q >nul
if errorlevel 1 goto :fail

:: [2/6] Restore
echo [2/6] Restoring NuGet packages...
dotnet restore %SOLUTION% -v q
if errorlevel 1 goto :fail

:: [3/6] Build
echo [3/6] Building solution...
dotnet build %SOLUTION% -c %CONFIGURATION% --no-restore -v q
if errorlevel 1 goto :fail

:: [4/6] Test
echo [4/6] Running tests...
dotnet test %TEST_PROJECT% -c %CONFIGURATION% --no-build -v q --nologo
if errorlevel 1 goto :fail

:: [5/6] Publish (NativeAOT)
echo [5/6] Publishing NativeAOT...
dotnet publish Sandbox\%PROJECT_NAME%.csproj ^
    -c %CONFIGURATION% ^
    -r %RUNTIME% ^
    -o %OUTPUT_DIR% ^
    --no-build ^
    -v q ^
    /p:PublishAot=true
if errorlevel 1 goto :fail

echo [5.5/6] Copying Assets...
xcopy /E /I /Y ".\Assets" ".\publish\Assets" >nul

:: [6/6] Cleanup
echo [6/6] Finalizing...
if exist "%OUTPUT_DIR%\*.pdb" del /q "%OUTPUT_DIR%\*.pdb"

echo ==========================================
echo   BUILD SUCCESS
echo ==========================================
goto :end

:fail
echo.
echo   BUILD FAILED
echo ==========================================
exit /b 1

:end
pause