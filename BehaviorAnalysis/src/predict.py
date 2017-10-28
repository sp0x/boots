import time
from constants import *
from classifiers import conduct_experiment, graph_experiment, plot_cutoff, Experiment
import urllib
import csv
from datetime import datetime, timedelta
import os
from buyers import check_prediction, check_prediction_ex
from utils import save, load, abs_path
import settings
from openpyxl import Workbook


db = settings.get_db()
collection = db.IntegratedDocument

userTypeId = "59cbc103003e730508e87c2c"
appId = "123123123"
#
company = "Netinfo"

weeksAvailable = collection.find({
    "UserId": appId,
    "TypeId": userTypeId
}).distinct("Document.g_timestamp")
weeksAvailable.sort() 
print weeksAvailable


target_week = weeksAvailable[6]  # last week has only 2 days, so use the one that's before it
target_week_end = target_week + timedelta(days=7)
print "Target week {0} will predict for {1}".format(target_week, target_week_end)

paying_users = collection.find({
    "UserId": appId,
    "TypeId": userTypeId,
    "Document.is_paying": 1
}).distinct("Document.uuid")


next_week = target_week_end
next_week_end = target_week_end + timedelta(days=7)
next_week_f = str(next_week).replace(':', '_')
filter_entries = True

print "Gathering data from week " + str(target_week)
prediction_features_query ={
    "TypeId": userTypeId,
    "UserId": appId,
    "Document.noticed_date": {'$gte': target_week, '$lt': target_week_end}
}
pipeline = [
        {"$match": prediction_features_query},
        {"$group": {
            "_id": {
                "uuid" : "$Document.uuid"
            },
            "visits_count": {"$sum": 1},
            "visits_on_weekends": {"$avg": "$Document.visits_on_weekends"},
            "p_online_weekend": {"$avg": "$Document.p_online_weekend"},
            "days_visited_ebag": {"$avg": "$Document.days_visited_ebag"},
            "time_spent_ebag": {"$avg" : "$Document.time_spent_ebag"},
            "time_spent_on_mobile_sites" : { "$avg" : "$Document.time_spent_on_mobile_sites"},
            "mobile_visits" : { "$avg" : "$Document.mobile_visits"},
            "visited_ebag" : { "$avg" : "$Document.visited_ebag"},
            "p_buy_age_group" : { "$avg" : "$Document.p_buy_age_group"},
            "p_buy_gender_group" : { "$avg" : "$Document.p_buy_gender_group"},
            "p_visit_ebag_age" : { "$avg" : "$Document.p_visit_ebag_age"},
            "p_visit_ebag_gender" : { "$avg" : "$Document.p_visit_ebag_gender"},
            "p_to_go_online" : { "$avg" : "$Document.p_to_go_online"},
            "avg_time_spent_on_high_pageranksites" : { "$avg" : "$Document.avg_time_spent_on_high_pageranksites"},
            "highranking_page_0" : {"$avg": "$Document.highranking_page_0"},
            "highranking_page_1" : {"$avg": "$Document.highranking_page_1"},
            "highranking_page_2" : {"$avg": "$Document.highranking_page_2"},
            "highranking_page_3" : {"$avg": "$Document.highranking_page_3"},
            "highranking_page_4" : {"$avg": "$Document.highranking_page_4"},
            "time_spent_online" : { "$avg": "$Document.time_spent_online"},
            "path_similarity_score" : { "$avg" : "$path_similarity_score"},
            "path_similarity_score_time_spent" : { "$avg" : "$path_similarity_score_time_spent"},
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
weekData = list(weekData) 

previously_purchasing_users = collection.find({
    "TypeId": userTypeId,
    "UserId": appId,
    "Document.noticed_date": {"$lt": target_week_end},
    "Document.is_paying": 1
}).distinct("Document.uuid")

# offset = 1000 * 200
exp = Experiment(None, None, None, company)

userFeatures = []
userData = []
targets = []
next_week_purchasing_users = users = collection.find({
    "TypeId": userTypeId,
    "UserId": appId,
    "Document.is_paying": 1,
    "Document.noticed_date": {'$gte': next_week, '$lte': next_week_end}
}).distinct("Document.uuid")

for tmpDoc in weekData:
    uuid = tmpDoc["_id"]["uuid"]
    if uuid in previously_purchasing_users:    # skip users that have paid some time
        continue
    userData.append({'uuid': uuid})

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

    # since we have only 1 week
    has_paid_before = 1 if uuid in previously_purchasing_users else 0

    input_element = [
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
        input_element.append(f_val)
    userFeatures.append(input_element)

    target_val = 0
    if "Document.is_paying" in prediction_features_query and prediction_features_query["Document.is_paying"] == 1:
        target_val = 1
    else:
        if uuid in next_week_purchasing_users and has_paid_before == 0:
            target_val = 1
    targets.append(target_val)


print "Loaded " + str(len(userFeatures)) + " test user items" 
payingDict = dict() 

filtered = 0 
models = exp.load_model_files(['rf'])
# models = [
#   Experiment.load_model("/experiments/Netinfo/model_gba_2017-10-04_12:44:31.pickle",company),
#   Experiment.load_model("/experiments/Netinfo/model_rf_2017-10-04_14:57:34.pickle", company)
#]

model_cutoffs = {
    'gba': 0.1,
    'rf': 0.2
}
print "Loaded prediction models"

for m in models:
    t_started = time.time()
    c_type = m['type']
    # x_train = Experiment.load_dump(company, 'x_train_{0}.dmp'.format(c_type))
    # y_train = Experiment.load_dump(company, 'y_train_{0}.dmp'.format(c_type))
    # m['model'].fit(x_train, y_train)
    predictions = m['model'].predict_proba(userFeatures)
    x_test = Experiment.load_dump(company, 'x_test_{0}.dmp'.format(c_type))
    y_true = Experiment.load_dump(company, 'y_true_{0}.dmp'.format(c_type))
    real_cutoff = plot_cutoff(m, x_test, y_true, client=company)
    graph_experiment(userFeatures, targets, company, m)
    try:
        Experiment.predict_explain_non_tree2(m, company, np.array(userFeatures), [
        "visits_on_weekends",
        "p_online_weekend",
        "days_visited_ebag",
        "time_spent_ebag",
        "time_spent_on_mobile_sites",
        "mobile_visits",
        "visited_ebag",
        "p_buy_age_group",
        "p_buy_gender_group",
        "p_visit_ebag_age",
        "p_visit_ebag_gender",
        "p_to_go_online",
        "avg_time_spent_on_high_pageranksites",
        "highranking_page_0",
        "highranking_page_1",
        "highranking_page_2",
        "highranking_page_3",
        "highranking_page_4",
        "time_spent_online",
        "simscore",
        "simtime",
        "non_paying_s_time",
        "non_paying_s_freq",
        "paying_s_time",
        "paying_s_freq",
        "has_paid_before",
        "has_type_val_0",
        "has_type_val_1",
        "has_type_val_2",
        "has_type_val_3",
        "has_type_val_4",
        "has_type_val_5",
        "has_type_val_6",
        "has_type_val_7",
        "has_type_val_8",
        "has_type_val_9",
        "time_between_visits_avg"
        ], next_week_f)
    except:
        pass
    fileName = os.path.join(company, 'prediction_{0}_{1}.xlsx'.format(c_type, next_week_f))

    print "Writing predictions in: " + fileName
    report = Workbook()
    main_sheet = report.active
    info_sheet = report.create_sheet("Output")
    validations_sheet = report.create_sheet("Validated")

    main_sheet.append(["uuid", "chance to by"])
    for p in xrange(len(predictions)):
        prediction = predictions[p]
        uuid = userData[p]['uuid']
        pval = predictions[1]
        print uuid
        print pval
        main_sheet.append([uuid, pval])
    time_taken = time.time() - t_started
    info_sheet.append([
        "{0} Duration of prediction: {1} ({2}u/s)".format(c_type, time_taken, time_taken / len(predictions)),

    ])
    
    cutoff_value = model_cutoffs[c_type]
    check = check_prediction_ex(next_week, {
        'users' : userData, 'predictions': predictions
    }, cutoff_value, company, c_type)
    for match in check['matches']:
        uuid = match['uuid']
        chance = float(match['chance'])
        validations_sheet.append([uuid, chance, chance >= cutoff_value])

    #rf Matches 90.1961%: 51, pos46 c0.2(0.6974)rf False positives: 14.0023%, count 3084 of 22025 with (0.0685-0.3187)% chance
    # print "Users paid in {0}, but not in the previous week: {1} ({2} filtered)".format(next_week, len(check['payers']), check['filtered'])
    str_matches = "Matches {0:0.4f}% ({1} of {2})".format(check['positive_perc'], len(check['matches']), check['positive_matches'])
    str_cutoff = "Cutoff {0:0.4f}(highest {1:0.4f})".format(cutoff_value, real_cutoff)
    str_fp = "FP {0:0.4f}% ({1} of {2} / med {3:0.4f} - {4:0.4f})".format(
        check['false_positives']['perc'],
        check['false_positives']['count'],
        check['false_positives']['out_of'],
        check['false_positives']['chance']['median'],
        check['false_positives']['chance']['high'])
    info_sheet.append([str_matches])
    info_sheet.append([str_cutoff])
    info_sheet.append([str_fp])
    info_sheet.append(["Precision: {0:0.4f}".format(check['precision'])])
    info_sheet.append(["Recall: {0:0.4f}".format(check['recall'])])
    report.save(fileName)
    print "{0}\n{1}\n{2}\n".format(str_matches, str_cutoff, str_fp)
    nowVal = datetime.now().strftime("%Y-%m-%d_%H_%M_%S")
    output_file = open("final_result_{0}_{1}_{2}.txt".format(c_type, next_week_f, nowVal), "w")
    output_file.write(outputVal)
    output_file.close()
