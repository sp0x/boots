from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split, GridSearchCV, cross_val_score
from keras.wrappers.scikit_learn import KerasClassifier
from keras.models import save_model, load_model, Model
from keras.layers import *
from threading import Thread,Condition
from Queue import Queue
from constants import RANDOM_SEED,MONITOR_RESPONSE_TIME
import numpy as np


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


def build_rnn(a=2):
    hidden = get_hidden_neurons_count(0, 0)
    inp = Input(shape=())
    inp2 = Input(shape=())  # for metadata aka age,wage,blah blah
    mem = LSTM(hidden)(inp)  # keep mem of past behavior
    x = concatenate(mem, inp2)
    x = Dense(hidden, activation='relu')(x)
    x = Dense(hidden, activation='sigmoid')(x)
    x = Dense(hidden, activation='sigmoid')(x)
    out = Dense(1, activation='sigmoid')(x)
    model = Model(inputs=[inp, inp2], outputs=out)
    model.compile(optimizer='', loss='', metrics='', loss_weights=[])


def create_and_train(data, target):
    """
    :param data: vector of features (1 list of vals per user)
    :param target: vector of result ( 1 val per user)
    :return: 
    """
    rf = RandomForestClassifier(n_jobs=-1, oob_score=True)
    rnn = KerasClassifier(build_rnn)
    X_train, X_test, y_train, y_test = train_test_split(data, target, test_size=0.25, random_state=RANDOM_SEED)
    cross_val_score(rf, data, target)
    rf_params = {
        "n_estimators": [5, 10, 20, 30, 35],
        "max_depth": [None, 10, 20, 40, 80],
        "min_samples_split": [2, 8, 16, 32, 64, 128],
        "max_features": ["auto", "sqrt", 10, 20, 30, 40, 50]
    }
    rnn_params = {
        "a": range(2, 10),
    }
    clf = GridSearchCV(rf, rf_params, cv=10, scoring="roc_auc")
    clf.fit(X_train, y_train)
    clf2 = GridSearchCV(rnn, rnn_params, cv=10, scoring="roc_auc")
    clf2.fit(X_train, y_train)
    return clf.best_estimator_, clf2.best_estimator_
