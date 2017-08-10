import time
from constants import *
from classifiers import conduct_experiment,Experiment
from pymongo import MongoClient
import urllib
 
##host is local
password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
host = "10.10.1.5"

client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
db = client.netvoid
userDaysCollection = db.IntegratedDocument

allData = userDaysCollection.find({
    "UserId" : "123123123"
}, {
     'Document.uuid' : 1,
    'Document.noticed_date' : 1,
    'Document.is_paying' : 1,
    'Document.max_time_spent_by_any_paying_user_ebag' : 1,
    'Document.prob_buy_is_holiday' : 1,
    'Document.prob_buy_is_before_holiday' : 1,
    'Document.prop_buy_is_weekend' : 1,
    'Document.visits_per_time' : 1,
    'Document.bought_last_week' : 1,
    'Document.bought_last_month' : 1,
    'Document.bought_last_year' : 1,
    'Document.time_spent' : 1,
    'Document.time_spent_max' : 1,
    'Document.month' : 1,
    'Document.prob_buy_is_holiday_user' : 1,
    'Document.prob_buy_is_before_holiday_user' : 1,
    'Document.prop_buy_is_weekend_user' : 1,
    'Document.is_from_mobile' : 1,
    'Document.is_on_promotions_page' : 1,
    'Document.before_visit_from_mobile' : 1,
    'Document.time_before_leaving' : 1,
    'Document.page_rank' : 1,
    'Document.prop_buy_is_before_weekend_user' : 1,
    'Document.visits_before_weekend' : 1,
    'Document.visits_before_holidays' : 1,
    'Document.visits_on_holidays' : 1,
    'Document.visits_on_weekends' : 1,
    'Document.days_visited_ebag' : 1,
    'Document.mobile_visits' : 1,
    'Document.mobile_purchases' : 1
} ).limit(1000 * 200)
targets = []
data = []
p=0

for doc in allData:  
    targets.append(1 if doc['Document']['is_paying']==1 else 0)
    tmpDoc = doc['Document']
    data.append([
        tmpDoc['max_time_spent_by_any_paying_user_ebag'],
        tmpDoc['prob_buy_is_holiday'],
        tmpDoc['prob_buy_is_before_holiday'],
        tmpDoc['prop_buy_is_weekend'],
        tmpDoc['visits_per_time'],
        tmpDoc['bought_last_week'],
        tmpDoc['bought_last_month'],
        tmpDoc['bought_last_year'],
        tmpDoc['time_spent'],
        tmpDoc['time_spent_max'],
        tmpDoc['month'],
        tmpDoc['prob_buy_is_holiday_user'],
        tmpDoc['prob_buy_is_before_holiday_user'],
        tmpDoc['prop_buy_is_weekend_user'],
        tmpDoc['is_from_mobile'],
        tmpDoc['is_on_promotions_page'],
        tmpDoc['before_visit_from_mobile'],
        tmpDoc['time_before_leaving'],
        tmpDoc['page_rank'],
        tmpDoc['prop_buy_is_before_weekend_user'],
        tmpDoc['visits_before_weekend'],
        tmpDoc['visits_before_holidays'],
        tmpDoc['visits_on_holidays'],
        tmpDoc['visits_on_weekends'],
        tmpDoc['days_visited_ebag'],
        tmpDoc['mobile_visits'],
        tmpDoc['mobile_purchases']])
    p += 1
allData = None

print "Prepared " + str(len(data)) + " items"
conduct_experiment(data, targets, 'Netinfo')
