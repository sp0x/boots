import zmq
import threading
import time
from constants import *
from classifiers import conduct_experiment,Experiment,build_and_train


class Server(threading.Thread):
    def __init__(self):
        super(Server,self).__init__()
        self.context = zmq.Context()
        self.in_stream = self.context.socket(zmq.PULL)
        self.out_stream = self.context.socket(zmq.PUSH)
        self.in_stream.bind("tcp://%s:%d" % (LISTEN_ADDR, INPUT_PORT))
        self.out_stream.bind("tcp://%s:%d" % (LISTEN_ADDR, OUTPUT_PORT))
        self.is_running = True
        self.experimental_data = dict()
        self.cond = threading.Lock()
        self.experiments = []

    def shutdown(self):
        with self.cond:
            self.is_running = False
            if any(map(lambda x: x.is_alive,self.experiments)):
                print("Waiting for all running experiments to finish executing")
                for e in self.experiments:
                    e.join()

    def run(self):
        print ("Server running and listening at tcp://%s:%d" % (LISTEN_ADDR, INPUT_PORT))
        while self.is_running:
            while self.in_stream.poll(0):
                try:
                    msg = self.in_stream.recv_json()
                    data = msg['data']
                    op = msg['op']
                    company = msg['company']
                    seq = msg.get('seq')
                    resp = {} 
                    if op == MAKE_PREDICTION:
                        m_id = msg.get('model_id')
                        models = Experiment.load_models_file(company)
                        resp = {'results':[{'value':m['model'].predict(data),'model':m['type']} for m in models]}
                    elif op == TRAIN:
                        build_and_train(msg['params'])
                    resp.update({'seq':seq})
                    self.out_stream.send_json(resp)
                except Exception, e:
                    resp = {'status' : 'err', 'message' : str(e)}
                    self.out_stream.send_json(resp)
                    print("Error: %s" % str(e)) 



class ClientHandler(threading.Thread):
    def __init__(self):
        super(ClientHandler,self).__init__()
