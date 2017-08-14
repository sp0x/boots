import time
from constants import *
from classifiers import conduct_experiment,Experiment
from pymongo import MongoClient
import urllib
import sys
import pymongo

from datetime import datetime, timedelta

##host is local
password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
host = "10.10.1.5"

client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
db = client.netvoid
collection = db.IntegratedDocument
userTypeId = "598da0a2bff3d758b4025d21" 
appId = "123123123"

weeksAvailable = collection.find({    
    "UserId" : appId,
    "TypeId" : userTypeId
}).distinct("Document.g_timestamp")
weeksAvailable.sort()

#our test week
lastWeek = weeksAvailable.pop()
targetData = []
inputData = []
for index, week in enumerate(weeksAvailable):
    if index== (len(weeksAvailable) -1 ):
        next_week = lastWeek
    else:
        next_week = weeksAvailable[index+1]
    print "Preparing week: " + str(week) + "    " + str(index+1) + "/" + str(len(weeksAvailable))
    weekData = collection.find({
        "UserId" : appId,
        "TypeId" : userTypeId,
        "Document.g_timestamp" : {'$gte': week, '$lt': next_week}
    }, { 
    "Document.uuid" : 1,
    "Document.noticed_date" : 1,
    "Document.is_paying" : 1,
    "Document.visits_on_weekends" : 1,
    "Document.p_online_weekend" : 1,
    "Document.days_visited_ebag" : 1,
    "Document.time_spent_ebag" : 1,
    "Document.time_spent_on_mobile_sites" : 1,
    "Document.mobile_visits" : 1,
    "Document.visited_ebag" : 1,
    "Document.p_buy_age_group" : 1,
    "Document.p_buy_gender_group" : 1,
    "Document.p_visit_ebag_age" : 1,
    "Document.p_visit_ebag_gender" : 1,
    "Document.p_to_go_online" : 1,
    "Document.avg_time_spent_on_high_pageranksites" : 1,
    "Document.highranking_page_0" : 1,
    "Document.highranking_page_1" : 1,
    "Document.highranking_page_2" : 1,
    "Document.highranking_page_3" : 1,
    "Document.highranking_page_4" : 1,
    "Document.time_spent_online" : 1,
    "path_similarity_score" : 1,
    "path_similarity_score_time_spent" : 1
    }).batch_size(10000)
    next_week_end = next_week + timedelta(days=7)
    for week_session in weekData:
        tmpDoc = week_session["Document"]
        uuid = tmpDoc["uuid"]
        inputData.append([
            tmpDoc["visits_on_weekends"],
            tmpDoc["p_online_weekend"],
            tmpDoc["days_visited_ebag"],
            tmpDoc["time_spent_ebag"],
            tmpDoc["time_spent_on_mobile_sites"],
            tmpDoc["mobile_visits"],
            tmpDoc["visited_ebag"],
            tmpDoc["p_buy_age_group"],
            tmpDoc["p_buy_gender_group"],
            tmpDoc["p_visit_ebag_age"],
            tmpDoc["p_visit_ebag_gender"],
            tmpDoc["p_to_go_online"],
            tmpDoc["avg_time_spent_on_high_pageranksites"],
            tmpDoc["highranking_page_0"],
            tmpDoc["highranking_page_1"],
            tmpDoc["highranking_page_2"],
            tmpDoc["highranking_page_3"],
            tmpDoc["highranking_page_4"],
            tmpDoc["time_spent_online"],
            week_session["path_similarity_score"],
            week_session["path_similarity_score_time_spent"]
        ])
        #next week user purchases
        purchasesCount = collection.find({
            "TypeId" :  userTypeId,
            "UserId" : appId,
            "Document.noticed_date" : { "$gte" : next_week, "$lt" : next_week_end  },
            "Document.uuid" : uuid
        }).count()
        if purchasesCount > 0:
            targetData.append(1)
        else:
            targetData.append(0)

print "Prepared " + str(len(data)) + " items"
conduct_experiment(inputData, targetData, 'Netinfo')

