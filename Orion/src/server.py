import zmq
import threading
import time
from constants import *
from classifiers import conduct_experiment,Experiment,build, train
from psutil import virtual_memory
import os
import multiprocessing

class Server(threading.Thread):
    def __init__(self):
        super(Server,self).__init__()
        self.context = zmq.Context()
        self.in_stream = self.context.socket(zmq.PULL)
        self.out_stream = self.context.socket(zmq.PUSH)
        self.in_stream.bind("tcp://%s:%d" % (LISTEN_ADDR, INPUT_PORT))
        self.out_stream.bind("tcp://%s:%d" % (LISTEN_ADDR, OUTPUT_PORT))
        self.is_running = True
        self.cond = threading.Lock()
        self.pool = multiprocessing.Pool()
        cache_size = os.environ.get('MCACHE_SIZE',5)
        self.cache = ModelCache(cache_size)

    def shutdown(self):
        with self.cond:
            self.is_running = False
            print("Waiting for all running experiments to finish executing")
            self.pool.close()
            self.pool.join()

    def reply(self, msg):
        try:
            self.out_stream.send_json(msg)
        except Exception as e:
            pass

    def run(self):
        print ("Server running and listening at tcp://%s:%d" % (LISTEN_ADDR, INPUT_PORT))
        while self.is_running:
            while self.in_stream.poll(0):
                resp = {}
                try:
                    msg = self.in_stream.recv_json()
                    data = msg['data']
                    op = msg['op']
                    company = msg['company']
                    seq = msg.get('seq')
                    if op == MAKE_PREDICTION:
                        # make this load and keep a model in memory at all times once we can afford to do that
                        m_id = msg.get('model_id')
                        p_type = msg.get('p_type', 'proba')
                        model = self.cache.fetch(m_id)
                        if p_type == 'proba':
                            resp = {'results':{'value':model['model'].predict_proba(data), 'model':model['type']}}
                        elif p_type == 'log_proba':
                            resp = {'results':{'value':model['model'].predict_log_proba(data), 'model':model['type']}}
                        else:
                            resp = {'results':{'value':model['model'].predict(data), 'model':model['type']}}
                    elif op == TRAIN:
                        exp = build(msg['params']) 
                        self.pool.map_async(train,(exp,))   
                        resp = {'ids': exp.get_model_ids()}
                    resp.update({'seq':seq})                    
                except Exception as e:
                    resp = {'status' : 'err', 'message' : str(e)}
                    print("Error: %s" % str(e)) 
                self.reply(resp)


class ModelCache:
    
    cache = {} # {m_id:model}
    order = [] # [m_id] in order of calling/adding least called always at the bottom
    cache_max_size = 3
    
    def __init__(self, max_size):
        cache_max_size = max_size

    def clean(self):
        'uses LRU to clean up unused models from cache'
        lru = ordered.pop(-1)
        del cache[lru]

    def add(self, model):
        if len(order) + 1 > cache_max_size:
            self.clean()
        self._add(model)

    def _add(self, model):
        _id = model['id']
        cache[_id] = model
        order.insert(0, _id)

    def fetch(self, m_id, company):
        model = cache.get(m_id)
        if model:
            order.insert(0, order.pop(m_id))
        else:
            if len(order) + 1 > cache_max_size:
                self.clean()
            model = Experiment.load_model(company, m_id)
            self._add(model)
        return model

    