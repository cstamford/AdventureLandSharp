@echo off
dotnet publish -c Release -r linux-arm --self-contained

echo ========================================================
echo Now copy bin/Release/net8.0/linux-arm/publish to remote!
echo ========================================================