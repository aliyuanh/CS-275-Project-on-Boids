using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using BoidVision;
using Unity.Barracuda;

public class HeartOfTheSwarm : MonoBehaviour
{
    public GameObject boidPrefab;
    public GameObject CameraController;
    public int swarmCount;
    private int maxDistance = 45;
    private Vector3 origin;

    //[Header("Computer Vision Model Parameters (varies according to model)")]
    //[SerializeField]
    //NNModel ImageCVModel;

    [SerializeField]
    int ImageDim = 416;

    [SerializeField]
    float SampleFrequency = 0.1f; // How often we want the boid to capture an image

    [SerializeField]
    string URL = "http://127.0.0.1:5000/getFromUnity";

    List<Tuple<GameObject, Camera, RenderTexture>> boidCamPairs = new List<Tuple<GameObject, Camera, RenderTexture>>();
    CameraCapture captureManager;
    Camera currentCamera;
    int CameraIndex;
    int PackageIndex;

    //create boids (of # swarmCount) in a sphere randomly relative to the origin. 
    //also, create the camera controller. 
    void Awake()
    {
        //Model graph = ModelLoader.Load(ImageCVModel);//ModelLoader.LoadFromStreamingAssets(string.Format("Models/{0}.onnx", yoloGraphName));
        //string graphName = ImageCVModel.name;
        CameraIndex = 0;
        PackageIndex = 0;

        origin = transform.position;
        for (int i = 0; i < swarmCount; i++)
        {
            // Create a boid at a random location within reasonable radius
            GameObject boid = Instantiate(boidPrefab, origin + UnityEngine.Random.insideUnitSphere * maxDistance, Quaternion.identity);
            boid.gameObject.GetComponent<Boid>().origin = origin;

            RenderTexture rt = new RenderTexture(ImageDim, ImageDim, 24);
            Camera cam = boid.GetComponentInChildren<Camera>();
            cam.targetTexture = rt;

            // By passing the graphPB to the boid, it will automatically start running the CV model
            //boid.GetComponent<MLVisionController>().initializeModel(graph, ImageDim, ConfidenceThreshold, SampleFrequency, graphName, true);
            boid.GetComponent<BoidVisionClient>().InitializeBoid(ImageDim, SampleFrequency, URL, true);

            // Store to keep track of boids (maybe in future we can use it for debugging
            boidCamPairs.Add(new Tuple<GameObject, Camera, RenderTexture>(boid, cam, rt));
        }

        // Randomly select a boid from the boidCamPairs and choose one to be enabled for GUI
        ChangeCamera();
    }

    void ChangeCamera()
    {
        CameraIndex = (++CameraIndex) % boidCamPairs.Count;
        if (currentCamera)
            currentCamera.enabled = false;

        currentCamera = boidCamPairs[CameraIndex].Item2;
        currentCamera.enabled = true;
        Debug.Log("Currently using camera from boid " + boidCamPairs[CameraIndex].Item1.GetInstanceID());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeCamera();
        }
    }

}
