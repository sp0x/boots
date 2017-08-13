import time
from constants import *
from classifiers import conduct_experiment,Experiment
from features import BNode, BTree, lin
from pymongo import MongoClient
import urllib
from os import walk
import csv
from datetime import datetime
import cPickle as pickle
import pandas as pd
from datetime import datetime, timedelta
import re
from utils import parse_timespan
from Queue import Queue
from threading import Thread, Lock, Condition

 
#type used for web sessions
userSessionTypeId = "598f20d002d2516dd0dbcee2"
week4Start = datetime(2017, 7, 17, 0, 0, 0)
appId = "123123123"
#sessionsPath = "testData/Netinfo/payingBrowsingSessionsDaySorted.csv"
password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
host = "10.10.1.5"

client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
db = client.netvoid
documents_col = db.IntegratedDocument


class BuildWorker(Thread):

    def __init__(self,par):
        super(BuildWorker,self).__init__()
        self.daemon = True        
        self.par = par

    def run(self):
        while self.par.work_available():
            job = self.par.__get_work__()
            userTree = BTree()
            itemCount = 0
            uuid = -1
            #job_size = len(job)
            #print job
            #print "Worker got job with size: " + str(job_size)
            day_items = []
            sessionDay = job
            
            sessions = sessionDay["Document"]["Sessions"]
            uuid = sessionDay['Document']['UserId']
            print "adding sessions for {0}".format(uuid)
            for session in sessions:
                host = session["Domain"]
                duration = parse_timespan(session["Duration"]).total_seconds()
                day_items.append({'time': duration, 'label': host})
                itemCount += 1
            userTree.build(day_items)
            print "added sessions for {0}".format(uuid)
            self.par.push_result(userTree,uuid)
        print "Worker stopping because no work is available"


class MassTreeBuilder:
    def __init__(self, batch_size, store):
        self.userSessionTypeId = "598f20d002d2516dd0dbcee2"
        appId = "123123123"
        # sessionsPath = "testData/Netinfo/payingBrowsingSessionsDaySorted.csv"
        password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
        host = "10.10.1.5"
        client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
        db = client.netvoid
        self.documents_col = db.IntegratedDocument
        self.work_queue = Queue()
        self.lock = Lock()
        self.batch_size = batch_size
        self.res_queue = Queue()
        self.remaining = self.documents_col.find({
            "TypeId": self.userSessionTypeId,
            "Document.is_paying": 0,
            # "Document.Created" : { "$lt" : week4Start }
        }).distinct("Document.UserId")
        self.workers = []
        self.collecting_data = False
        self.io_lock = Lock()
        self.store = store
        [self.__fetch__() for _ in xrange(2)]

    def __fetch__(self):
        self.collecting_data = True
        with self.lock:
            ids = self.remaining[:self.batch_size]
            del self.remaining[:self.batch_size]
        sessions = documents_col.find({
            "TypeId": self.userSessionTypeId,
            "Document.is_paying": 0,
            "Document.UserId": {"$in": ids},
        }).batch_size(self.batch_size)
        for d in sessions:
            self.work_queue.put(d)
        self.collecting_data = False

    def __get_work__(self):
        with self.lock:
            rem = len(self.remaining)
        if self.work_queue.qsize() <= self.batch_size and rem>0:
            if not self.collecting_data:
                t = Thread(target=self.__fetch__)
                t.daemon = True
                t.start()
        job = self.work_queue.get()
        self.work_queue.task_done()
        return job

    def work_available(self):
        with self.lock:
            rem = len(self.remaining)
        return rem != 0

    def push_result(self, res, id=None):
        if self.store:
            from utils import abs_path, save
            import os
            path = abs_path(os.path.join("netinfo",id +".pickle"))
            with self.io_lock:
                save(res, path)
        else:
            self.res_queue.put(res)

    def build(self, max_threads=8):
        for i in xrange(max_threads):
            w = BuildWorker(self)
            w.start()
            self.workers.append(w)

    def get_result(self):
        if not self.work_available():
            return list(self.res_queue.queue)
        return []



