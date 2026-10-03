@echo off
:: Batch script to fix Unity GPU TDR crash
echo Dang sua loi GPU Timeout TDR cho Windows...
net session >nul 2>&1
if %errorLevel% == 0 (
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v "TdrDelay" /t REG_DWORD /d 10 /f
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v "TdrDdiDelay" /t REG_DWORD /d 10 /f
    echo.
    echo ========================================================
    echo DA FIX THANH CONG! TdrDelay da duoc nang len 10 giay.
    echo Windows se khong con tu dong ngat card GPU cua Unity nua.
    echo ========================================================
    pause
) else (
    echo Dang yeu cau quyen Administrator (UAC)...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
)
