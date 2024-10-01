chcp 65001 > NUL
@echo off

pushd %~dp0
cd ./Live2dChatter_Data/ExternalPrograms/sbv2\Style-Bert-VITS2
venv\Scripts\python inference.py --text="音声合成の実験、成功だにゃ" --outname="喜"

if %errorlevel% neq 0 ( pause & popd & exit /b %errorlevel% )

popd