#go through the sessions
payingSessionsTree = BTree() 
paying_users = documents_col.find({
    "TypeId" : userSessionTypeId,
    "Document.is_paying" : 1, #todo: add week limit?
    #"Document.Created" : { "$lt" : week4Start } 
})
items = []
lastDay = None
for paying_user_day in paying_users:  
    cr_items = []
    for item in paying_user_day["Document"]["Sessions"]: 
        host = item["Domain"]
        duration = parse_timespan(item["Duration"]) #00:02:14.3457500 
        duration = duration.total_seconds() 
        cr_items.append({ 'time' : duration, 'label' : host })
    payingSessionsTree.build(cr_items)

user_features = dict()
builder = MassTreeBuilder(10000, False)
builder.build()
builder.get_result()

# non_paying_user_ids = documents_col.find({
#     "TypeId" : userSessionTypeId,
#     "Document.is_paying" : 0,  
#     #"Document.Created" : { "$lt" : week4Start } 
# }).distinct("Document.UserId")
# npu_count = len(non_paying_user_ids)
# cr_user_ix = 0
# print "Building user features: " + str(npu_count)
# for non_paying_user_id in non_paying_user_ids: 
#     userId = non_paying_user_id
#     userSessionDays = documents_col.find({
#         "TypeId" : userSessionTypeId,
#         "Document.is_paying" : 0,
#         "Document.UserId" : userId,
#         #"Document.Created" : { "$lt" : week4Start } 
#     })
#     userTree = BTree() 
#     itemCount = 0
#     for sessionDay in userSessionDays: 
#         day_items = [] 
#         for session in sessionDay["Document"]["Sessions"]: 
#             host = session["Domain"]
#             duration = parse_timespan(session["Duration"]).total_seconds()
#             day_items.append({ 'time' : duration , 'label' : host})
#             itemCount += 1
#         userTree.build(day_items)
#     #lin 
#     f1 = lin(payingSessionsTree, userTree, "time")
#     f2 = lin(payingSessionsTree, userTree, "frequency")
#     perc = "%.3f" % ((cr_user_ix / float(npu_count)) * 100)
#     print "[" + str(itemCount) + "] " + perc + "% Built trees for: " + userId + " " + str(f1) + " - " + str(f2)
#     user_features[userId] = { 
#         'path_similarity_score' : f2,
#         'path_similarity_score_time_spent' :f1
#     }
#     cr_user_ix += 1
#     #update user features
# print "Updating user features"
# for user_id, user_f in user_features.iteritems():
#     score = user_f['path_similarity_score']
#     timescore = user_f['path_similarity_score_time_spent']
#     documents_col.update({
#         "UserId" : appId,
#         "Document.uuid" : user_id
#     },  {'$set': {
#         "path_similarity_score" : score,
#         "path_similarity_score_time_spent" : timescore
#     }}, multi=True)



# allData = userDaysCollection.find({
#     "UserId" : "123123123"
# }, {
#      "Document.uuid" : 1,
#     "Document.noticed_date" : 1,
#     "Document.is_paying" : 1,
#     "Document.visits_on_weekends" : 1,
#     "Document.p_online_weekend" : 1,
#     "Document.days_visited_ebag" : 1,
#     "Document.time_spent_ebag" : 1,
#     "Document.time_spent_on_mobile_sites" : 1,
#     "Document.mobile_visits" : 1,
#     "Document.visited_ebag" : 1,
#     "Document.p_buy_age_group" : 1,
#     "Document.p_buy_gender_group" : 1,
#     "Document.p_visit_ebag_age" : 1,
#     "Document.p_visit_ebag_gender" : 1,
#     "Document.p_to_go_online" : 1,
#     "Document.avg_time_spent_on_high_pageranksites" : 1,
#     "Document.highranking_page_0" : 1,
#     "Document.highranking_page_1" : 1,
#     "Document.highranking_page_2" : 1,
#     "Document.highranking_page_3" : 1,
#     "Document.highranking_page_4" : 1,
#     "Document.time_spent_online" : 1
# } ).limit(1000 * 200)

# #build all the data for first 3 weeks
# b1 = BTree()

# #b1.predict_proba
# b1.build([{"time": 10, "label": "a"},
#               {"time": 13, "label": "b"},
#               {"time": 2, "label": "c"}])
# b2 = BTree()
# b2.build([{"time": 1, "label": "a"},
#               {"time": 2, "label": "b"}])
# print lin(b1, b2, "frequency")