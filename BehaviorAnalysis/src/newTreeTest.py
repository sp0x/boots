import time
from features import BNode, BTree, lin
from pymongo import MongoClient
import urllib
from utils import parse_timespan, chunks
from time import sleep
import sys
from pymongo import UpdateMany
from trees import MassTreeBuilder
from datetime import datetime, timedelta

 
#type used for web sessions
userSessionTypeId = "598f20d002d2516dd0dbcee2"
userTypeId = "598da0a2bff3d758b4025d21"

week4Start = datetime(2017, 7, 17, 0, 0, 0)
appId = "123123123"
password = urllib.quote_plus('Y8Iwb6lI4gRdA+tbsaBtVj0sIRVuUedCOJfNyD4hymuRqG4WVNlY9BfQzZixm763')
host = "10.10.1.5"

client = MongoClient('mongodb://vasko:' + password + '@' + host + ':27017/netvoid?authSource=admin')
db = client.netvoid
documents_col = db.IntegratedDocument


def create_sessions_tree(query, filter_uuids=None):
    tr = BTree()
    query["TypeId"] = userSessionTypeId
    cursor = documents_col.find(query)
    for day_user_session in cursor:
        cr_items = []
        cr_uuid = day_user_session["Document"]["UserId"]
        if filter_uuids is not None and cr_uuid in filter_uuids:
            continue
        for session_item in day_user_session["Document"]["Sessions"]:
            domain = session_item["Domain"]
            duration = parse_timespan(session_item["Duration"])
            duration = duration.total_seconds()
            cr_items.append({'time': duration, 'label': domain})
        tr.build(cr_items)
    return tr

weeksAvailable = documents_col.find({
    "UserId": appId,
    "TypeId": userTypeId
}).distinct("Document.g_timestamp")
weeksAvailable.sort()
target_week = weeksAvailable[0]
target_week_end = target_week + timedelta(days=7)
target_next_week = weeksAvailable[1]
target_next_week_end = target_next_week + timedelta(days=7)


print "Target weeks: {0} to {1} and".format(str(target_week), str(target_week_end))
print "{0} - {1}".format(str(target_next_week), str(target_next_week_end))

# IDs of the users who bought in the target week, so we can filter them
target_week_buyers = documents_col.find({
    "TypeId": userSessionTypeId,
    "Document.is_paying": 1,
    "Document.Created": {"$lte": target_week_end, "$gte": target_week}
}).distinct("Document.uuid")

tree_users_not_purchased_in_tweek = create_sessions_tree({
    "Document.is_paying": 0,
    'Document.Created': {"$lte": target_week_end, "$gte": target_week}
})
tree_users_purchased_after_tweek = create_sessions_tree({
    "Document.is_paying": 1,
    'Document.Created': {"$lte": target_next_week_end, "$gte": target_week}
}, target_week_buyers)

# build trees for users that have not been paying in week 2 and 3
builder = MassTreeBuilder(100, False, {
    "TypeId": userSessionTypeId,
    "Document.Created": {"$gte": weeksAvailable[1], "$lte": weeksAvailable[2] + timedelta(days=7)},
    "Document.is_paying": 0  # they have to be non paying
}, "Document.UserId")
non_paying_usertrees = builder.make()

print "Updating {0} users with new features!".format(len(non_paying_usertrees))
chunkIndex = 0
for chnk in chunks(non_paying_usertrees, 1000):
    items = []
    print "Processing chunk: {0} [{1}]".format(str(chunkIndex + 1), str(len(chnk)))
    for item in chnk:
        uuid = item['uuid']
        tree = item['result']
        simtime = lin(tree_users_not_purchased_in_tweek, tree, "time")
        simscore = lin(tree_users_not_purchased_in_tweek, tree, "frequency")

        simtime2 = lin(tree_users_purchased_after_tweek, tree, "time")
        simscore2 = lin(tree_users_purchased_after_tweek, tree, "frequency")

        up = UpdateMany({
            "UserId": appId,
            "Document.uuid": uuid
        },  {'$set': {
            "Document.non_paying_s_time": simtime,
            "Document.non_paying_s_freq": simscore,
            "Document.paying_s_time": simtime2,
            "Document.paying_s_freq": simscore2,
        }})
        items.append(up)
    documents_col.bulk_write(items)
    chunkIndex += 1
