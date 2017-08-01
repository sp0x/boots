 import time
 from constants import *
 from classifiers import conduct_experiment,Experiment
 from pymongo import MongoClient
 import urllib
 
 ##host is local
 password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
host = "10.10.1.5"

client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
 db = client.netvoid;
 userDaysCollection = db.IntegratedDocument;
 
 allData = userDaysCollection.find({
     "UserId" : "123123123"
 }, {
     "Document.day" : 1,
     "Document.date" : 1,
     "Document.max_time_spent_by_any_paying_user_ebag" : 1,
     "Document.time_of_day" : 1,
     "Document.is_holiday" : 1,
     "Document.is_weekend" : 1,
     "Document.is_before_weekend" : 1,
     "Document.is_before_holiday" : 1,
     "Document.prob_buy_is_holiday" : 1,
     "Document.prob_buy_is_before_holiday" : 1,
     "Document.prop_buy_is_weekend" : 1,
     "Document.visits_per_time" : 1,
     "Document.bought_last_week" : 1,
     "Document.bought_last_month" : 1,
     "Document.bought_last_year" : 1,
     "Document.time_spent" : 1,
     "Document.time_spent_max" : 1,
     "Document.month" : 1,
     "Document.prob_buy_is_holiday_user" : 1,
     "Document.prob_buy_is_before_holiday_user" : 1,
     "Document.prop_buy_is_weekend_user" : 1,
     "Document.is_from_mobile" : 1,
     "Document.is_on_promotions_page" : 1,
     "Document.before_visit_from_mobile" : 1,
     "Document.time_before_leaving" : 1,
     "Document.page_rank" : 1,
     "Document.is_paying" : 1
 } ).limit(1000 * 100)
 targets = dict()
 data = dict()
 p=0
 for doc in allData: 
     targets[p] = 1 if doc['Document']['is_paying']==1 else 0
     data[p] = doc['Document']
     p += 1
print "Prepared " + str(allData.count()) + " items"
 
 conduct_experiment(data, targets, 'Netinfo'); 
