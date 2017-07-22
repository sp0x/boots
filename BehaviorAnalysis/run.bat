@echo off
REM this will just start bash, use this script for debugging

docker run -it --rm ^
-p 5556:5556 -p 5557:5557 ^
-v d:/dev/asp.net/Peeralize/BehaviorAnalysis/src:/app ^
--entrypoint=bash ^
 peeralize/behavior