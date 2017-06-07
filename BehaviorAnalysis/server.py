import zmq
import threading
from constants import *




class Server(threading.Thread):
    def __init__(self):
        super(Server,self).__init__()
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.REP)
        self.socket.bind("tcp://*:%s" % PORT)
        self.is_running = True
        self.cond = threading.Lock()

    def run(self):
        while self.is_running:
            m = self.socket.recv_json()


class ClientHandler(threading.Thread):
    def __init__(self):
        super(ClientHandler,self).__init__()
