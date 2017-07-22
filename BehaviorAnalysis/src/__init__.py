from server import *
import time
 
if __name__ == "__main__":
    s = Server()
    print ("Starting server")
    s.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print ("Shutting server down")
    except Exception:
        pass
    s.shutdown()
