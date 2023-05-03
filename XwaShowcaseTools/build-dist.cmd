@echo off
setlocal

cd "%~dp0"

xcopy /s /d "%~dp0\XwaOptShowcase\bin\Release\net6.0-windows\" dist\
