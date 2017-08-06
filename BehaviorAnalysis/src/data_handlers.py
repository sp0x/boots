import threading
from pymongo import MongoClient


class MongoDataStream:
    def __init__(self, host, password, db, collection, stat_date, end_date, chunk_size=10000, max_items=None):
        self.client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
        self.source = self.client[db][collection]
        # total number of batches of data in the db/collection
        if not max_items:
            self.slices = int(self.source.find({'Document.g_timestamp': {'$gte': self.start, '$lt': self.end}}).count()/chunk_size)
        else:
            self.slices = int(max_items / chunk_size)
        self.data = []   # always keep 2 slices 1 to read from and 1 as a buffer
        self.lock = threading.Lock()
        self.cond = threading.Condition()
        self.available = True
        self.start = stat_date
        self.end = end_date
        self.offset = 0
        self.chunk_size = chunk_size
        self.data = [self.__fetch__() for _ in xrange(2)]

    def __fetch__(self):
        data = self.source.find({'Document.g_timestamp': {'$gte': self.start, '$lt': self.end}})\
            .skip(self.offset).limit(self.chunk_size)
        if self.slices == 0:return
        with self.lock:
            self.data.append(data)
            self.slices -= 1
            self.offset += self.chunk_size
            self.available = True
        with self.cond:
            self.cond.notifyAll()

    def __pre_load__(self):
        t = threading.Thread(target=self.__fetch__)
        t.daemon = True
        t.start()

    def get_doc_ids(self):
        return self.source.find({"UserId": "123123123"}, {"_id": 0, "Document.uuid": 1, "Document.is_paying": 1})

    def read(self):
        while len(self.data) > 0:
            with self.lock:
                t_avl = self.available or self.slices == 0
            while not t_avl:
                with self.cond:
                    self.cond.wait(1)
                with self.lock:
                    t_avl = self.available or self.slices == 0
            with self.lock:
                d = self.data.pop()
                self.available = False
            yield d
            self.__pre_load__()
        return


class MongoDataStreamReader:
    def __init__(self, stream, features):
        self.stream = stream
        self.features = features

    def read(self):
        data = self.stream.read()
        for d in data:
            doc = []
            if d is None:
                return
            for dd in d:
                tmp = dd['Document']
                for f in self.features:
                    doc.append(tmp[f])
                yield doc
