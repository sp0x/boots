import time
from constants import *
from classifiers import conduct_experiment,Experiment
from features import BNode, BTree, lin 
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
import settings
 
#type used for web sessions
userSessionTypeId = "598f20d002d2516dd0dbcee2"
userTypeId = "59cbc103003e730508e87c2c"

week4Start = datetime(2017, 7, 17, 0, 0, 0)
appId = "123123123"

db = settings.get_db()
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