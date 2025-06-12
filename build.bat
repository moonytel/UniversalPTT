@echo off
echo Building Universal Push-to-Talk...
echo.

REM Clean previous builds
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"

echo Cleaning completed.
echo.

REM Build single-file executable
echo Building single-file executable...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Build completed successfully!
    echo.
    echo Output file: bin\Release\net6.0-windows\win-x64\publish\UniversalPTT.exe
    echo.
    echo You can now distribute this single EXE file.
    pause
) else (
    echo.
    echo ❌ Build failed!
    pause
)