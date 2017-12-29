import os
from datetime import timedelta
import pickle
import glob
import json


def abs_path(fl=None):
    curr_dir = os.path.realpath(os.path.join(os.getcwd(), os.path.dirname(__file__)))
    if not fl:
        return curr_dir
    abs_file_path = os.path.join(curr_dir, fl)
    return abs_file_path


def par_path(fl=None):
    path = abs_path()
    path = os.path.abspath(os.path.join(path, os.pardir))
    return os.path.join(path, fl)

def proportion(data, item):
    return list(data).count(item) / float(len(data))

def chunks(l, n):
    """Yield successive n-sized chunks from l."""
    for i in xrange(0, len(l), n):
        yield l[i:i + n]

def parse_timespan(span):
    duration = span.split(":")
    hours = float(duration[0])
    mins = float(duration[1])
    seconds = float(duration[2])        
    duration = timedelta(hours=hours, minutes= mins, seconds=seconds)
    return duration


def save(obj, path):
    with open(path, 'wb') as f:
        pickle.dump(obj, f)

def latest_file(filepath):
    files = glob.glob(filepath + "*")
    last_file = max(files, key=os.path.getctime)
    return last_file

def load(path):
    out = None
    with open(path, 'rb') as f:
        out = pickle.load(f)
    return out

def load_json(path):
    out = None
    with open(path, 'r') as f:
        out = json.load(f)
    return out
