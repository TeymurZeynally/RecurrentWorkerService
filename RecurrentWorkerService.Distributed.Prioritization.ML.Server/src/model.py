import numpy as np
import pandas as pd
import tensorflow as tf

import random

from tensorflow.keras.models import Sequential
from tensorflow.keras.models import Model

from tensorflow.keras.layers import LSTM
from tensorflow.keras.layers import Dense
from tensorflow.keras.layers import Lambda
from tensorflow.keras.layers import Input
from tensorflow.keras.layers import Multiply

from tensorflow.keras.callbacks import EarlyStopping

from sklearn.preprocessing import MinMaxScaler

from sklearn.metrics import mean_squared_error

import onnx
import tf2onnx

def generate_onnx_model(data, savePath):
	print(data, flush=True)

	dataframe = pd.DataFrame(data=data)
	dataframe = dataframe.rename(columns={'Memory': 'Ram', 'Network': 'Net'})

	stick = lambda x: 0 if x < 0.15 else (1 if x > 0.85 else x)
	dataframe[['Cpu%', 'Ram%', 'Net%']] = [[stick(round(random.uniform(0, 1), 2)),stick(round(random.uniform(0, 1), 2)), stick(round(random.uniform(0, 1), 2))] for i, f in dataframe.iterrows()]

	scaler = MinMaxScaler(feature_range=(0, 1))
	dataframe[['Cpu', 'Ram', 'Net']] = scaler.fit_transform(dataframe[['Cpu', 'Ram', 'Net']])

	dataframe['Priority'] = [max(f['Cpu']*f['Cpu%'], f['Ram']*f['Ram%'], f['Net']*f['Net%']) for i, f in dataframe.iterrows()]

	dataset_series = dataframe[['Cpu', 'Ram', 'Net', 'Priority']].values
	dataset_priorities = dataframe[['Cpu%', 'Ram%', 'Net%']].values

	train_size = int(len(dataset_series) * 0.67)
	test_size = len(dataset_series) - train_size

	train_s, test_s = dataset_series[0:train_size,:], dataset_series[train_size:len(dataset_series),:]
	train_p, test_p = dataset_priorities[0:train_size,:], dataset_priorities[train_size:len(dataset_priorities),:]

	def split_sequences(sequences, priorities, n_steps):
		x1, x2, y = list(), list(), list()
		for i in range(len(sequences) - n_steps):
			end_ix = i + n_steps

			seq_x1 = sequences[i:end_ix, :-1]
			seq_x2 = priorities[end_ix-1, :]
			seq_y = sequences[end_ix-1, -1]

			x1.append(seq_x1)
			x2.append(seq_x2)
			y.append(seq_y)
		return np.array(x1), np.array(x2), np.array(y)

	n_steps = 10
	n_features = 3

	train_x1, train_x2, train_y = split_sequences(train_s, train_p, n_steps)
	test_x1, test_x2, test_y = split_sequences(test_s, test_p, n_steps)

	def get_model():
		time_series_input = Input(shape=(n_steps, n_features))
		time_series = LSTM(100, activation='relu', return_sequences=True, input_shape=(n_steps, n_features))(time_series_input)
		time_series = LSTM(100, activation='relu')(time_series)
		time_series = Dense(n_features)(time_series)
		time_series = Model(inputs=time_series_input, outputs=time_series)

		priority_input = Input(shape=(None, 3))
		priority = Model(inputs=priority_input, outputs=priority_input)

		combined = Multiply()([time_series.output, priority.output])
		combined = Lambda(lambda f: tf.expand_dims(tf.keras.backend.max(f, axis=-1), -1))(combined)

		model = Model(inputs=[time_series.input, priority.input], outputs=combined)

		model.compile(optimizer='adam', loss='mean_squared_error')
		return model

	model = get_model()
	model.fit([train_x1, train_x2], train_y, epochs=100, batch_size=1, callbacks=[EarlyStopping(monitor='loss', mode='min', patience=5)])

	onnx_model, _ = tf2onnx.convert.from_keras(model, opset=11)
	onnx.save(onnx_model, savePath)