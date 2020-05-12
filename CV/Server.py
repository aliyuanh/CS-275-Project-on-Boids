import flask, json
from flask import request

app = flask.Flask(__name__)
app.config["DEBUG"] = True;
@app.route('/', methods=['GET'])
def home():
    return "<h1>Distant Reading Archive</h1><p>This site is a prototype API for distant reading of science fiction novels.</p>"

@app.route('/getFromUnity', methods=['GET'])
def getData():
    event_data = request.json
    return "getting data from unity!" + json.dumps(event_data)

app.run();