import threading
from pymongo import MongoClient
from sklearn import preprocessing


class MongoDataStream(object):
    def __init__(self, host, password, db, collection, start_date, end_date, chunk_size=10000, max_items=None):
        self.client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
        self.source = self.client[db][collection]
        # total number of batches of data in the db/collection
        if not max_items:
            self.len  = self.source.find({'Document.g_timestamp': {'$gte': start_date, '$lt': end_date}}).count()
        else:
            self.len = max_items
        self.slices = int(self.len / chunk_size)
        self.data = []   # always keep 2 slices 1 to read from and 1 as a buffer
        self.lock = threading.Lock()
        self.cond = threading.Condition()
        self.available = True
        self.start = start_date
        self.end = end_date
        self.offset = 0
        self.chunk_size = chunk_size
        self.data = [self.__fetch__() for _ in xrange(2)]
        self.order = []

    def _get_next_ (self,offset):
        return self.order[offset:offset + self.chunk_size]

    def reset_stream(self):
        with self.lock:
            self.offset = 0
            self.slices = int(self.len / self.chunk_size)

    def __fetch__(self):
        with self.lock:
            offset = self.offset
        ids = self._get_next_(offset)
        data = self.source.find({'Document.g_timestamp': {'$gte': self.start, '$lt': self.end},
                                 'Document.uuid': {"$in": ids}})
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
        """Retrieves the ids of all users in the db with their status(paid/unpaid)"""
        ids = self.source.find({"UserId": "123123123"}, {"_id": 0, "Document.uuid": 1, "Document.is_paying": 1})
        payment_stats = self.source.find() # insert the query here
        return ids, payment_stats

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


class MongoDataStreamReader(object):
    def __init__(self, stream, features, normalize=False):
        self.stream = stream
        self.features = features
        self.normalize = normalize

    def set_order(self, ids):
        self.stream.order = ids

    def reset_cursor(self):
        self.stream.reset_stream()

    def get_training_set(self):
        return self.stream.get_doc_ids()

    def set_normalize(self, value):
        self.normalize = value

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
                if self.normalize:
                    yield preprocessing.normalize(doc)
                else:
                    yield doc
