from server import *
import time
import classifiers
from utils import load_json, par_path
 
if __name__ == "__main__":
    classifiers.model_table = load_json(par_path("data/classlist.json"))
    classifiers.param_table = load_json(par_path("data/paramlist.json"))
    s = Server()
    print ("Starting server")
    s.start()
    try:
        while True:
            time.sleep(5)
    except KeyboardInterrupt:
        print ("Shutting server down")
    except Exception:
        pass
    s.shutdown()
