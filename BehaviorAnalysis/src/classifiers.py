import logging
from sklearn.ensemble import RandomForestClassifier, GradientBoostingClassifier
from sklearn.neural_network import MLPClassifier
from sklearn.model_selection import train_test_split, GridSearchCV, cross_val_score
from sklearn.utils.class_weight import compute_class_weight
from sklearn.metrics import classification_report, accuracy_score, confusion_matrix
from keras.wrappers.scikit_learn import KerasClassifier
from keras.models import save_model, load_model, Model
from sklearn import preprocessing
from keras.layers import *
from threading import Thread,Condition
from Queue import Queue
from constants import RANDOM_SEED,MONITOR_RESPONSE_TIME
from utils import save, load, abs_path, latest_file
import numpy as np
import time, datetime
import pandas as pd
import os
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import glob

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
    base_path = "/experiments"
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
        self.log = abs_path(os.path.join(self._for, sub_dir))
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
        models_file = "models_{0}.pickle".format(datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
        models_path = self.get_experiments_dir()
        if not os.path.exists(models_path):
            os.makedirs(models_path)
        models_path = os.path.join(models_path, models_file)
        save(self.best_models, models_path)

    @staticmethod
    def store_model(model, for_client):
        now = datetime.datetime.now().strftime("%Y-%m-%d_%H:%M:%S")
        exp_dir = Experiment.get_experiments_dir_for(for_client)
        if model['type'] == 'rnn':
            hdf5_file = abs_path(os.path.join(exp_dir, 'model_rnn_{0}.hdf5'.format(now)))
            save_model(model['model'], hdf5_file)
            return
        path = abs_path(os.path.join(exp_dir, "model_{0}_{1}.pickle".format(model['type'], now)))
        save(model, path)

    def load_models_file(self, modelsFile=None):
        """
        Loads a model collection file (models_*))
        :param modelsFile: Name of the file to load, in which models are stored
        :return:
        """
        models_path = abs_path(os.path.join(Experiment.base_path, self._for))
        if not os.path.exists(models_path):
            os.makedirs(models_path)
        if modelsFile is not None:
            last_model = os.path.join(models_path, modelsFile)
        else:
            models = glob.glob(os.path.join(models_path, "models_*"))
            last_model = max(models, key=os.path.getctime)

        print "Loading model: {0}".format(last_model)
        return load(last_model)

    def load_model_files(self, model_names):
        """
        :param model_names: Names of the models to load
        :return:
        """
        exp_dir = self.get_experiments_dir()
        models = []
        for m_name in model_names:
            model_path = latest_file(os.path.join(exp_dir, "model_{0}_".format(m_name)))
            print "Loading " + model_path
            if 'hdf5' in m_name:
                model = load_model(abs_path(model_path))
            else:
                model = load(model_path)
            models.append(model)
        return models


    @staticmethod
    def load_model(model_name, for_client):
        mod_path = os.path.join(for_client, model_name)
        if 'hdf5' in model_name:
            return load_model(abs_path(mod_path))
        path = abs_path(mod_path)
        return load(path)


    @staticmethod
    def predict_explain(tree, data, labels, print_proba=False):
        report = ""
        from treeinterpreter import treeinterpreter as ti
        prediction, bias, contributions = ti.predict(tree, data) if not print_proba else ti.predict_proba(tree,data)
        report += "Prediction(s): {0}\n".format(prediction)
        report += "Bias(es): {0}\n".format(bias)
        report += "Feature contributions:\n"
        for c, feature in zip(contributions[0],labels):
            report += feature + str(c) + '\n'
        return report

    @staticmethod
    def predict_explain_non_tree(clf, data, labels, name):
        # Plot feature importance
        try:
            if not hasattr(clf, 'feature_importances_'):
                return 
            feature_importances = clf.feature_importances_
            std = np.std([tree.feature_importances_ for tree in clf.estimators_],
             axis=0)
            indices = np.argsort(feature_importances)[::-1]

            plt.subplot(1, 2, 2)
            plt.title("Feature importances")
            plt.xlabel('Relative Importance')
            plt.bar(range(data.shape[1]), feature_importances[indices],
                color="r", yerr=std[indices], align="center")
            plt.xticks(range(data.shape[1]), indices)
            plt.yticks(pos, labels[indices])
            plt.xlim([-1, data.shape[1]])
            plt.show()
 
            now = datetime.datetime.now().strftime("%Y-%m-%d_%H:%M:%S")
            plt.savefig(abs_path("{0}_importance".format(now)))
        except AttributeError:
            pass

    @staticmethod
    def make_graphs(y_pred_proba,
                    y_true,
                    model='model',
                    company = 'cashlend',
                    prefix=''):
        y_true = np.array(y_true)
        df = pd.DataFrame({'total users': [100] * 10,
                           'buyers': [100 * 0.5] * 10})
        df['userPopulation'] = 100 * df['total users'].cumsum() / \
                                          df['total users'].sum()

        df['buyers cumulative'] = 100 * df['buyers'].cumsum() / \
                                            df['buyers'].sum()

        df.plot('userPopulation', 'buyers cumulative', figsize=(8, 6))
        plt.ylabel('Buyers %')

        df = pd.DataFrame({'buyerProba': y_pred_proba[:,1], 'actual': y_true})
        df.sort_values('buyerProba', ascending=False, inplace=True)
        df['total users'] = 1
        df['userPopulation'] = 100 * df['total users'].cumsum() / df['total users'].sum()
        decile = pd.cut(df['userPopulation'],
                        np.arange(0, 1.1, 0.1) * 100,
                        labels=np.arange(0.1, 1.1, 0.1) * 100)

        df = df.groupby(decile)['actual'].sum().reset_index()
        df = pd.DataFrame(np.concatenate([[[0, 0]], df.values, ]), columns=df.columns)
        df['cumulativeActual'] = 100 * df['actual'].cumsum() / df['actual'].sum()
        df['userPopulation'] = df['userPopulation'].astype('float64')
        df.plot('userPopulation',
                'cumulativeActual',
                xlim=[0, 100], ylim=[0, 110],
                label='Model', figsize=(8, 6))

        plt.plot(df['userPopulation'],
                 df['userPopulation'],
                 label='Random')
        plt.legend(loc='lower right')
        plt.ylabel('Buyers %')
        plt.xlabel('User Population %')
        base_path = abs_path(company)
        plt.savefig(os.path.join(base_path, "{0}{1}_gain.png".format(prefix, model)))
        plt.clf()
        df = df.loc[1:]
        df['lift'] = df['cumulativeActual'] / (df['userPopulation'])
        df.plot('userPopulation', 'lift', ylim=[0, 3], figsize=(8, 6))
        plt.xlabel('User Population %')
        plt.hlines(1, 0, 100)
        plt.savefig(os.path.join(base_path, '{0}{1}_lift.png'.format(prefix, model)))
        plt.clf()
    
    @staticmethod
    def plot_confusion_matrix(cm, classes,
                              normalize=False,
                              title='Confusion matrix',
                              model='model',
                              cmap=plt.cm.Blues,
                              company='',
                              prefix=''):
        """
        This function prints and plots the confusion matrix.
        Normalization can be applied by setting `normalize=True`.
        """
        import itertools
        if normalize:
            cm = cm.astype('float') / cm.sum(axis=1)[:, np.newaxis]
        else:
            print('Confusion matrix, without normalization')
        plt.clf()

        plt.imshow(cm, interpolation='nearest', cmap=cmap)
        plt.title(title)
        plt.colorbar()
        tick_marks = np.arange(len(classes))
        plt.xticks(tick_marks, classes, rotation=45)
        plt.yticks(tick_marks, classes)

        fmt = '.2f' if normalize else 'd'
        thresh = cm.max() / 2.
        for i, j in itertools.product(range(cm.shape[0]), range(cm.shape[1])):
            plt.text(j, i, format(cm[i, j], fmt),
                     horizontalalignment="center",
                     color="white" if cm[i, j] > thresh else "black")

        plt.tight_layout()
        plt.ylabel('True label')
        plt.xlabel('Predicted label')
        filename = '{0}{1}_confusion_matrix.png'.format(prefix, model)
        filepath = None
        if len(company)>0:
            filepath = abs_path(os.path.join(company, filename))
        else:
            filepath = abs_path(filename)
        plt.savefig(filepath)
        plt.clf()

    @staticmethod
    def predict_explain_non_tree2(clf, for_client, data, labels, name):
        c_type = clf['type']
        clf = clf['model']
        # Plot feature importance
        if not hasattr(clf, 'feature_importances_'):
            return
        feature_importances = clf.feature_importances_
        plt.clf()
        std = np.std([tree.feature_importances_ for tree in clf.estimators_],
         axis=0)
        indices = np.argsort(feature_importances)[::-1]

        plt.subplot(1, 2, 2)
        plt.title("Feature importances")
        plt.xlabel('Relative Importance')
        plt.bar(range(data.shape[1]), feature_importances[indices],
         color="r", yerr=std[indices], align="center")
        plt.xticks(range(data.shape[1]), indices)
        plt.yticks(pos, labels[indices])
        plt.xlim([-1, data.shape[1]]) 
        now = datetime.datetime.now().strftime("%Y-%m-%d_%H_%M_%S")
        path = os.path.join(for_client, "importance_{0}_{1}_{2}.png".format(c_type, name, now))
        plt.savefig(abs_path(path))
        plt.clf() 

    @staticmethod
    def t_test(y_pred, y_true, x_train=None, y_train=None, x_test=None, confidence=0.95):
        from scipy.stats import ttest_rel
        y_pred = y_pred.tolist()
        y_true = y_true.tolist() if type(y_true) != list else y_true 
        if x_train and y_train and x_test:
            from sklearn.dummy import DummyClassifier
            dc = DummyClassifier(strategy='most_frequent', random_state=RANDOM_SEED)
            dc.fit(x_train, y_train)
            dc_pred = dc.predict(x_test).tolist()
        else:
            from numpy.random import normal
            dc_pred = np.random.binomial(1, 0.5, len(y_pred)).tolist()#normal(0.5, 0.2,len(y_pred)).tolist()
        dc_acc_scores, model_acc_scores = [], []
        for i in xrange(len(y_pred)):
            dc_acc_scores.append(accuracy_score([y_true[i]], [dc_pred[i]]))
            model_acc_scores.append(accuracy_score([y_true[i]], [y_pred[i]]))
        #diff = np.mean(dc_acc_scores) - np.mean(model_acc_scores)
        t, p = ttest_rel(dc_acc_scores, model_acc_scores)
        print("T-Test results t = {0} p = {1}".format(t,p))
        if p < 1.0-confidence:
            print ("Null Hypothesis rejected. Classifier A better than random classifier")
        else:
            print("Null Hypothesis NOT rejected. Classifier A is random")

    @staticmethod
    def load_dump(company, name):
        file = abs_path(os.path.join(Experiment.base_path, company, "dumps", name))
        dump = load(file)
        return dump

    def get_client_dir(self):        
        return abs_path(os.path.join(self._for) )

    def get_experiments_dir(self):
        return abs_path(os.path.join(Experiment.base_path, self._for))

    @staticmethod
    def get_experiments_dir_for(for_client):
        return abs_path(os.path.join(Experiment.base_path, for_client))

    def get_model(self, type, client):
        exp_dir = self.get_experiments_dir_for(client)
        models = self.best_models
        if self.best_models == None or len(self.best_models) == 0:
            models = self.load_model_files([type])
        for model in models:
            if model['type'] == type:
                return model['model']
        return None

    def create_and_train(self, model_info = None):
        X_train, X_test, y_train, y_test = train_test_split(self.data, self.targets, test_size=0.25, random_state=RANDOM_SEED)
        best_models = []
        for m in self.models:
            #print "training " + m['type']
            gs = GridSearchCV(estimator=m['model'],param_grid=m['params'],cv=10,scoring=m['scoring'])
            if 'nn' in m['type'] or 'lr' in m['type']:
                X_train = preprocessing.normalize(X_train)
            gs.fit(X_train, y_train)
            log_data = dict(results=gs.cv_results_, best=gs.best_params_)
            log_data['model'] = m['type']
            best_models.append(dict(type=m['type'], model=gs.best_estimator_))
            y_true, y_pred = y_test, gs.predict(X_test)
            y_pred_proba = gs.predict_proba(X_test)
            log_data['best_performance'] = classification_report(y_true, y_pred)
            log_data['accuracy'] = accuracy_score(y_true, y_pred)
            self._log_data_(log_data)            
            cm = confusion_matrix(y_true, y_pred)
            m['model'] = gs.best_estimator_
            m['info'] = model_info
            Experiment.store_model(m, self._for)
            Experiment.plot_confusion_matrix(cm, ['Non-Buyers', 'Buyers'], False, model=m['type'], company=self._for)
            dump_dir = os.path.join(self.get_experiments_dir(), 'dumps')
            if not os.path.exists(dump_dir):
                os.makedirs(dump_dir)
            save(X_train, os.path.join(dump_dir, "x_train_{0}.dmp".format(log_data['model']) ))
            save(X_test, os.path.join(dump_dir, "x_test_{0}.dmp".format(log_data['model'])))
            save(y_train, os.path.join(dump_dir, "y_train_{0}.dmp".format(log_data['model'])))
            save(y_pred, os.path.join(dump_dir, "y_pred_{0}.dmp".format(log_data['model'])))
            save(y_pred_proba, os.path.join(dump_dir, "y_pred_proba_{0}.dmp".format(log_data['model'])))
            save(y_true, os.path.join(dump_dir, "y_true_{0}.dmp".format(log_data['model'])))
            Experiment.make_graphs(y_pred_proba, y_true, m['type'], self._for)

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



def plot_cutoff(model, data, data_y, client='cashlend'):
    import seaborn as sns
    classifier = model['model']
    c_type = model['type']

    def cutoff_predict(x, cutoff):
        return (classifier.predict_proba(x)[:, 1] > cutoff).astype(int)

    def custom_f1(cutoff):
        def f1_cutoff(clf, x, y):
            import sklearn
            ypred = cutoff_predict(x, cutoff)
            return sklearn.metrics.f1_score(y, ypred)
        return f1_cutoff

    def custom_roc_auc(cutoff):
        def roc_cutoff(clf, x, y):
            import sklearn
            ypred = cutoff_predict(x, cutoff)
            return sklearn.metrics.roc_auc_score(y, ypred)
        return roc_cutoff

    scores = []
    cutoffs = np.arange(0.1, 1.0, 0.1)
    max_median = 0
    for cut_off in cutoffs:
        # In case of supervised learning
        # validated = cross_val_score(classifier, data, target, cv=10, scoring=custom_roc_auc(cut_off))
        validated = cross_val_score(classifier, data, data_y, cv=10, scoring=custom_roc_auc(cut_off))
        median = np.median(validated)
        if median > max_median:
            max_median = median
        scores.append(validated)
    # print "Cutoffs"
    # print scores
    sns.boxplot(scores, names=cutoffs)
    plt.title("F scores for each tree")
    plt.xlabel("each cut off value")
    plt.ylabel("custom F score")
    fig_path = abs_path(os.path.join(client, 'cutoff_{0}.png'.format(c_type))) 
    # fig_path_1 = os.path.join(client, abs_path('cutoff_{0}_alt.png'.format(c_type)))
    plt.savefig(fig_path)
    return max_median


def graph_experiment(data, targets, client='cashlend', model=None, modelsFile=None):
    X_train, X_test, y_train, y_test = train_test_split(data, targets, test_size=0.25, random_state=RANDOM_SEED)
    exp = Experiment(None, None, None, client)
    if model is not None:
        models = [model]
    else:
         models = exp.load_models_file(modelsFile)  # load_model_files(['gba'])

    for m in models:
        #print "training " + m['type']
        model = m['model']
        model_type = m['type']
        y_true, y_pred = y_test, model.predict(X_test)
        y_pred_proba = model.predict_proba(X_test)
        cm = confusion_matrix(y_true, y_pred)
        Experiment.plot_confusion_matrix(cm, ['Non-Buyers', 'Buyers'], False, model=model_type, company=client, prefix='pred_')
        Experiment.make_graphs(y_pred_proba, y_true, model_type, client, 'pred_')


def conduct_experiment(data, targets, client='cashlend', extras=None):
    tmp = np.unique(targets)
    c = dict()
    cw = compute_class_weight('balanced', tmp, targets)

    for i in xrange(len(tmp)):
        c[tmp[i]] = cw[i]

    rf = RandomForestClassifier(n_jobs=-1, oob_score = True, class_weight = c, random_state=RANDOM_SEED)
    scoring = "roc_auc"  # "f1_micro" uncomment this if performance is too poor
    builder = RNNBuilder(data, targets, c)
    rnn = KerasClassifier(builder.build_rnn)
    gba = GradientBoostingClassifier(random_state=RANDOM_SEED)
    gba_params = {
        # 'learning_rate': [0.1, 0.05, 0.02, 0.01],
        'max_depth': [3, 6, 12, 24],
        'n_estimators': [100, 200, 300]
    }
    rf_params = {        
        "n_estimators": [100, 200, 300, 500],
        "max_features": [None, "auto"],
        "max_depth": [20, 40, 80],
        "class_weight": [c]
    }
    rnn_params = { 
        "a": [2, 4, 10],
    }
    fl = "system/{0}.log".format(datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
    logging.basicConfig(filename=abs_path(fl), level=logging.DEBUG)
    logging.info("experiments for {1} started at {0}".format(unicode(datetime.datetime.now()), client))
    e = Experiment(data, targets, [
        # {'model': rnn, 'params': rnn_params, 'scoring': scoring, 'type': 'rnn'},
        {'model': gba, 'params': gba_params, 'scoring': scoring, 'type': 'gba'},
        {'model': rf, 'params': rf_params, 'scoring': scoring, 'type': 'rf'}
        # {'model': lr, 'params': lr_params, 'scoring': scoring, 'type': 'lr'},
        # {'model': mlp, 'params': mlp_params, 'scoring': scoring, 'type': 'mlpnn'},
    ], client)
    e.create_and_train(extras)
    logging.info("experiments for {1} ended at {0}".format(unicode(datetime.datetime.now()), client))
    # e.store_models()
    # rf_model = e.get_model('rf', client)
    # gba_model = e.get_model('gba', client)
    # rf_prediction = rf_model.predict_proba(data)
    # gba_prediction = gba_model.predict_proba(data)
    # train_balancer(rf_prediction, gba_prediction, targets, client)
    return e

def create_balancer(input_data, target, classifier_types, client='netinfo'):
    exp = Experiment(None, None, None, client)
    model_files = exp.load_model_files(classifier_types)
    predictions = []
    if len(model_files) > 2:
        raise ValueError('Only 2 classifiers supported for now!')
    for model in model_files:
        m_type = model['type']
        model_obj = model['model']
        print "Predicting with {0} - {1}".format(m_type, len(input_data))
        prediction = model_obj.predict_proba(input_data)
        predictions.append(prediction)
    arguments = predictions
    arguments.append(target)
    arguments.append(client)
    train_balancer(*arguments)

def train_balancer(predict1, predict2, targets, client='netinfo'):
    tmp = np.unique(targets)
    c = dict()
    cw = compute_class_weight('balanced', tmp, targets)
    data = np.array(zip(predict1[:,1],predict2[:,1]))
    for i in xrange(len(tmp)):
        c[tmp[i]] = cw[i]
    rf = RandomForestClassifier(n_jobs=-1, oob_score=True, class_weight=c, random_state=RANDOM_SEED)
    mlp = MLPClassifier(activation='logistic', solver='lbfgs', random_state=RANDOM_SEED)
    scoring = "f1_micro"
    rf_params = {
        "n_estimators": [20, 80, 100],
        "max_features": [None],
        "max_depth": [10, 20],
        "class_weight": [c]
    }
    mlp_params = {
        "hidden_layer_sizes": [(100, 100, 100, 100, 100),
                              (80, 100, 140, 100, 80),
                              (80, 100, 150, 80, 40)]
    }
    fl = "system/{0}.log".format(datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"))
    logging.basicConfig(filename=abs_path(fl), level=logging.DEBUG)
    logging.info("experiments for {1} started at {0}".format(unicode(datetime.datetime.now()), client))
    e = Experiment(data, targets, [
        {'model': rf, 'params': rf_params, 'scoring': scoring, 'type': 'brf'},
        {'model': mlp, 'params': mlp_params, 'scoring': scoring, 'type': 'bdnn'}
    ], client)
    e.create_and_train()
    logging.info("experiments for {1} ended at {0}".format(unicode(datetime.datetime.now()), client))
    e.store_models()