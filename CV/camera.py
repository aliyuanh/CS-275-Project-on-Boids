import numpy as np
import pandas as pd
import time
import torch
import torch.nn as nn
import cv2
from utility import *
from yolo import Darknet
import random
import argparse
import pickle as pkl
import threading 
import base64
# from multiprocessing import Pool

def arg_parse():
    """Parsing arguments"""
    parser = argparse.ArgumentParser(description='YOLO v3 Real Time Detection')
    parser.add_argument("--confidence", dest="confidence", help="Object Confidence to filter predictions", default=0.25)
    parser.add_argument("--nms_thresh", dest="nms_thresh", help="NMS Threshhold", default=0.4)
    parser.add_argument("--reso", dest='reso', help=
    "Input resolution of the network. Increase to increase accuracy. Decrease to increase speed",
                        default="256", type=str)
    return parser.parse_args()


def prep_image(img, inp_dim):
    """Converting a numpy array of frame into PyTorch tensor"""
    orig_im = img
    dim = orig_im.shape[1], orig_im.shape[0]
    img = cv2.resize(orig_im, (inp_dim, inp_dim))
    img_ = img[:, :, ::-1].transpose((2, 0, 1)).copy()
    img_ = torch.from_numpy(img_).float().div(255.0).unsqueeze(0)
    return img_, orig_im, dim


"""
    Global Configurations for Network to be used by Server.py
    num_classes and confidence should be modified to suit intelligence of boid
"""
# Global parameters for YOLOv3 Model
yolo3_config = {
    "cfgfile": "cfg/yolov3.cfg",
    "weightsfile": "cfg/yolov3.weights",
    "num_classes": 80,
    "bbox_attrs": 85,
    "confidence": 0.75,
    "nms_thresh": 0.4,
    "img_res": 416
}

yolo3_tiny_config = {
    "cfgfile": "cfg/yolov3-tiny.cfg",
    "weightsfile": "cfg/yolov3-tiny.weights",
    "num_classes": 80,
    "bbox_attrs": 85,
    "confidence": 0.5,
    "nms_thresh": 0.4,
    "img_res": 416
}

# Global Objects for storing model 
classes = None
colors = None
model = None
config = None

"""
    detectOne: Helper function that is called per process to detect one image at a time
        - boid_img: nparray -- Image that was decoded for processing in our model
"""
def detectOne(boid_image, image_name="test.jpg", showImage=False, unityCompute=False):
    boid_img = cv2.imdecode(np.fromstring(base64.b64decode(boid_image), np.uint8), cv2.IMREAD_UNCHANGED)
    inp_dim = config["img_res"]
    img, orig_im, dim = prep_image(boid_img, inp_dim)

    img.to(device)

    # Batch of images x Number of boxes x Bounds of box
    output = model(img)
    output = write_results(output, config["confidence"], config["num_classes"], nms_conf=config["nms_thresh"])

    results = {"boxes": [], "classes": []}

    # There is nothing so we just continue
    if type(output) == int:
        return results

    output[:, 1:5] = torch.clamp(output[:, 1:5], 0.0, float(inp_dim)) / inp_dim

    output[:, [1, 3]] *= boid_img.shape[1]
    output[:, [2, 4]] *= boid_img.shape[0]
    list(map(lambda x: format_results(x, results), output))

    if showImage:
        # print(results)
        list(map(lambda x: write(x, orig_im), output))
        cv2.imwrite(image_name, orig_im)
        # cv2.imshow("Debugger", orig_im)
        # key = cv2.waitKey(1)

    return results


"""
    write: Helper function that is called through lambda function for debugging; draws the specified rectangles onto the given image
        - x: tensor of outpus from the Yolov3 model -- The output from the model specifying the labels and the boxes that are to be drawn
        - img: openCV image (encoded as an tensor) -- The image that will be drawn on
"""
def write(x, img):
    global classes, colors
    c1 = tuple(x[1:3].int())
    c2 = tuple(x[3:5].int())
    cls = int(x[-1])
    label = str(classes[cls])
    color = random.choice(colors)
    cv2.rectangle(img, c1, c2, color, 1)
    t_size = cv2.getTextSize(label, cv2.FONT_HERSHEY_PLAIN, 1, 1)[0]
    c2 = c1[0] + t_size[0] + 3, c1[1] + t_size[1] + 4
    # cv2.rectangle(img, c1, c2, color, -1)
    cv2.putText(img, label, (c1[0], c1[1] + t_size[1] + 4),
                cv2.FONT_HERSHEY_PLAIN, 1, [225, 255, 255], 1);
    return img

"""
    format_results: Helper function that is called through lambda function for debugging; draws the specified rectangles onto the given image
        - x: tensor of outpus from the Yolov3 model -- The output from the model specifying the labels and the boxes that are to be drawn
        - img: openCV image (encoded as an tensor) -- The image that will be drawn on
"""
def format_results(x, results):
    c1 = tuple(x[1:3].int().numpy())
    c2 = tuple(x[3:5].int().numpy())
    intLabel = int(x[-1])

    results["boxes"].append([int(c1[0]), int(c1[1]), int(c2[0]), int(c2[1])])
    results["classes"].append(intLabel)

    return results

"""
    preload_network: Helper function to initialize the model for YOLOv3 based on CFG and Weights
"""
def preload_network():
    global config, model, classes, colors
    config = yolo3_config

    model = Darknet(config["cfgfile"]).to(device)
    model.load_weights(config["weightsfile"])
    model.network_info["height"] = config["img_res"]
    inp_dim = int(model.network_info["height"])

    assert inp_dim % 32 == 0
    assert inp_dim > 32

    classes = load_classes('data/coco.names')
    colors = pkl.load(open("color/pallete", "rb"))

    model.eval()

"""
    preload_network_tiny: Helper function to initialize the model for YOLOv3-tiny based on CFG and Weights
"""
def preload_network_tiny():
    global config, model, classes, colors
    config = yolo3_tiny_config

    model = Darknet(config["cfgfile"]).to(device)
    model.load_weights(config["weightsfile"])
    model.network_info["height"] = config["img_res"]
    inp_dim = int(model.network_info["height"])

    assert inp_dim % 32 == 0
    assert inp_dim > 32

    classes = load_classes('data/coco.names')
    colors = pkl.load(open("color/pallete", "rb"))

    model.eval()

"""
    performDetect: Helper function to detect a batch of image
        - img_batch: list(img) -- list of cv2 decoded images as np arrays
        - showImage: bool -- boolean to help you determine whether you draw and show the image or not
"""
def performDetect(img_batch, showImage=False):
    inp_dim = config["img_res"]
    img_results = []
    for boid_frame in img_batch:
        img, orig_im, dim = prep_image(boid_frame, inp_dim)

        img.to(device)

        output = model(img)
        output = write_results(output, config["confidence"], config["num_classes"], nms_conf=config["nms_thresh"])

        results = {"boxes": [], "classes": []}

        # There is nothing so we just continue
        if type(output) == int:
            img_results.append(results)
            continue

        output[:, 1:5] = torch.clamp(output[:, 1:5], 0.0, float(inp_dim)) / inp_dim

        output[:, [1, 3]] *= boid_frame.shape[1]
        output[:, [2, 4]] *= boid_frame.shape[0]
        list(map(lambda x: format_results(x, results), output))

        if showImage:
            print(results)
            list(map(lambda x: write(x, orig_im), output))
            cv2.imshow("Debugger", orig_im)
            key = cv2.waitKey(1)

        img_results.append(results)

    return img_results
