FROM continuumio/anaconda

ARG source
WORKDIR /app

#COPY ./src/ .

RUN conda install keras pyzmq pymongo -y --quiet

EXPOSE 5556 5557

ENTRYPOINT ["python", "-u", "__init__.py"] 













































