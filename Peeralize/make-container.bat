@echo off

rd published /s /q
dotnet restore
dotnet publish -c Debug -o published
docker build ^
 -t peeralize/main .
