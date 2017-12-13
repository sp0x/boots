import time
from constants import *
import urllib
import csv
from datetime import datetime, timedelta
import os
import csv
import timestring
from utils import save,load,abs_path, latest_file
import numpy as np
import sys
import settings

##host is local
filterFile = "Netinfo/filters/paying_users.csv"

db = settings.get_db()
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



def check_prediction_ex(prediction_week_start, users_and_predictions, cutoff=0.5, company="Netinfo", model="rf"):
    target_week = prediction_week_start
    target_week_end = target_week + timedelta(days=7)
    prev_week = target_week - timedelta(days=7)
    prev_week_end = target_week
    users_data = users_and_predictions['users']
    predictions_data = users_and_predictions['predictions']
     
    print "Checking for sales between {0} - {1}".format(target_week, target_week_end)
    users_purchased = collection.find({
            "TypeId": userTypeId,
            "UserId": appId,
            "Document.is_paying": 1,
            "Document.noticed_date": {'$gte': target_week, '$lte': target_week_end}
        }).distinct("Document.uuid")

    # predictions_file = latest_file(os.path.join(company, "prediction_{0}_".format(model)))
    prediction_matches = []
    negative_chances = []
    negative_changes_overall = []
    positive_matches = 0 
    cnt_predictions = 0
    cnt_above_cutoff = 0
    print "Validating predictions"
    false_negatives = 0

    for p in xrange(len(predictions_data)):
        prediction = predictions_data[p]
        uuid = users_data[p]['uuid']
        prediction = predictions_data[p]
        cnt_predictions += 1
        perc_will_buy = float(prediction[1])
        if perc_will_buy >= cutoff:
            cnt_above_cutoff += 1
        # if he really did make a purchase
        if uuid in users_purchased:
            match = {'uuid': uuid, 'chance': perc_will_buy}
            if perc_will_buy >= cutoff:
                positive_matches += 1
                match['positive'] = True
            else:
                #user made a purchase, but we predicted that he won`t
                false_negatives += 1
            prediction_matches.append(match)
        else:
            # check if our cutoff includes him
            negative_changes_overall.append(perc_will_buy)  
            if perc_will_buy >= cutoff: 
                negative_chances.append(perc_will_buy) 
                    


    positive_perc = (float(positive_matches)/float(max(1, len(prediction_matches)))) * 100
    precision = float(positive_matches) / (positive_matches + len(negative_chances))
    recall = float(positive_matches) / (positive_matches + false_negatives)
    return {
        'precision' : precision,
        'recall' : recall,
        'false_positives': {
            'perc': (float(len(negative_chances)) / float(max(1,cnt_predictions))) * 100,
            'count': len(negative_chances),
            'out_of': cnt_predictions,
            'chance': {
                'median' : np.median(negative_changes_overall),
                'high' : np.median(negative_chances)
            }
        },
        'positive_perc': positive_perc,
        'target_week': target_week,
        'payers': users_purchased,
        'filtered': 0,
        'matches': prediction_matches,
        'positive_matches': positive_matches,
        'above_cutoff' : cnt_above_cutoff
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
    false_negatives = 0
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
                else:
                    false_negatives += 1
    return {
        'payers': users_purchased,
        'filtered': cnt_filtered,
        'matches': prediction_matches,
        'positive_matches': positive_matches
    }


if __name__ == "__main__":
    company = "Netinfo"

    weeksAvailable = collection.find({
         "UserId": appId,
         "TypeId": userTypeId
    }).distinct("Document.g_timestamp")
    weeksAvailable.sort()
    target_week = weeksAvailable[9]  # verify week 5 results
    next_week = target_week + timedelta(days=7)
    print "Validating {0}".format(next_week)
    prediction_file = os.path.join(company, "prediction_rf_{0}.csv".format(next_week).replace(":", "_"))
    print "Checking file {0} for old buyers".format(prediction_file)
    previous_buyers = []
    with open(prediction_file, 'rb') as checkCsv:
        prediction_reader = csv.reader(checkCsv, delimiter=',', quotechar='|')
        for row in prediction_reader:
            uuid = row[0].replace('"', '')
            cnt_in_mongo = collection.find({
                "UserId": appId,
                "TypeId": userTypeId,
                "Document.uuid": uuid,
                "Document.noticed_date": { '$gte' : next_week + timedelta(days=7) },
                "Document.is_paying": 1
            }).count()
            if cnt_in_mongo > 0:
                previous_buyers.append(uuid)
    if len(previous_buyers) > 0:
        print "Found {0} users that should not be in the prediction!".format(len(previous_buyers))
    else:
        print "Prediction file is clean from old buyers"

    next_week_f = str(next_week).replace(':', '_')

    cutoff = 0.7115
    c_types = ['rf']
    model_cutoffs = {
        'gba': 0.1,
        'rf': 0.5
    }

    for model_name in c_types:
        cutoff_value = model_cutoffs[model_name]
        validation_file = abs_path(os.path.join(company, "validation", "validation.csv"))
        check = check_prediction_ex(validation_file, next_week, cutoff_value, company, model_name)
        matches_filename = abs_path(os.path.join(company, "validation_matches_{0}.csv".format(next_week_f)))
        # with open(matches_filename, 'wb') as validation_file:
        #     validation_writer = csv.writer(validation_file, delimiter=',',  quotechar='|', quoting=csv.QUOTE_MINIMAL)
        #     validation_writer.writerow(["uuid", "chance", "valid (cutoff " + str(cutoff) + ")"])
        #     for match in check['matches']:
        #         uuid = match['uuid']
        #         chance = float(match['chance'])
        #         validation_writer.writerow([uuid, chance, chance >= cutoff])

        # print "Users paid in {0}, but not in the previous week: {1} ({2} filtered)".format(next_week, len(check['payers']), check['filtered'])
        print model_name + " Matches {0:0.4f}%: {1}, pos{2} c{3}({4:0.4f}) - {5} total".format(check['positive_perc'], 
        len(check['matches']), check['positive_matches'], cutoff_value, cutoff, check['above_cutoff'])
        print model_name + " False positives: {0:0.4f}%, count {1} of {2} with ({3:0.4f}-{4:0.4f})% chance".format(check['false_positives']['perc'],
                                                                                                           check['false_positives']['count'],
                                                                                                           check['false_positives']['out_of'],
                                                                                                           check['false_positives']['chance']['median'],
                                                                                                           check['false_positives']['chance']['high'])



