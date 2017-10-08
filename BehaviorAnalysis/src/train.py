import time
from constants import *
from classifiers import conduct_experiment, create_balancer, Experiment
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
# userTypeId = "598da0a2bff3d758b4025d21"
userTypeId = "59cbc103003e730508e87c2c"
appId = "123123123"

weeksAvailable = collection.find({
    "UserId": appId,
    "TypeId": userTypeId
}).distinct("Document.g_timestamp")
weeksAvailable.sort()
print weeksAvailable
#our test week
weekLimit = 0
# weeksAvailable = weeksAvailable[(-1) * weekLimit:] if weekLimit > 0 else weeksAvailable  # last n weeks
weeksAvailable = [weeksAvailable[5], weeksAvailable[6], weeksAvailable[7], weeksAvailable[8], weeksAvailable[9]]
targetData = []
inputData = []
# limit = 2 * 100 * 1000
# userlimit = 15 * 1000

for index, week in enumerate(weeksAvailable):
    next_week = week + timedelta(days=7)
    print "Preparing week: " + str(week) + "    " + str(index+1) + "/" + str(len(weeksAvailable))
    pipeline = [
        {"$match": {
            "TypeId": userTypeId,
            "UserId": appId,
            "Document.g_timestamp": {'$gte': week, '$lt': next_week}
         }
        },
        {"$group": {
            "_id": {
                "uuid": "$Document.uuid",
                "week_start": "$Document.g_timestamp"
            }, 
            "visits_count": {"$sum": 1}, 
            "visits_on_weekends": {"$avg": "$Document.visits_on_weekends"},
            "p_online_weekend": {"$avg": "$Document.p_online_weekend"},
            "days_visited_ebag": {"$avg": "$Document.days_visited_ebag"},
            "time_spent_ebag": {"$avg": "$Document.time_spent_ebag"},
            "time_spent_on_mobile_sites": {"$avg": "$Document.time_spent_on_mobile_sites"},
            "mobile_visits": {"$avg": "$Document.mobile_visits"},
            "visited_ebag": {"$avg": "$Document.visited_ebag"},
            "p_buy_age_group": {"$avg": "$Document.p_buy_age_group"},
            "p_buy_gender_group": {"$avg": "$Document.p_buy_gender_group"},
            "p_visit_ebag_age": {"$avg": "$Document.p_visit_ebag_age"},
            "p_visit_ebag_gender": {"$avg": "$Document.p_visit_ebag_gender"},
            "p_to_go_online": {"$avg": "$Document.p_to_go_online"},
            "avg_time_spent_on_high_pageranksites": {"$avg": "$Document.avg_time_spent_on_high_pageranksites"},
            "highranking_page_0": {"$avg": "$Document.highranking_page_0"},
            "highranking_page_1": {"$avg": "$Document.highranking_page_1"},
            "highranking_page_2": {"$avg": "$Document.highranking_page_2"},
            "highranking_page_3": {"$avg": "$Document.highranking_page_3"},
            "highranking_page_4": {"$avg": "$Document.highranking_page_4"},
            "time_spent_online": {"$avg": "$Document.time_spent_online"},
            "path_similarity_score": {"$avg": "$path_similarity_score"},
            "path_similarity_score_time_spent": {"$avg": "$path_similarity_score_time_spent"},
            "non_paying_s_time": {"$avg": "$Document.non_paying_s_time"},
            "non_paying_s_freq": {"$avg": "$Document.non_paying_s_freq"},
            "paying_s_time": {"$avg": "$Document.paying_s_time"},
            "paying_s_freq": {"$avg": "$Document.paying_s_freq"},
            "has_type_val_0": {"$avg": "$Document.has_type_val_0"},
            "has_type_val_1": {"$avg": "$Document.has_type_val_1"},
            "has_type_val_2": {"$avg": "$Document.has_type_val_2"},
            "has_type_val_3": {"$avg": "$Document.has_type_val_3"},
            "has_type_val_4": {"$avg": "$Document.has_type_val_4"},
            "has_type_val_5": {"$avg": "$Document.has_type_val_5"},
            "has_type_val_6": {"$avg": "$Document.has_type_val_6"},
            "has_type_val_7": {"$avg": "$Document.has_type_val_7"},
            "has_type_val_8": {"$avg": "$Document.has_type_val_8"},
            "has_type_val_9": {"$avg": "$Document.has_type_val_9"},
            "time_between_visits_avg": {"$avg": "$Document.time_between_visits_avg"}
        }}]
    weekData = collection.aggregate(pipeline, allowDiskUse=True)
    next_week_end = next_week + timedelta(days=7)
    next_week_purchases = collection.find({        
        "TypeId": userTypeId,
        "UserId": appId,
        "Document.noticed_date": {"$gte": next_week, "$lt": next_week_end},
        "Document.is_paying": 1
    }).distinct("Document.uuid")
    weekData = list(weekData)
    users_paid = []
    print "CrWeek Visits: {0} NxWeek Purchased users: {1}".format(len(weekData), len(next_week_purchases))
    for week_session in weekData:
        tmpDoc = week_session
        uuid = tmpDoc["_id"]["uuid"]
        simscore = 0 if "path_similarity_score" not in tmpDoc else tmpDoc["path_similarity_score"]
        simscore = 0 if simscore == None else simscore
        simtime = 0 if "path_similarity_score_time_spent" not in tmpDoc else tmpDoc["path_similarity_score_time_spent"]
        simtime = 0 if simtime == None else simtime
        non_paying_s_time = 0 if "non_paying_s_time" not in tmpDoc else tmpDoc["non_paying_s_time"]
        non_paying_s_time = 0 if non_paying_s_time is None else non_paying_s_time

        non_paying_s_freq = 0 if "non_paying_s_freq" not in tmpDoc else tmpDoc["non_paying_s_freq"]
        non_paying_s_freq = 0 if non_paying_s_freq is None else non_paying_s_freq

        paying_s_time = 0 if "paying_s_time" not in tmpDoc else tmpDoc["paying_s_time"]
        paying_s_time = 0 if paying_s_time is None else paying_s_time

        paying_s_freq = 0 if "paying_s_freq" not in tmpDoc else tmpDoc["paying_s_freq"]
        paying_s_freq = 0 if paying_s_freq is None else paying_s_freq

        time_between_visits_avg = 0 if "time_between_visits_avg" not in tmpDoc else tmpDoc["time_between_visits_avg"]
        time_between_visits_avg = 0 if time_between_visits_avg is None else time_between_visits_avg

        has_paid_before = 1 if uuid in users_paid else 0
        inputElement = [
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
            simtime,
            non_paying_s_time,
            non_paying_s_freq,
            paying_s_time,
            paying_s_freq,
            has_paid_before,
            time_between_visits_avg
        ]
        for i in xrange(10): 
            f_name = "has_type_val_{0}".format(i)
            f_val = 0 if f_name not in tmpDoc else tmpDoc[f_name]
            f_val = 0 if f_val is None else f_val
            inputElement.append(f_val)

        inputData.append(inputElement)
        targetVar = 0
        if uuid in next_week_purchases and not has_paid_before:
            targetVar = 1
            users_paid.append(uuid)                                 #If user has ever purchased, use 0 for following targets from him
        targetData.append(targetVar)

print "Prepared " + str(len(inputData)) + " items"
conduct_experiment(inputData, targetData, 'Netinfo')