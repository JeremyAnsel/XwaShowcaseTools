@echo off
setlocal

cd "%~dp0"

xcopy /s /d "XwaOptShowcase\bin\Release\net6.0-windows" dist\
xcopy /s /d "XwaSizeComparison\bin\Release\net6.0-windows" dist\

