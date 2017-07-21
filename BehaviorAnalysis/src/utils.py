import os

def abs_path(fl):
    curr_dir = os.path.realpath(os.path.join(os.getcwd(), os.path.dirname(__file__)))
    abs_file_path = os.path.join(curr_dir, fl)
    return abs_file_path

def proportion(data,item):
    return list(data).count(item)/float(len(data))
