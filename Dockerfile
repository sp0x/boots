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

ENV MONGO_HOST=mongo.netlyt.com
ENV MONGO_DB=netvoid
ENV MONGO_PORT=27017

ENV PGSQL_HOST=postgres.netlyt.com
ENV PGSQL_DB=netlyt
ENV PGSQL_PASS=gpiowuert0g9

ENV MQ_PORT=5672
ENV	MQ_HOST=mq.netlyt.com
ENV MQ_USER=netlyt

CMD ["Netlyt.Web.dll"]
ENTRYPOINT [ "dotnet" ]