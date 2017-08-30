import time
from constants import *
from classifiers import conduct_experiment, plot_cutoff, Experiment
from pymongo import MongoClient
import urllib
import csv
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
target_week = weeksAvailable[len(weeksAvailable) - 2]
target_week_end = target_week + timedelta(days=7)
print "Target week " + str(target_week)
pipeline = [
        {"$match" : { 
            "TypeId" : userTypeId,
            "UserId" : appId,
            "Document.g_timestamp" : {'$gte': target_week, '$lt': target_week_end}
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
weekData = list(weekData) 

company = "Netinfo"
offset = 1000 * 200
exp = Experiment(None, None, None, company)
models = exp.load_models()
print "Loaded prediction models"

userFeatures = []
userData = []
for tmpDoc in weekData:
    uuid = tmpDoc["_id"]["uuid"]
    
    userData.append({ 'uuid' : uuid })
    simscore = 0 if not "path_similarity_score" in tmpDoc else  tmpDoc["path_similarity_score"] 
    simscore = 0 if simscore == None else simscore
    simtime = 0 if not "path_similarity_score_time_spent" in tmpDoc else  tmpDoc["path_similarity_score_time_spent"]
    simtime = 0 if simtime == None else simtime        
    userFeatures.append([
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

# print "Loaded " + str(len(userFeatures)) + " test user items"
# filterFile = "NetInfo/filters/paying_users.csv"
# payingDict = dict()
# with open(filterFile, 'rb') as filterCsv:
#     filterReader = csv.reader(filterCsv, delimiter=';', quotechar='|')
#     for row in filterReader:
#         uuid = row[0].replace('"', '')
#         payingDict[uuid] = True

filtered = 0
for m in models:
    predictions = m['model'].predict_proba(userFeatures)
    plot_cutoff(m, userFeatures, None, client=company)
    fileName = company + '_prediction_' + m['type'] + ".csv"

    print "Writing predictions in: " + fileName
    with open(fileName, 'wb') as csvfile:
        writer = csv.writer(csvfile, delimiter=',',  quotechar='|', quoting=csv.QUOTE_MINIMAL)
        writer.writerow([ "uuid", "%will not purchase", "%will purchase"])
        for p in xrange(len(predictions)): 
            prediction = predictions[p] 
            uuid = userData[p]['uuid']       
            #if uuid in payingDict: #skip filtered users
            #    filtered = filtered + 1
            #   continue
            # else:
            writer.writerow([ uuid, prediction[0], prediction[1] ])
print "Filtered users: " + str(filtered)
