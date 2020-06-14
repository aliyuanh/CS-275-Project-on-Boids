using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace BoidVision
{
    public class BoidVisionClient : MonoBehaviour
    {
        [Serializable]
        public class VisionJson
        {
            public int BoidID; // ID of the boid

            public int PackageID; // ID of the package to keep track of package sequence

            public string BoidImage; // Base64 String of Image

            public float Timestamp; // InstanceID of the boid that sent this object
        }

        [Serializable]
        public class ResultJson {

            public int BoidID; // ID of the boid

            public int PackageID;

            public List<List<float>> BoidImages; // Returning coordinates of boxes for the objects

            public List<int> BoidClasses; // Returning class labels for the corresponding boxes

            public List<float> BoidScores; // Returning scores per box

            public float Timestamp;
        }

        [Serializable]
        public class BoxOutline {
            public int ClassID;

            public Vector2 cornerMin; // Bottom left corner of the box relative to size of texture

            public Vector2 cornerMax; // Top right corner of the box relative to the size of the texture

            public Ray objectRay; // The ray from the camera viewport casted outwards

        }

        /// <summary>
        /// Helper class to get a return object from a coroutine
        /// </summary>
        public class CoroutineWithData
        {
            public Coroutine coroutine { get; private set; }
            public object result;
            private IEnumerator target;
            public CoroutineWithData(MonoBehaviour owner, IEnumerator target)
            {
                this.target = target;
                this.coroutine = owner.StartCoroutine(Run());
            }

            private IEnumerator Run()
            {
                while (target.MoveNext())
                {
                    result = target.Current;
                    yield return result;
                }
            }
        }

        // Variables for ML Vision model
        [SerializeField]
        Camera boidEyes;

        [SerializeField]
        Boid actualBoid;

        [SerializeField]
        TextAsset labelAsset;

        // Variables for tracking the boid image sent through the server
        int RecordID;
        int BoidID;
        int ImageSize;
        RenderTexture rt;
        string url;

        [HideInInspector]
        public bool CurrCameraActive;

        // List to hold all the information stored in the current boid
        [HideInInspector]
        public List<BoxOutline> boidDetectedObjects = new List<BoxOutline>();

        // Variables for debugging the boid image
        bool DrawBoxes;
        int ShiftX, ShiftY;
        Texture2D boxOutlineTexture;
        GUIStyle labelStyle = new GUIStyle();
        float scaleFactor;

        string[] labels;

        // Dictionary to handle the fact that some objects do not look realistic
        // For demo purposes, we will only use our computer vision model to detect birds
        // From experimentation and observation, we realized that our model tends to misclassify birds as kites, airplanes, and umbrellas
        Dictionary<int, int> ClassLabelDict = new Dictionary<int, int>() {
            {33, 14},
            {4, 14},
            {25, 14},
            {14, 14},
        };


        //collect every boid in the scene + give it a render texture with RGB vals
        void Awake()
        {
            RecordID = 0;
            DrawBoxes = false;
            labelStyle.normal.textColor = Color.green;
            labelStyle.fontSize = 20;

            ShiftX = 0;
            ShiftY = 0;
            scaleFactor = 1;

            boxOutlineTexture = new Texture2D(1, 1);
            boxOutlineTexture.SetPixel(0, 0, Color.green);
            boxOutlineTexture.Apply();

            string file = labelAsset.text;
            labels = file.Split('\n');
            CurrCameraActive = false;
        }

        /// <summary>
        /// Initializer function to create the parameters for a boid, and then call 'update vision' to get the boid to regularly check vision
        /// </summary>
        /// <param name="imgDim">Size of the image</param>
        /// <param name="SampleFrequency">Frequency by which we sent the images to the server and get a reply</param>
        /// <param name="serverURL">URL for Server</param>
        /// <param name="debug">Determines whether the boxes are drawn on top of the boid</param>
        public void InitializeBoid(int imgDim, float SampleFrequency, string serverURL, int ID, bool debug=false) {
            ImageSize = imgDim;
            BoidID = ID;
            rt = new RenderTexture(ImageSize, ImageSize, 24);
            url = serverURL;
            DrawBoxes = debug;

            InvokeRepeating("UpdateVision", 0.5f, SampleFrequency);
        }

        // Update is called once per frame
        void UpdateVision()
        {
            StartCoroutine(SendImage());
        }


        // Draws our Boxes on the GUI
        private void OnGUI()
        {
            if (DrawBoxes && boidEyes.isActiveAndEnabled && boidEyes.targetTexture == null && boidDetectedObjects.Count > 0 && CurrCameraActive) {

                Debug.Log("BOID " + BoidID + " with ID " + gameObject.GetInstanceID() + " Camera is active and enabled");

                foreach (BoxOutline box in boidDetectedObjects)
                {
                    DrawBoxOutline(box);
                }
            }
        }

        // Asynchronously sends an image to the server
        IEnumerator SendImage() {
            CoroutineWithData snapshotHandler = new CoroutineWithData(this, CollectBoidImages(ImageSize));
            VisionJson snapShot = new VisionJson();
            snapShot.PackageID = RecordID;
            snapShot.BoidID = BoidID;
            snapShot.Timestamp = Time.time;

            yield return snapshotHandler.coroutine;
            snapShot.BoidImage = (string)snapshotHandler.result;
            var jsonStr = JsonConvert.SerializeObject(snapShot);

            Debug.Log(jsonStr);
            StartCoroutine(sendPost(jsonStr));
            RecordID++;
        }

        // Takes a snapshot of the current boid image
        public IEnumerator CollectBoidImages(int dim)
        {
            Dictionary<int, string> BoidCaptureObject = new Dictionary<int, string>();
            boidEyes.targetTexture = rt;
            boidEyes.Render();

            CoroutineWithData textureHandler = new CoroutineWithData(this, toTexture2D(rt, dim));
            yield return textureHandler.coroutine;
            boidEyes.targetTexture = null;

            byte[] ResponseFileData = ((Texture2D)textureHandler.result).EncodeToPNG();
            yield return Convert.ToBase64String(ResponseFileData);
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

        // Sends the image to the python flask server
        IEnumerator sendPost(string payload)
        {
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                byte[] encodedPayload = new System.Text.UTF8Encoding().GetBytes(payload);
                www.uploadHandler = (UploadHandler)new UploadHandlerRaw(encodedPayload);
                www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("cache-control", "no-cache");

                yield return www.SendWebRequest();

                // While we wait for return value, we will loop and yield
                while (!www.isDone)
                {
                    yield return null;
                }

                if (string.IsNullOrEmpty(www.error))
                {
                    // Retrieved correct result
                    string message = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                    //Debug.Log(message);
                    CoroutineWithData resultHandler = new CoroutineWithData(this, DecodeResultJson(message));
                    yield return resultHandler.coroutine;
                    //ResultJson result = JsonConvert.DeserializeObject<ResultJson>(message, new BoidResultConverter(typeof(ResultJson)));

                    CoroutineWithData boxHandler = new CoroutineWithData(this, CreateBoxes((ResultJson)resultHandler.result));
                    yield return boxHandler.coroutine;

                    boidDetectedObjects = (List<BoxOutline>)boxHandler.result;
                }
                else
                {
                    Debug.Log("ERROR");

                }
            }
        }

        IEnumerator DecodeResultJson(string jsonStr) {
            yield return JsonConvert.DeserializeObject<ResultJson>(jsonStr, new BoidResultConverter(typeof(ResultJson)));
        }

        IEnumerator CreateBoxes(ResultJson result) {
            List<BoxOutline> boxes = new List<BoxOutline>();

            if (result.BoidImages == null)
                yield return boxes;

            UpdateWindowShift();

            // Box format: x_min,y_min,x_max,y_max
            // Assume boidImages.count == boidClasses.count
            for (int i = 0; i < result.BoidImages.Count; i++) {
                BoxOutline box = new BoxOutline();
                List<float> boxCoord = result.BoidImages[i];

                if (DrawBoxes)
                {
                    box.cornerMin = new Vector2(boxCoord[0] * scaleFactor + ShiftX, boxCoord[1] * scaleFactor + ShiftY);
                    box.cornerMax = new Vector2(boxCoord[2] * scaleFactor + ShiftX, boxCoord[3] * scaleFactor + ShiftY);
                }

                // Draw the ray by casting viewport point to ray
                Vector2 boxCenter = new Vector2((boxCoord[0] + boxCoord[2]) / 2.0f, (boxCoord[1] + boxCoord[3]) / 2.0f);
                boxCenter /= ImageSize;
                Vector3 temp = new Vector3(boxCenter.x, boxCenter.y, 0.0f);
                box.objectRay = boidEyes.ViewportPointToRay(temp);
                //box.cornerMin = new Vector2(boxCoord[0], boxCoord[1]);
                //box.cornerMax = new Vector3(boxCoord[2], boxCoord[3]);

                if (ClassLabelDict.ContainsKey(result.BoidClasses[i])) {
                    box.ClassID = ClassLabelDict[result.BoidClasses[i]];
                    boxes.Add(box);
                    Debug.Log(gameObject.GetInstanceID() + ": " + "Found class " + labels[box.ClassID]);
                }
            }

            actualBoid.ParseCV(boxes);
            // DebugBoxes(boxes);
            yield return boxes;
        }

        void DebugBoxes(List<BoxOutline> boxes) {
            foreach (BoxOutline box in boxes) {
                Debug.Log(string.Format("Found box-- ClassID: {0} P1: <{1}, {2}> P2: <{3}, {4}>", box.ClassID, box.cornerMin.x, box.cornerMin.y, box.cornerMax.x, box.cornerMax.y));
            }
        }

        /// <summary>
        /// Helper function to draw the box on your screen if you are in debug mode
        /// </summary>
        /// <param name="outline">The box that you want to draw</param>
        void DrawBoxOutline(BoxOutline outline)
        {
            Vector2 diff = outline.cornerMax - outline.cornerMin;
            //Vector2 midPoint = 0.5f * (outline.cornerMin + outline.cornerMax);

            Rect boundingBox = new Rect(outline.cornerMin.x, outline.cornerMin.y, diff.x + 10, diff.y + 10);
            Rect labelBox = new Rect(outline.cornerMin.x + 10, outline.cornerMin.y + 10, diff.x, diff.y);

            DrawRectangle(boundingBox, 4);
            GUI.Box(labelBox, labels[outline.ClassID], labelStyle);
        }

        /// <summary>
        /// Helper function to draw a hollow box around the object you are detecting
        /// </summary>
        /// <param name="area"></param>
        /// <param name="frameWidth"></param>
        void DrawRectangle(Rect area, int frameWidth)
        {
            Rect lineArea = area;
            lineArea.height = frameWidth;
            GUI.DrawTexture(lineArea, boxOutlineTexture); // Top line

            lineArea.y = area.yMax - frameWidth;
            GUI.DrawTexture(lineArea, boxOutlineTexture); // Bottom line

            lineArea = area;
            lineArea.width = frameWidth;
            GUI.DrawTexture(lineArea, boxOutlineTexture); // Left line

            lineArea.x = area.xMax - frameWidth;
            GUI.DrawTexture(lineArea, boxOutlineTexture); // Right line
        }

        void UpdateWindowShift() {
            int ScreenBound = (int)Mathf.Min(Screen.width, Screen.height);

            if (Screen.width < Screen.height) {
                ShiftY = (Screen.height - ScreenBound) / 2;
            }
            else {
                ShiftX = (Screen.width - ScreenBound) / 2;
            }

            scaleFactor = (float)ScreenBound / ImageSize;
        }

        /// <summary>
        /// Helper function for you to shoot a ray from a given box coordinate on the screen into the world
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Ray CastCameraPointToWorld(Vector2 point) {
            return boidEyes.ScreenPointToRay(point);
        }
    }
}