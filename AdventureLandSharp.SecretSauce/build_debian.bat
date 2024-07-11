@echo off
dotnet publish -c Release -r linux-x64 --self-contained

echo ========================================================
echo Now copy bin/Release/net8.0/linux-x64/publish to remote!
echo ========================================================