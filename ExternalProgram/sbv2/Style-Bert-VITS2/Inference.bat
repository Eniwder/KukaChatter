chcp 65001 > NUL
@echo off

pushd %~dp0
echo Running ./inference.py...
venv\Scripts\python inference.py --text="引数から音声合成の実験だにゃ"

if %errorlevel% neq 0 ( pause & popd & exit /b %errorlevel% )

popd
pause