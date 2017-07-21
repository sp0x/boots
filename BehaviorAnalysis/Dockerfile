FROM continuumio/anaconda

ARG source
WORKDIR /app
COPY ./src/ .

RUN conda install keras -y --quiet

EXPOSE 5556 5557

ENTRYPOINT ["python", "__init__.py"]