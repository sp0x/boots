import time
from constants import *
from classifiers import conduct_experiment,Experiment
from pymongo import MongoClient

##host is local

client = MongoClient('mongodb://vasko:Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763@mongo.peeralize.com:27017/netvoid?authSource=admin')
db = client.netvoid;
userDaysCollection = db.IntegratedDocument;

data = userDaysCollection.find({}, {
    "day" : 1,
    "date" : 1,
    "max_time_spent_by_any_paying_user_ebag" : 1,
    "time_of_day" : 1,
    "is_holiday" : 1,
    "is_weekend" : 1,
    "is_before_weekend" : 1,
    "is_before_holiday" : 1,
    "prob_buy_is_holiday" : 1,
    "prob_buy_is_before_holiday" : 1,
    "prop_buy_is_weekend" : 1,
    "visits_per_time" : 1,
    "bought_last_week" : 1,
    "bought_last_month" : 1,
    "bought_last_year" : 1,
    "time_spent" : 1,
    "time_spent_max" : 1,
    "month" : 1,
    "prob_buy_is_holiday_user" : 1,
    "prob_buy_is_before_holiday_user" : 1,
    "prop_buy_is_weekend_user" : 1,
    "is_from_mobile" : 1,
    "is_on_promotions_page" : 1,
    "before_visit_from_mobile" : 1,
    "time_before_leaving" : 1,
    "page_rank" : 1,
    "is_paying" : 1
} ).limit(1000 * 100)
targets = dict()
p=0
for doc in data:
    targets[p] = 1 if doc.is_paying else 0
    p += 1


conduct_experiment(data, targets, 'Netinfo'); 