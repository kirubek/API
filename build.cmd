@echo off
setlocal

echo Restoring solution...
dotnet restore "BaseOps.sln"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo Publishing BaseOps.API...
dotnet publish "BaseOps.API\BaseOps.API.csproj" -c Release -o "out"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo Publish complete.
endlocal
exit /b 0
