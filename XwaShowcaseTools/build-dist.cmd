@echo off
setlocal

cd "%~dp0"

For %%a in (
"XwaOptShowcase\bin\Release\net6.0-windows\*.dll"
"XwaOptShowcase\bin\Release\net6.0-windows\*.exe"
"XwaOptShowcase\bin\Release\net6.0-windows\*.json"
"XwaOptShowcase\bin\Release\net6.0-windows\*.cso"
"XwaOptShowcase\bin\Release\net6.0-windows\runtime\*.*"
) do (
xcopy /s /d "%%~a" dist\
)
