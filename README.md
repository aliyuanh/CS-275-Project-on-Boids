# CS 275 Project on Boids
 UCLA CS275 (Artificial Life with Computer Graphics and Computer Vision) final term project on boids and bird behavior. 

 ## Overview of Computer Vision
 The current working version uses YOLOv3-tiny, which is a lightweight real-time 2D object detection network. It is integrated with a Python Flask server, in order to process Unity-generated images with Pytorch. There are multiple different scripts, but the important things to note are:

[BoidVisionClient.cs](): This script is attached to each boid and sends an image to the Python server at an interval set by "SampleFrequency" in [HeartOfTheSwarm.cs](). **To get the coordinates of the boxes and detected classes, use 'CastCameraPointToWorld' (defined in this script)**

[Server.py](): The server-side handler that is used to manage requests from the Unity script. Currently, performance depends on the number of threads that your computer can handle. **If you try and set 'swarmCount' > (Max # Threads flask can handle) in [HeartOfTheSwarm.cs](), Unity will start freezing and may crash.**

## Current Configuration
Sample Frequency: 0.3 - 0.5 (Use a higher frequency for a higher number of swarm count)
Swarm Count: 1 - 5 (For debugging purposes, run 1 boid with 0.3 frequency)
Image Size: 416 (This is fixed from configuration of YOLO, but can be reconfigured with additional training)
Enable/Disable Debug Mode: In [HeartOfTheSwarm.cs](), line 57, set InitializeBoid(ImageDim, SampleFrequency, URL, [true for debug mode, false otherwise]). **This determines whether you will see the bounding boxes on your camera when the scene is playing**


## How to Use
1. Install all python requirements by doing pip install -r CV/requirements.txt
2. Run python ./Server.py
3. Configure BoidInitializer in Unity initialScene
4. Run Unity scene
5. Currently, the game camera will automatically attach to the boid's camera. To change which boid you are looking at, press the 'C' key while the scene is playing. **This only works if you have multiple boids in the scene**