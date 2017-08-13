import os
from datetime import datetime, timedelta
import pickle


def abs_path(fl):
    curr_dir = os.path.realpath(os.path.join(os.getcwd(), os.path.dirname(__file__)))
    abs_file_path = os.path.join(curr_dir, fl)
    return abs_file_path


def proportion(data, item):
    return list(data).count(item) / float(len(data))


def parse_timespan(span):
    duration = span.split(":")
    hours = float(duration[0])
    mins = float(duration[1])
    seconds = float(duration[2])        
    duration = timedelta(hours= hours, minutes= mins, seconds=seconds)
    return duration


def save(obj, path):
    with open(path, 'wb') as f:
        pickle.dump(obj, f)


def load(path):
    out = None
    with open(path, 'rb') as f:
        out = pickle.load(f)
    return out