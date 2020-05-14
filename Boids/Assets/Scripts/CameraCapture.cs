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

public class CameraCapture : MonoBehaviour
{
    public KeyCode screenshotKey;
    List<(GameObject, Camera, RenderTexture)> boidCamPairs;
    private string serverURL = "http://127.0.0.1:5000/";

    //each boid's data. name for boid name, picData is a base64 encoded string of the pic
    [Serializable]
    public class PayloadJson
    {
        public string name;
        public string picData;
    }

    //collect every boid in the scene + give it a render texture with RGB vals
    void Start()
    {
        boidCamPairs = new List<(GameObject, Camera, RenderTexture)>();
        GameObject[] boids = GameObject.FindGameObjectsWithTag("Boid");
        foreach(GameObject b in boids)
        {
            Camera boidCam = b.GetComponentInChildren<Camera>();
            RenderTexture rt = new RenderTexture(256, 256, 24);
            boidCam.targetTexture = rt;
            boidCamPairs.Add((b, boidCam, rt));
        }
    }

    //check for screenshot key pressed. If it's pressed, send the boids' images to the server 
    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            Debug.Log("Screenshot key pressed!");
            sendAllImages();
        }
    }
    //Convert every boid + its view to a string, then send it. 
    public void sendAllImages()
    {
        List<string> boidsPayload = new List<string>();
        foreach(var tup in boidCamPairs){
            PayloadJson myPayload = ConvertTextureToPayload(tup.Item1.name, tup.Item3);
            string payload = JsonConvert.SerializeObject(myPayload);
            boidsPayload.Add(payload);
        }
        string big = JsonConvert.SerializeObject(boidsPayload);
        sendPost(serverURL + "getFromUnity", big);

    }
    //converts a boid's name and render texture to a PayloadJson object 
    public PayloadJson ConvertTextureToPayload(string myName, RenderTexture rt)
    {
        byte[] ResponseFileData = toTexture2D(rt).GetRawTextureData();
        PayloadJson myPayload = new PayloadJson();
        myPayload.name = myName;
        myPayload.picData = Convert.ToBase64String(ResponseFileData);
        return myPayload;
    }
    //turns a render texture into a toTexture2D
    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D text = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        RenderTexture.active = rTex;
        text.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        text.Apply();
        return text;
    }
    //HTTP request to the server with a given JSON payload
    void sendPost(string url, string payload)
    {
        UnityWebRequest webRequest = new UnityWebRequest(url, "POST");
        byte[] encodedPayload = new System.Text.UTF8Encoding().GetBytes(payload);
        webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(encodedPayload);
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("cache-control", "no-cache");
        UnityWebRequestAsyncOperation requestHandle = webRequest.SendWebRequest();
        requestHandle.completed += delegate (AsyncOperation pOperation)
        {
            Debug.Log(webRequest.responseCode);
            Debug.Log(webRequest.downloadHandler.text);
        };
    }
}
