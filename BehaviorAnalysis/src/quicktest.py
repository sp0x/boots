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
weekLimit = 4
weeksAvailable = weeksAvailable[ (-1) * weekLimit:] #last n weeks
lastWeek = weeksAvailable.pop()
targetData = []
inputData = []
limit = 2 * 100 * 1000
userlimit = 15 * 1000


for index, week in enumerate(weeksAvailable):
    if index== (len(weeksAvailable) -1 ):
        next_week = lastWeek
    else:
        next_week = weeksAvailable[index+1]
    print "Preparing week: " + str(week) + "    " + str(index+1) + "/" + str(len(weeksAvailable))
    next_week_purchases = dict()

    pipeline = [
        {"$match" : { 
            "TypeId" : userTypeId, 
            "UserId" : appId,
            "Document.g_timestamp" : {'$gte': week, '$lt': next_week},
         }
        },
        {"$group": {
            "_id": {
                "uuid" : "$Document.uuid",
                "week_start" : "$Document.g_timestamp"
            }, 
            "visits_count": {"$sum": 1}, 
                "visits_on_weekends" : { "$avg" : "$Document.visits_on_weekends"},
                "p_online_weekend" : { "$avg" : "$Document.p_online_weekend"},
                "days_visited_ebag" : { "$avg" : "$Document.days_visited_ebag"},
                "time_spent_ebag" : { "$avg" : "$Document.time_spent_ebag"},
                "time_spent_on_mobile_sites" : { "$avg" : "$Document.time_spent_on_mobile_sites"},
                "mobile_visits" : { "$avg" : "$Document.mobile_visits"},
                "visited_ebag" : { "$avg" : "$Document.visited_ebag"},
                "p_buy_age_group" : { "$avg" : "$Document.p_buy_age_group"},
                "p_buy_gender_group" : { "$avg" : "$Document.p_buy_gender_group"},
                "p_visit_ebag_age" : { "$avg" : "$Document.p_visit_ebag_age"},
                "p_visit_ebag_gender" : { "$avg" : "$Document.p_visit_ebag_gender"},
                "p_to_go_online" : { "$avg" : "$Document.p_to_go_online"},
                "avg_time_spent_on_high_pageranksites" : { "$avg" : "$Document.avg_time_spent_on_high_pageranksites"},
                "highranking_page_0" : { "$avg" : "$Document.highranking_page_0"},
                "highranking_page_1" : { "$avg" : "$Document.highranking_page_1"},
                "highranking_page_2" : { "$avg" : "$Document.highranking_page_2"},
                "highranking_page_3" : { "$avg" : "$Document.highranking_page_3"},
                "highranking_page_4" : { "$avg" : "$Document.highranking_page_4"},
                "time_spent_online" : { "$avg" : "$Document.time_spent_online"},
                "path_similarity_score" : { "$avg" : "$path_similarity_score"},
                "path_similarity_score_time_spent" : { "$avg" : "$path_similarity_score_time_spent"}
            
        }}]
    weekData = collection.aggregate(pipeline)     
    next_week_end = next_week + timedelta(days=7)
    next_week_purchases = collection.find({        
        "TypeId" :  userTypeId,
        "UserId" : appId,
        "Document.noticed_date" : { "$gte" : next_week, "$lt" : next_week_end  }
    }).distinct("Document.uuid")
    weekData = list(weekData)
    print "CrWeek Visits: {0} NxWeek Purchased users: {1}".format(len(weekData), len(next_week_purchases))
    for week_session in weekData:
        tmpDoc = week_session #["Document"] 
        uuid = tmpDoc["_id"]["uuid"]
        simscore = 0 if not "path_similarity_score" in tmpDoc else  tmpDoc["path_similarity_score"] 
        simtime = 0 if not "path_similarity_score_time_spent" in tmpDoc else  tmpDoc["path_similarity_score_time_spent"]
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
            simscore,
            simtime
        ])
        targetVar = 0
        if uuid in next_week_purchases:
            targetVar = 1
        targetData.append(targetVar)

print "Prepared " + str(len(inputData)) + " items"
conduct_experiment(inputData, targetData, 'Netinfo')

