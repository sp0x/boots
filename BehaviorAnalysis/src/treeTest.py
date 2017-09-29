import time
from constants import *
from classifiers import conduct_experiment,Experiment
from features import BNode, BTree, lin
from pymongo import MongoClient
from pymongo import UpdateMany
import urllib
from os import walk
import csv
from datetime import datetime
import cPickle as pickle
import pandas as pd
from datetime import datetime, timedelta
import re
from utils import parse_timespan, chunks
from Queue import Queue
from threading import Thread, Lock, Condition
from time import sleep
import sys
from trees import BuildWorker, MassTreeBuilder

 
#type used for web sessions
userSessionTypeId = "598f20d002d2516dd0dbcee2"
userTypeId = "59cbc103003e730508e87c2c"

week4Start = datetime(2017, 7, 17, 0, 0, 0)
appId = "123123123"
#sessionsPath = "testData/Netinfo/payingBrowsingSessionsDaySorted.csv"
password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
host = "10.10.1.5"

client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
db = client.netvoid
documents_col = db.IntegratedDocument

weeksAvailable = documents_col.find({
    "UserId": appId,
    "TypeId": userTypeId
}).distinct("Document.g_timestamp")
weeksAvailable.sort()

#go through the sessions
payingSessionsTree = BTree() 
paying_users = documents_col.find({
    "TypeId" : userSessionTypeId,
    "Document.is_paying": 1,
    # "Document.Created": {"$gte": weeksAvailable[1], "$lte": weeksAvailable[2] + timedelta(days=7)},
    # "Document.Created" : { "$lt" : week4Start }
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
builder = MassTreeBuilder(200, False, {
    "TypeId": userSessionTypeId,
    "Document.is_paying": 0,  # they have to be non paying
    # "Document.Created": {"$gte": weeksAvailable[1], "$lte": weeksAvailable[2] + timedelta(days=7)},
}, "Document.UserId")
results = builder.make(8)

print "Finished with {0} items!".format(len(results))
for chnk in chunks(results, 1000):
    items =[]
    for item in chnk:
        uuid = item['uuid']
        tree = item['result']
        simtime = lin(payingSessionsTree, tree, "time")
        simscore = lin(payingSessionsTree, tree, "frequency")

        up = UpdateMany({
            "UserId": appId,
            "Document.uuid": uuid
        },  {'$set': {
            "Document.path_similarity_score": simscore,
            "Document.path_similarity_score_time_spent": simtime
        }})
        items.append(up)
    documents_col.bulk_write(items)


# p = Pool(8)
# p.map(gen_features, results)



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