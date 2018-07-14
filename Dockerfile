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

EXPOSE 5000
EXPOSE 80
CMD ["dotnet"]
ENTRYPOINT [ "dotnet" ]
