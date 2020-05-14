import flask, json
from flask import request
#note: pip install flask before running this. Run this script to start server
app = flask.Flask(__name__)
app.config["DEBUG"] = True;
@app.route('/', methods=['GET'])
def home():
    return "homepage is here"


#input: JSON of names + images from each boid from Unity. 
#output: JSON of names + aim vectors to food for each boid 
@app.route('/getFromUnity', methods=['POST'])
def getData():
    #event data is the JSON passed from Unity & will be in this format:
    #[{"name": boidName, "picData": "raw texture encoded as base 64 string"}, {...}]
    event_data = request.json
    #return: names and aim vectors to food
    return "getting data from unity!" + str(len(str(event_data)))

app.run();