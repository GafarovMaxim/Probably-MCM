@echo off
chcp 65001 >nul
title Probably MCM - Steam Workshop Uploader

echo =======================================================
echo     Probably MCM - Steam Workshop Publishing Tool
echo =======================================================
echo.
echo Author: Raul Moreau
echo AppID:  4348910 (Probably Stolen)
echo Config: workshop_item.vdf
echo.

set SCRIPT_DIR=%~dp0
set VDF_FILE=%SCRIPT_DIR%workshop_item.vdf
set TOOLS_DIR=%SCRIPT_DIR%.steamcmd
set STEAMCMD_EXE=%TOOLS_DIR%\steamcmd.exe

if not exist "%VDF_FILE%" (
    echo [ERROR] workshop_item.vdf not found!
    pause
    exit /b 1
)

if not exist "%STEAMCMD_EXE%" (
    echo [INFO] SteamCMD not found. Downloading official SteamCMD from Valve...
    if not exist "%TOOLS_DIR%" mkdir "%TOOLS_DIR%"
    powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip' -OutFile '%TOOLS_DIR%\steamcmd.zip'"
    if not exist "%TOOLS_DIR%\steamcmd.zip" (
        echo [ERROR] Failed to download SteamCMD. Please download it manually from https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
        pause
        exit /b 1
    )
    echo [INFO] Extracting SteamCMD...
    powershell -Command "Expand-Archive -Path '%TOOLS_DIR%\steamcmd.zip' -DestinationPath '%TOOLS_DIR%' -Force"
    del /q "%TOOLS_DIR%\steamcmd.zip"
)

echo.
echo Please enter your Steam account username:
set /p STEAM_USER="Steam Username: "

if "%STEAM_USER%"=="" (
    echo [ERROR] Username cannot be empty.
    pause
    exit /b 1
)

echo.
echo =======================================================
echo Logging in and uploading Probably MCM to Steam Workshop...
echo (If prompted, enter your Steam password and Steam Guard code)
echo =======================================================
echo.

"%STEAMCMD_EXE%" +login %STEAM_USER% +workshop_build_item "%VDF_FILE%" +quit

echo.
echo =======================================================
echo Process finished. Check output above for 'Success. Published file ID: XXXXXXXX'.
echo Note: Mode is set to 'Hidden' (visibility 2). You can change it to Public on Steam.
echo =======================================================
pause
