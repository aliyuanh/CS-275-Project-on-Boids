using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;
using YOLO;

public class CameraCapture : MonoBehaviour
{
    //public KeyCode screenshotKey;
    // Variables and parameters for JSON Server
    private string serverURL = "http://127.0.0.1:5000/";

    //Dictionary<int, string> BoidCaptureObject = new Dictionary<int, string>();
    int RecordID;

    [System.Serializable]
    public class VisionJson
    {
        public int PackageID; // ID of the package to keep track of package sequence

        public Dictionary<int, string> BoidDictionary { get; set; } // Key Value pair to map images from GameObjectID

        public float PackageTime; // Time that was sent in the package in Unity Time
    }


    // Variables for ML Vision model
    [SerializeField]
    Camera boidEyes;


    //collect every boid in the scene + give it a render texture with RGB vals
    void Awake()
    {
        RecordID = 0;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// FUNCTIONS FOR SERVER IMPLEMENTATION 
    /// ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Convert every boid + its view to a string, then send it. 
    public IEnumerator sendAllImages(List<Tuple<GameObject, Camera, RenderTexture>> boidCamPairs, int imgDim)
    {
        //if (RecordID > 2)
        //    return;

        VisionJson boidsPayload = new VisionJson();
        boidsPayload.PackageID = RecordID;

        YOLO.MLVisionController.CoroutineWithData snapshotHandler = new MLVisionController.CoroutineWithData(this, CollectBoidImages(boidCamPairs, imgDim));

        yield return snapshotHandler.coroutine;
        boidsPayload.BoidDictionary = (Dictionary<int, string>)snapshotHandler.result;
        boidsPayload.PackageTime = Time.time;
        YOLO.MLVisionController.CoroutineWithData jsonHandler = new MLVisionController.CoroutineWithData(this, ConvertToSerializable(boidsPayload));

        yield return jsonHandler.coroutine;
        Debug.Log((string)jsonHandler.result);
        StartCoroutine(sendPost(serverURL + "getFromUnity", (string)jsonHandler.result));
        RecordID++;
    }

    //converts a boid's name and render texture to a PayloadJson object 
    //public IEnumerator ConvertTextureToPayload(int ID, RenderTexture rt, int imgDim)
    //{
    //    YOLO.MLVisionController.CoroutineWithData textureHandler = new MLVisionController.CoroutineWithData(this, toTexture2D(rt, imgDim));
    //    yield return textureHandler.coroutine;

    //    byte[] ResponseFileData = ((Texture2D)textureHandler.result).EncodeToPNG();

    //    if (BoidCaptureObject.ContainsKey(ID))
    //        BoidCaptureObject[ID] = Convert.ToBase64String(ResponseFileData);
    //    else
    //        BoidCaptureObject.Add(ID, Convert.ToBase64String(ResponseFileData));

    //    yield return null;
    //}

    public IEnumerator ConvertToSerializable(VisionJson payload) {
        yield return JsonConvert.SerializeObject(payload);
    }

    public IEnumerator ConvertToString(byte[] data) {
        yield return Convert.ToBase64String(data);
    }

    public IEnumerator CollectBoidImages(List<Tuple<GameObject, Camera, RenderTexture>> boidCamPairs, int dim) {
        Dictionary<int, string> BoidCaptureObject = new Dictionary<int, string>();
        foreach (var tup in boidCamPairs)
        {
            YOLO.MLVisionController.CoroutineWithData textureHandler = new MLVisionController.CoroutineWithData(this, toTexture2D(tup.Item3, dim));
            yield return textureHandler.coroutine;

            byte[] ResponseFileData = ((Texture2D)textureHandler.result).EncodeToPNG();

            MLVisionController.CoroutineWithData stringHandler = new MLVisionController.CoroutineWithData(this, ConvertToString(ResponseFileData));
            yield return stringHandler.coroutine;
            BoidCaptureObject[tup.Item1.GetInstanceID()] = (string)stringHandler.result;
        }

        yield return BoidCaptureObject;
    }

    //turns a render texture into a toTexture2D
    IEnumerator toTexture2D(RenderTexture rTex, int imgDim)
    {
        Texture2D text = new Texture2D(imgDim, imgDim, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        text.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        text.Apply();
        yield return text;
    }

    //HTTP request to the server with a given JSON payload
    //void sendPost(string url, string payload)
    //{
    //    //Debug.Log(url);
    //    UnityWebRequest webRequest = new UnityWebRequest(serverURL, "POST");
    //    byte[] encodedPayload = new System.Text.UTF8Encoding().GetBytes(payload);
    //    webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(encodedPayload);
    //    webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
    //    webRequest.SetRequestHeader("Content-Type", "application/json");
    //    webRequest.SetRequestHeader("cache-control", "no-cache");
    //    UnityWebRequestAsyncOperation requestHandle = webRequest.SendWebRequest();

    //    requestHandle.completed += delegate (AsyncOperation pOperation)
    //    {
    //        Debug.Log(webRequest.responseCode);
    //        Debug.Log(webRequest.downloadHandler.text);
    //    };
    //}

    public IEnumerator sendPost(string url, string payload)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] encodedPayload = new System.Text.UTF8Encoding().GetBytes(payload);
            www.uploadHandler = (UploadHandler) new UploadHandlerRaw(encodedPayload);
            www.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("cache-control", "no-cache");

            yield return www.SendWebRequest();

            while (!www.isDone)
            {
                yield return null;
            }

            if (string.IsNullOrEmpty(www.error))
            {
                //handle the problem
                var result = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                Debug.Log(result);
            }
            else
            {
                Debug.Log("ERROR");
            }
        }

    }
}
