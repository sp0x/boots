import time
from constants import *
from pymongo import MongoClient
import urllib
import csv
from datetime import datetime, timedelta
import os
import csv
import timestring
from utils import save,load,abs_path, latest_file

##host is local
password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
host = "10.10.1.5"
filterFile = "Netinfo/filters/paying_users.csv"

client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
db = client.netvoid
collection = db.IntegratedDocument

userTypeId = "59cbc103003e730508e87c2c"
appId = "123123123"

# weeksAvailable = collection.find({
#     "UserId": appId,
#     "TypeId": userTypeId
# }).distinct("Document.g_timestamp")
# weeksAvailable.sort()

# def get_targets(data, prediction_week_start):
#     target_week = prediction_week_start
#     target_week_end = target_week + timedelta(days=7)
#
#     previously_purchasing_users = collection.find({
#         "TypeId": userTypeId,
#         "UserId": appId,
#         "Document.noticed_date": {"$lt": target_week},
#         "Document.is_paying": 1
#     }).distinct("Document.uuid")
#
#     users = collection.find({
#         "TypeId": userTypeId,
#         "UserId": appId,
#         "Document.is_paying": 1,
#         "Document.noticed_date": {'$gte': target_week, '$lte': target_week_end}
#     }).distinct("Document.uuid")



def check_prediction_ex(validate_with, prediction_week_start, cutoff=0.5, company="Netinfo", model="rf"):
    target_week = prediction_week_start
    target_week_end = target_week + timedelta(days=7)
    prev_week = target_week - timedelta(days=7)
    prev_week_end = target_week

    users_purchased = dict()

    if validate_with == None:
        users = collection.find({
            "TypeId": userTypeId,
            "Document.is_paying": 1,
            "Document.noticed_date": {'$gte': target_week, '$lte': target_week_end}
        }).distinct("Document.uuid")
        for user in users:
            users_purchased[user] = True
    else:
        print "Validating with: " + validate_with
        with open(validate_with, 'rb') as paying_csv:
            paying_users_reader = csv.reader(paying_csv, delimiter=',', quotechar='|')
            irow = 0
            for row in paying_users_reader:
                if irow == 0:
                    irow += 1
                    continue
                if len(row) == 0:
                    continue
                try:
                    uuid = row[0].replace('"', '')
                    dt = row[1].replace('"', '')
                    ondate = timestring.Date(dt)
                    is_current = ondate > target_week and ondate < target_week_end
                    if uuid == 'uuid' or not is_current:
                        continue
                    users_purchased[uuid] = True
                except:
                    pass

    predictions_file = latest_file(os.path.join(company, "prediction_{0}_".format(model)))
    prediction_matches = []
    positive_matches = 0
    cnt_negatives = 0
    cnt_predictions = 0
    print "Validating {0}".format(predictions_file)
    with open(predictions_file, 'rb') as prediction_csv:
        predicted_reader = csv.reader(prediction_csv, delimiter=',', quotechar='|')
        row_c = 0
        for row in predicted_reader:
            if row_c == 0:
                row_c += 1
                continue
            cnt_predictions += 1
            uuid = row[0].replace('"', '')
            perc_will_buy = float(row[1])
            if uuid in users_purchased:
                match = {'uuid': uuid, 'chance': perc_will_buy}
                if perc_will_buy > cutoff:
                    positive_matches += 1
                    match['positive'] = True
                prediction_matches.append(match)
            else:
                if perc_will_buy > cutoff:
                    cnt_negatives += 1
    positive_perc = (float(positive_matches)/float(len(prediction_matches))) * 100
    return {
        'false_positives': {
            'perc': (float(cnt_negatives) / float(cnt_predictions)) * 100,
            'count': cnt_negatives,
            'out_of': cnt_predictions
        },
        'positive_perc': positive_perc,
        'target_week': target_week,
        'payers': users_purchased,
        'filtered': 0,
        'matches': prediction_matches,
        'positive_matches': positive_matches
    }


def check_prediction(prediction_week_start, cutoff=0.5, company="Netinfo", model="rf"):
    target_week = prediction_week_start
    target_week_end = target_week + timedelta(days=7)
    prev_week = target_week - timedelta(days=7)
    prev_week_end = target_week

    non_paying_in_previous_week = collection.find({
        "UserId": appId,
        "TypeId": userTypeId,
        "Document.is_paying": 1,
        "Document.g_timestamp": {'$gte': prev_week, '$lt': prev_week_end}
    }).distinct("Document.uuid")

    payingDict = dict()
    with open(filterFile, 'rb') as filterCsv:
        filterReader = csv.reader(filterCsv, delimiter=';', quotechar='|')
        for row in filterReader:
            uuid = row[0].replace('"', '')
            payingDict[uuid] = True

    users_purchased = []
    cnt_filtered = 0
    for user in non_paying_in_previous_week:
        if user in paying_users:
            if user in payingDict:  # skip filtered users
                cnt_filtered = cnt_filtered + 1
                continue
            print user
            users_purchased.append(user)

    predictions_file = os.path.join(company, "prediction_{0}.csv".format(model))
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
            perc_will_buy = float(row[1])
            if uuid in users_purchased:
                prediction_matches += 1
                if perc_will_buy > cutoff:
                    positive_matches += 1
    return {
        'payers': users_purchased,
        'filtered': cnt_filtered,
        'matches': prediction_matches,
        'positive_matches': positive_matches
    }


if __name__ == "__main__":
    weeksAvailable = collection.find({
         "UserId": appId,
         "TypeId": userTypeId
    }).distinct("Document.g_timestamp")
    weeksAvailable.sort()
    target_week = weeksAvailable[len(weeksAvailable) - 2]  # verify week 5 results
    next_week = target_week + timedelta(days=7)
    next_week_f = str(next_week).replace(':', '_')

    cutoff = 0.11
    company = "Netinfo"
    c_types = ['gba']

    for model_name in c_types:
        validation_file = abs_path(os.path.join(company, "validation", "validation.csv"))
        check = check_prediction_ex(validation_file, next_week, cutoff, company, model_name)
        matches_filename = abs_path(os.path.join(company, "validation_matches_{0}.csv".format(next_week_f)))
        with open(matches_filename, 'wb') as validation_file:
            validation_writer = csv.writer(validation_file, delimiter=',',  quotechar='|', quoting=csv.QUOTE_MINIMAL)
            validation_writer.writerow(["uuid", "chance", "valid (cutoff " + str(cutoff) + ")"])
            for match in check['matches']:
                uuid = match['uuid']
                chance = float(match['chance'])
                validation_writer.writerow([uuid, chance, chance >= cutoff])

        # print "Users paid in {0}, but not in the previous week: {1} ({2} filtered)".format(next_week, len(check['payers']), check['filtered'])
        print model_name + " Matches: {0}, positive {1}".format(len(check['matches']), check['positive_matches'])
        print model_name + " False positives: {0}%, count {1}".format(check['false_positives']['perc'], check['false_positives']['count'])


