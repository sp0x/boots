docker run -it --rm \
-p 5556:5556 -p 5557:5557  \
--mount type=bind,source="$(pwd)"/src,target=/app \
--mount type=volume,source=experiments,target=/experiments \
--entrypoint=bash \
 peeralize/behavior