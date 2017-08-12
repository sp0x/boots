import logging
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split, GridSearchCV, cross_val_score
from sklearn.utils.class_weight import compute_class_weight
from sklearn.metrics import classification_report, accuracy_score
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
import os


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
    def __init__(self, data, targets, class_weights):
        self.num_samples = len(data)
        self.meta_size = len(data[0])#[0])
        #self.time_size = len(data[0][1])
        self.targets = targets
        self.class_weights = class_weights

    def build_rnn(self,a=2):
        hidden = get_hidden_neurons_count(self.meta_size, self.num_samples,a=a)
        tmp = np.unique(self.targets)
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
        model.compile(optimizer='rmsprop', loss='binary_crossentropy', metrics='accuracy',loss_weights=self.class_weights)

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
        sub_dir = "experiments/{0}.log".format(datetime.datetime.now().strftime("%Y-%m-%d_%H:%M:%S"))
        if not os.path.exists(self._for):
            os.makedirs(self._for)
        self.log = abs_path(os.path.join(self._for,sub_dir))
        if not os.path.exists(self._for):
            os.makedirs(self._for + "/experiments")

    def _log_data_(self,data):
        with open(self.log,'w') as f:
            report = str(data['model']) + '\n'
            report += "=========Best configuration==============\n"
            report += str(data['best']) + '\n'
            report += "\n==========Best model performance on test cases===========\n"
            report += data['best_performance']
            report += "\n"+ str(data['accuracy']) + "\n"
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

    @staticmethod
    def predict_explain(self, tree, data, labels, print_proba=False):
        report = ""
        from treeinterpreter import treeinterpreter as ti
        prediction, bias, contributions = ti.predict(tree, data) if not print_proba else ti.predict_proba(tree,data)
        report += "Prediction(s): {0}\n".format(prediction)
        report += "Bias(es): {0}\n".format(bias)
        report += "Feature contributions:\n"
        for c, feature in zip(contributions[0],labels):
            report += feature + str(c) + '\n'
        return report

    def create_and_train(self):
        #rnn = KerasClassifier(self.build_rnn)
        X_train, X_test, y_train, y_test = train_test_split(self.data, self.targets, test_size=0.25, random_state=RANDOM_SEED)
        #cross_val_score(rf, data, target)
        #clf = GridSearchCV(rf, rf_params, cv=10, scoring="roc_auc")
        #clf.fit(X_train, y_train)
        #clf2 = GridSearchCV(rnn, rnn_params, cv=10, scoring="roc_auc")
        #clf2.fit(X_train, y_train)
        best_models = [] 
        for m in self.models:
            gs = GridSearchCV(estimator=m['model'],param_grid=m['params'],cv=10,scoring=m['scoring'])
            if 'nn' in m['type']:
                X_train = preprocessing.normalize(X_train)
            gs.fit(X_train,y_train)
            log_data = dict(results=gs.cv_results_,best=gs.best_params_)
            log_data['model'] = m['type']
            best_models.append(dict(type=m['type'],model=gs.best_estimator_))
            y_true, y_pred = y_test, gs.predict(X_test)
            log_data['best_performance'] = classification_report(y_true,y_pred)
            log_data['accuracy'] = accuracy_score(y_true, y_pred)
            self._log_data_(log_data)
        self.best_models = best_models

        def create_and_train_from_stream(self,stream_reader):
            """
            Train using a stream reader rather than loading everything into memory
            :param self: 
            :param stream_reader: MongoDataStreamReader
            """
            data, targets = stream_reader.get_training_set()
            X_train, X_test, y_train, y_test = train_test_split(data, targets, test_size=0.25,
                                                                random_state=RANDOM_SEED)
            stream_reader.set_order(X_train.extends(X_test))
            best_models = []
            for m in self.models:
                gs = GridSearchCV(estimator=m['model'], param_grid=m['params'], cv=10, scoring=m['scoring'])
                stream_reader.reset_cursor()
                if 'nn' in m['type']:
                    stream_reader.set_normalize(True)
                else:
                    stream_reader.set_normalize(False)
                gs.fit(stream_reader.read(), y_train)
                log_data = dict(results=gs.cv_results_, best=gs.best_params_)
                log_data['model'] = m['type']
                best_models.append(dict(type=m['type'], model=gs.best_estimator_))
                y_true, y_pred = y_test, gs.predict(stream_reader.read())
                log_data['best_performance'] = classification_report(y_true, y_pred)
                log_data['accuracy'] = accuracy_score(y_true, y_pred)
                self._log_data_(log_data)
            self.best_models = best_models


def conduct_experiment(data, targets, client='cashlend'):
    tmp = np.unique(targets)
    c = dict()
    cw = compute_class_weight('balanced', tmp, targets)
    for i in xrange (len(tmp)):
        c[tmp[i]] = cw[i] 
    rf = RandomForestClassifier(n_jobs=-1, oob_score = True, class_weight = c, random_state=RANDOM_SEED)
    scoring = "roc_auc" # "f1_micro" uncomment this if performance is too poor
    builder = RNNBuilder(data,targets, c)
    rnn = KerasClassifier(builder.build_rnn)
    rf_params = {        
        "n_estimators": [100,200,500,1000],
        "max_features": [None, "auto", "sqrt"]
    }
    rnn_params = { 
        "a": [2, 4, 10],
    }
    fl = "system/{0}.log".format(datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
    logging.basicConfig(filename=abs_path(fl), level=logging.DEBUG)
    logging.info("experiments for {1} started at {0}".format(unicode(datetime.datetime.now()),client))
    e = Experiment(data,targets,[dict(model=rf,params=rf_params,scoring=scoring,type='rf')], client)    
    e.create_and_train()
    logging.info("experiments for {1} ended at {0}".format(unicode(datetime.datetime.now()),client))
    e.store_models()

