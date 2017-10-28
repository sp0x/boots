import os
import urllib
from pymongo import MongoClient

def get(name):
    """Gets a setting"""
    return os.environ[name]

def get_db_settings():
    """Gets the settings for mongodb"""
    username = get('mongo_user')
    port = get('mongo_port')
    url = "mongodb://{0}@{1}:{2}/{3}?authSource=admin".format(username,
      get('mongo_host'), port, get('mongo_db'))
    return {
        'password' : urllib.quote_plus(get('mongo_pass')),
        'url' : url
    }

def get_db():
    db_name = get('mongo_db')
    client = MongoClient(get_db_url())
    return client[db_name]

def get_db_url():
    """Gets the url to mongodb"""
    username = get('mongo_user')
    port = get('mongo_port')
    password = urllib.quote_plus(get('mongo_pass'))
    url = "mongodb://{0}:{1}@{2}:{3}/{4}?authSource=admin".format(username, password,
      get('mongo_host'), port, get('mongo_db'))
    return url
