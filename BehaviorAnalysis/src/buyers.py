import time
from constants import *
from pymongo import MongoClient
import urllib
import csv
from datetime import datetime, timedelta
import os
import csv

##host is local
password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
host = "10.10.1.5"
filterFile = "Netinfo/filters/paying_users.csv"

client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
db = client.netvoid
collection = db.IntegratedDocument

userTypeId = "598da0a2bff3d758b4025d21"
appId = "123123123"
#
company = "Netinfo"

weeksAvailable = collection.find({
    "UserId": appId,
    "TypeId": userTypeId
}).distinct("Document.g_timestamp")
weeksAvailable.sort()


target_week = weeksAvailable[4]
target_week_end = target_week + timedelta(days=7)
prev_week = target_week - timedelta(days=7)
prev_week_end = target_week

non_paying_in_previous_week = collection.find({
    "UserId": appId,
    "TypeId": userTypeId,
    "Document.is_paying": 1,
    "Document.g_timestamp": {'$gte': prev_week, '$lt': prev_week_end}
}).distinct("Document.uuid")

paying_users = collection.find({
    "UserId": appId,
    "TypeId": userTypeId,
    "Document.is_paying": 1,
    "Document.g_timestamp": {'$gte': target_week, '$lt': target_week_end}
}).distinct("Document.uuid")

payingDict = dict()
with open(filterFile, 'rb') as filterCsv:
    filterReader = csv.reader(filterCsv, delimiter=';', quotechar='|')
    for row in filterReader:
        uuid = row[0].replace('"', '')
        payingDict[uuid] = True


users_purchased = []
filtered = 0
for user in non_paying_in_previous_week:
    if user in paying_users:
        if user in payingDict:  # skip filtered users
            filtered = filtered + 1
            continue
        print user
        users_purchased.append(user)

predictions_file = "Netinfo/prediction_rf.csv"
payingDict = dict()
prediction_matches = 0
positive_matches = 0
with open(predictions_file, 'rb') as prediction_csv:
    predicted_reader = csv.reader(prediction_csv, delimiter=',', quotechar='|')
    row_c = 0
    for row in predicted_reader:
        if row_c == 0:
            row_c += 1
            continue
        row_c += 1
        uuid = row[0].replace('"', '')
        perc_will_not_buy = float(row[1])
        perc_will_buy = float(row[2])
        if uuid in users_purchased:
            prediction_matches += 1
            if perc_will_buy > 0.5:
                positive_matches += 1

print "Users paid in {0}, but not in the previous week: {1} ({2} filtered)".format(target_week, len(users_purchased), filtered)
print "Matches: {0}, positive {1}".format(prediction_matches, positive_matches)

