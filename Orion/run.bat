@echo off
REM this will just start bash, use this script for debugging
REM for linux use --mount type=bind,source="$(pwd)"/src,target=/app
docker run -it --rm ^
-p 5556:5556 -p 5557:5557 -m 6g  ^
-v d:/dev/asp.net/Netlyt/Orion/src:/app ^
--mount type=volume,source=./experiments,target=/experiments ^
--entrypoint=bash ^
 peeralize/behavior