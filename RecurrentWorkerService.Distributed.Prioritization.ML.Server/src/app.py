from flask import Flask, request, send_file

import tempfile
import os
import io

from model import generate_onnx_model

app = Flask(__name__)

@app.route('/', methods=['POST'])
def main():
	new_file, file_path = tempfile.mkstemp()
	generate_onnx_model(request.json, file_path)

	return_data = io.BytesIO()
	with open(file_path, 'rb') as fo:
		return_data.write(fo.read())
	return_data.seek(0)
	os.remove(file_path)

	return send_file(return_data, mimetype='application/octet-stream')

if __name__ == "__main__":
	from waitress import serve
	serve(app, host="0.0.0.0", port=8080)

