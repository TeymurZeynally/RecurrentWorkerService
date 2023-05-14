FROM tensorflow/tensorflow:latest

WORKDIR /app

COPY requirements.txt /app
RUN pip3 install -r requirements.txt

COPY /src /app
COPY *.py /app
ENTRYPOINT ["python3"]
CMD ["app.py"]