@echo off
set SLANGC="C:\Program Files\slang\bin\slangc.exe"
set FLAGS=-target spirv

%SLANGC% uiShader.slang %FLAGS% -o Compiled\uiShader.spv
if %ERRORLEVEL% EQU 0 (
echo ✅ shader compiled
) else (
echo ❌ shader failed
)

%SLANGC% textShader.slang %FLAGS% -o Compiled\textShader.spv
if %ERRORLEVEL% EQU 0 (
echo ✅ shader compiled
) else (
echo ❌ shader failed
)

%SLANGC% wireFrameShader.slang %FLAGS% -o Compiled\wireFrameShader.spv
if %ERRORLEVEL% EQU 0 (
echo ✅ shader compiled
) else (
echo ❌ shader failed
)

pause
:: Replace C:\path\to\slangc.exe with the location of slangc.exe
:: For example:
:: set SLANGC=C:\slang\bin\slangc.exe
:: or, if slangc.exe is on PATH:
:: set SLANGC=slangc
:: and you can remove the set FLAGS line and use -target spirv directly.
