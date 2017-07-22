@echo off
REM this will just start bash, use this script for debugging

docker run -it ^
-p 5556:5556 -p 5557:5557 ^
-v ./src:/app ^
--entrypoint=bash ^
 peeralize/behavior