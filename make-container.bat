@echo off

rd published /s /q
REM dotnet restore
REM dotnet publish -c Debug -o published
docker build -t netlyt .
