#Build the thing
FROM microsoft/dotnet:2.1-sdk as aspnet
ENV SLN_FILE Netlyt.Prod.sln
ENV FRAMEWORK netcoreapp2.1
WORKDIR /appsrc
COPY . /appsrc
RUN echo "Building ${SLN_FILE}" \
    && mkdir -p /appsrc/published \
	&& dotnet restore ${SLN_FILE} \
	&& dotnet publish ${SLN_FILE} -f=${FRAMEWORK} -c Debug -o /appsrc/published 

#Now with the same base container and the built binary
FROM microsoft/dotnet:2.1-sdk
ARG source
WORKDIR /app
#Copy our build
COPY --from=aspnet /appsrc/published/ .

#EXPOSE 5000

EXPOSE 81
HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=15s CMD curl -f http://localhost:81/status || exit 1

CMD ["dotnet"]
ENTRYPOINT [ "dotnet" ]
