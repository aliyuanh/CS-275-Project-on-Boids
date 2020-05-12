using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;
using UnityEngine;

public class CameraCapture : MonoBehaviour
{
    public KeyCode screenshotKey;
    List<KeyValuePair<GameObject, Camera>> boidCamPairs;
    List<RenderTexture> renderTexs;
    void Start()
    {
        boidCamPairs = new List<KeyValuePair<GameObject, Camera>>();
        renderTexs = new List<RenderTexture>();
        GameObject[] boids = GameObject.FindGameObjectsWithTag("Boid");
        foreach(GameObject b in boids)
        {
            Camera boidCam = b.GetComponentInChildren<Camera>();
            RenderTexture rt = new RenderTexture(256, 256, 24);
            boidCam.targetTexture = rt;
            renderTexs.Add(rt);
            boidCamPairs.Add(new KeyValuePair<GameObject, Camera>(b, boidCam));
            //Debug.Log(b.name);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            Debug.Log("Screenshot key pressed!");
            saveAllImages();
        }
    }
    public void saveAllImages()
    {
        int iterator = 0;
        foreach(KeyValuePair<GameObject, Camera> pair in boidCamPairs){
            SaveTexture(pair.Key.name, renderTexs[iterator]);
            iterator++;
        }
    }
    public void SaveTexture(string myName, RenderTexture rt)
    {
        //ppm format to sent to 
        string headerStr = string.Format("P6\n{0} {1}\n255\n", 256, 256);
        byte[] fileHeader = System.Text.Encoding.ASCII.GetBytes(headerStr);
        byte[] ResponseFileData = toTexture2D(rt).GetRawTextureData();

        byte[] bytes = toTexture2D(rt).EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Captures/BoidViews/" + myName+ ".png", bytes);
    }
    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D text = new Texture2D(256, 256, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        text.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        text.Apply();
        return text;
    }
}
