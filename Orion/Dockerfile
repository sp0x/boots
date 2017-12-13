FROM continuumio/anaconda

ARG source
WORKDIR /app

#COPY ./src/ .
RUN apt-get install -y libgl1-mesa-glx
RUN conda install keras pyzmq pymongo -y --quiet
RUN pip install -Iv seaborn==0.4
RUN pip install timestring
RUN conda install -c brown-data-science treeinterpreter scikit-learn  -y --quiet
EXPOSE 5556 5557

ENTRYPOINT ["python", "-u", "__init__.py"] 













































