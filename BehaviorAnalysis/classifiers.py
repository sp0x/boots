import logging
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split, GridSearchCV, cross_val_score
from sklearn.utils.class_weight import compute_class_weight
from sklearn.metrics import classification_report
from keras.wrappers.scikit_learn import KerasClassifier
from keras.models import save_model, load_model, Model
from sklearn import preprocessing
from keras.layers import *
from threading import Thread,Condition
from Queue import Queue
from constants import RANDOM_SEED,MONITOR_RESPONSE_TIME
from utils import abs_path
import numpy as np
import time,os,datetime
import pickle
import pandas as pd


np.random.seed(RANDOM_SEED)


# real time monitoring - gather data and build training set
# classifiers train and eval

class RealTimeMonitor(Thread):
    def __init__(self):
        super(RealTimeMonitor,self).__init__()
        self.daemon = True
        self.is_running = True
        self.cond = Condition()
        self.data = Queue()

    def interrupt(self):
       self.is_running = False

    def run(self):
        while self.is_running:
            while self.data.not_empty:
                item = self.data.get()
                # do some monitoring shit here
            with self.cond:
                self.cond.wait(MONITOR_RESPONSE_TIME)

    def feed(self, data):
        self.data.put(data)
        with self.cond:
            self.cond.notify()


def get_hidden_neurons_count(Ni, Ns, No=1, a=2):
    return Ns / (a * (Ni + No))

class RNNBuilder:
    def __init(self,data,targets):
        self.num_samples = len(data)
        self.meta_size = len(data[0])#[0])
        #self.time_size = len(data[0][1])
        self.targets = targets

    def build_rnn(self,a=2):
        hidden = get_hidden_neurons_count(self.meta_size, self.num_samples,a=a)
        cw = compute_class_weight('balanced',np.unique(self.targets),self.targets)
        #inp = Input(shape=(self.time_size,))
        inp2 = Input(shape=(self.meta_size,))  # for metadata aka age,wage,blah blah
        #mem = LSTM(hidden)(inp)  # keep mem of past behavior
        #x = concatenate(mem, inp2)
        x = inp2
        x = Dense(hidden, activation=advanced_activations.LeakyReLU())(x)
        x = Dense(hidden, activation='sigmoid')(x)
        x = Dense(hidden, activation='sigmoid')(x)
        out = Dense(1, activation='sigmoid')(x)
        model = Model(inputs=[inp2], outputs=out)
        model.compile(optimizer='rmsprop', loss='binary_crossentropy', metrics='accuracy',loss_weights=cw)

class Experiment:
    def __init__(self,data,targets,models,client):
        """
        :param data: stuff to train and test with 
        :param targets: the results we want
        :param models: the models among which to pick the best one
        :param client: the company or whatever that these models will be used for
        """
        self.data = data
        self.targets = targets
        self.models = models    # list of dicts = [{'model':some grid search compatible thing,
                                # 'params':stuff to test with,'scoring':scoring fx}]
        self._for = client
        self.started = time.time()
        #self.meta_size = len(data['meta'][0])
        #self.realtime_size = len(data['time'][0])
        self.best_models = []
        sub_dir = "experiments/{0}.log".format(datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
        self.log = abs_path(os.path.join(self._for,sub_dir))

    def _log_data_(self,data):
        with open(self.log,'w') as f:
            report = str(data['model']) + '\n'
            report += "=========Best configuration==============\n"
            report += str(data['best']) + '\n'
            report += "\n==========Best model performance on test cases===========\n"
            report += data['best_performance']
            report += "\n==========Other configurations============\n"
            report += pd.DataFrame(data['results']).to_string()
            report += '\n\n\n'
            f.write(report)

    def store_models(self):
        with open(abs_path(os.path.join(self._for,"models.pickle")),'wb') as f:
            pickle.dump(self.best_models,f)

    @staticmethod
    def load_models(company):
        out = None
        with open(abs_path(os.path.join(company,"models.pickle")),'rb') as f:
            out = pickle.load(f)
        return out

    def create_and_train(self):
        """
        :param data: vector of features (1 list of vals per user)
        :param target: vector of result ( 1 val per user)
        :return: 
        """
        #rnn = KerasClassifier(self.build_rnn)
        X_train, X_test, y_train, y_test = train_test_split(self.data, self.targets, test_size=0.25, random_state=RANDOM_SEED)
        #cross_val_score(rf, data, target)
        #clf = GridSearchCV(rf, rf_params, cv=10, scoring="roc_auc")
        #clf.fit(X_train, y_train)
        #clf2 = GridSearchCV(rnn, rnn_params, cv=10, scoring="roc_auc")
        #clf2.fit(X_train, y_train)
        best_models = []

        for m in self.models:
            gs = GridSearchCV(m['model'],m['params'],cv=10,scoring=m['scoring'])
            if 'nn' in m['type']:
                X_train = preprocessing.normalize(X_train)
            gs.fit(X_train,y_train)
            log_data = dict(results=gs.cv_results_,best=gs.best_params_)
            log_data['model'] = m['type']
            best_models.append(dict(type=m['type'],model=gs.best_estimator_))
            y_true, y_pred = y_test, gs.predict(X_test)
            log_data['best_performance'] = classification_report(y_true,y_pred)
            self._log_data_(log_data)
        self.best_models = best_models


def conduct_experiment(data,targets,client='cashlend'):
    rf = RandomForestClassifier(n_jobs=-1, oob_score=True)
    rnn = KerasClassifier(RNNBuilder(data,targets).build_rnn)
    rf_params = {
        "n_estimators": [5, 10, 20, 30, 35],
        "max_depth": [None, 10, 20, 40, 80],
        "min_samples_split": [2, 8, 16, 32, 64, 128],
        "max_features": ["auto", "sqrt", 10, 20, 30, 40, 50]
    }
    rnn_params = {
        "a": range(2, 10),
    }
    fl = "system/{0}.log".format(datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
    logging.basicConfig(filename=abs_path(fl), level=logging.DEBUG)
    logging.info("experiments for {1} started at {0}".format(unicode(datetime.datetime.now()),client))
    e = Experiment(data,targets,[dict(model=rnn,params=rnn_params,scoring='roc_auc',type='rnn'),
                                 dict(model=rf,params=rf_params,scoring='roc_auc',type='rf')], client)
    logging.info("experiments for {1} ended at {0}".format(unicode(datetime.datetime.now()),client))
    e.create_and_train()

