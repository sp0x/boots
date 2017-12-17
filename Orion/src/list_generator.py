import inspect
import json
from utils import par_path

# Use to regenerate the classlist.json and paramlist.json files

namespaces = [
    "sklearn.ensemble",
    "sklearn.tree",
    "sklearn.neighbors",
    "sklearn.linear_model",
    "sklearn.naive_bayes",
    "sklearn.semi_supervised",
    "sklearn.svm",
    "sklearn.gaussian_process"
]
paramlist = {}
klasslist = {}
for n in namespaces:
    mod = __import__(n, fromlist=[n.split(".")[1]])
    for name in dir(mod):
        obj = getattr(mod, name)
        if inspect.isclass(obj):
            try:
                ar = inspect.getargspec(obj.__init__)
                args = ar.args
                args.remove('self')
                params = dict(zip(args, ar.defaults))
                if 'random_state' in params:
                    params.pop('random_state')
                if 'memory' in params:
                    params.pop('memory')
                klasslist[name] = n
                paramlist[name] = params
            except:
                pass
with open(par_path("data/classlist.json"), "w") as f:
    json.dump(klasslist, f, indent=4)
with open(par_path("data/paramlist.json"), "w") as fp:
    json.dump(paramlist, fp, indent=4)

