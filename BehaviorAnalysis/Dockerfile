FROM continuumio/anaconda

ARG source
WORKDIR /app

#COPY ./src/ .
RUN apt-get install -y libgl1-mesa-glx
RUN conda install keras pyzmq pymongo seaborn -y --quiet
RUN conda install -c brown-data-science treeinterpreter  -y --quiet
EXPOSE 5556 5557

ENTRYPOINT ["python", "-u", "__init__.py"] 













































