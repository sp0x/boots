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

 
#type used for web sessions
userSessionTypeId = "598f20d002d2516dd0dbcee2"
#sessionsPath = "testData/Netinfo/payingBrowsingSessionsDaySorted.csv"
password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
host = "10.10.1.5"

client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
db = client.netvoid
documents_col = db.IntegratedDocument

#go through the sessions
payingSessionsTree = BTree() 
paying_users = documents_col.find({
    "TypeId" : userSessionTypeId,
    "Document.is_paying" : 1 #todo: add week limit?
})
items = []
lastDay = None
for paying_user_day in paying_users:
    print paying_user_day
    day = paying_user_day["Document"]["Created"]
    cr_items = []
    for item in paying_user_day["Document"]["Sessions"]: 
        host = item["Domain"]
        duration = parse_timespan(item["Duration"]) #00:02:14.3457500 
        duration = duration.total_seconds() 
        cr_items.append({ 'time' : duration, 'label' : host })
    payingSessionsTree.build(cr_items)

 
for non_paying_user in documents_col.find({
    "TypeId" : userSessionTypeId,
    "Document.is_paying" : 0
}).distinct("Document.UserId"):
    day = non_paying_user["Document"]["Created"]
    userId = non_paying_user["Document"]["UserId"] 
    userSessionDays = documents_col.find({
        "TypeId" : userSessionTypeId,
        "Document.is_paying" : 0,
        "Document.UserId" : userId #todo: add week limit
    })
    userTree = BTree() 
    for sessionDay in userSessionDays:
        day = sessionDay["Document"]["Created"]
        cr_items = []
        for session in sessionDay["Document"]["Sessions"]:
            host = session["Domain"]
            duration = parse_timespan(session["Duration"]).total_seconds()
            cr_items.append({ 'time' : duration , 'label' : host})   
        userTree.build(cr_items)
    #lin
    f1 = lin(payingSessionsTree, userTree, "time")
    f2 = lin(payingSessionsTree, userTree, "frequency")
    #update user features



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