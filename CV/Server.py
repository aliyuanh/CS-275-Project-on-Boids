from camera import performDetect, detectOne, preload_network_tiny
from flask import request, Flask
from PIL import Image
from io import BytesIO
import base64
import io
import cv2
import numpy as np
from timeit import default_timer as timer
import os
# from multiprocessing import Pool

# Run once to preload model info
# Need to check/test whether this batch model is loaded per call (unclear from code)
print("Preloading Weights and Model")
preload_network_tiny()
print("Server is ready. Start Unity whenever you want to begin.")

# Note: pip install flask before running this. Run this script to start server
app = Flask(__name__)
num_processes = os.cpu_count()

app.config["DEBUG"] = True;
@app.route('/', methods=['GET'])
def home():
    return "homepage is here"

"""
    stringToImage: Takes in base64 bytes and writes them as an image to an image file with the name [imgID].png
        - imgID: uint32 -- GameObjectID
        - imgBytes: bytes -- Decoded bytes from base64 string

    return:
        - fileName: str -- Name of the path for the data
"""
def stringToImage(imgID, imgBytes):
    fileName = "{}".format(str(np.uint32(imgID)) + ".png")
    with open(fileName, "wb") as imgFile:
        imgFile.write(imgBytes)

    return fileName
    

"""
    JSON Format:
        - PackageID: int -- An internally used ID that we use to keep track of packages (to maintain synchronization across network)
        - PackageTime: float -- A floating point that stores Unity's built-in Timestamp to track latency between results from previously sent image
        - BoidDictionary: <int, string> -- A key-value dictionary that uses the Boid GameObject ID (int) to map it to the resulting RGB texture (base64 string)

    return:
        - (if PackageID <= 1) Message: str -- Message indicating that data has been ignored
        - (else) 
"""
@app.route('/getFromUnity', methods=['POST'])
def getData():
    #event data is the JSON passed from Unity & will be in this format:
    event_data = request.json
    # print(event_data.keys())

    # Note: Image textures only start to render after Start() in Unity, which is Frame 1
    # So we wait until Frame 2 onwards to start collecting data
    if event_data["PackageID"] > 1:

        # First decode base64 string to Uint8, which is interpreted as an np.array
        # and then it is decoded as an image with cv2
        # boid_vision_samples = {key:cv2.imdecode(np.fromstring(base64.b64decode(value), np.uint8),
        #                                     cv2.IMREAD_UNCHANGED) for key, value in event_data["BoidDictionary"].items()}
        startTime = timer()

        # Batch compute the resulting boxes, scores and classes
        results = detectOne(event_data["BoidImage"])
        event_data["BoidImage"] = results["boxes"]
        event_data["BoidClasses"] = results["classes"]

        # Send back the data to Unity and let Unity process NMS and IOU
        # results = detectOne(event_data["BoidImage"])
        # event_data["BoidImage"] = results

        endTime = timer()

        print("Results completed in {} using {} cores. Results: {}".format(endTime - startTime, num_processes, results))
        return event_data

    #return: names and aim vectors to food
    return "Ignoring first two frames of boid data..."

app.run(threaded=True)