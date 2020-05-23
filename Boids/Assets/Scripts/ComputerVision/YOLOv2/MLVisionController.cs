using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Barracuda;

namespace YOLO
{
    // Based on: https://classifai.net/blog/tensorflow-onnx-unity/
    // Also see: https://github.com/Syn-McJ/TFClassify-Unity-Barracuda/blob/master/Assets/Scripts/Classifier.cs
    // https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx
    // For more models, see: https://github.com/onnx/models
    public class MLVisionController : MonoBehaviour
    {
        /// <summary>
        /// Struct to hold the bounding box information per frame. Based on definition of rectangle in OpenCV.
        /// </summary>
        public class BoxOutline
        {
            public string Label { get; set; }
            public float Score { get; set; }
            public Vector2 MidPoint { get; set; } = Vector2.zero;
            public Vector2 point1 { get; set; } = Vector2.zero;
            public Vector2 point2 { get; set; } = Vector2.zero;
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

        [Header("Camera Controller used to capture frames from camera and then render it as texture")]
        [SerializeField]
        public Camera boidEyes;

        // Internal Parameters for YOLO Implementation
        int inputSize;
        float MINIMUM_CONFIDENCE;
        bool DrawBoxes, UpdatingBoxes;
        RenderTexture rt;

        // Global variables copied from https://github.com/Syn-McJ/TFClassify-Unity-Barracuda/blob/master/Assets/Scripts/Detector.cs
        private static int IMAGE_MEAN = 117;
        private static float IMAGE_STD = 1;

        // Shift X and Y to map GUI onto Camera Image Texture
        float ShiftX;
        float ShiftY;
        float scaleFactor;

        List<BoxOutline> ModelResults = new List<BoxOutline>();

        Model graph;
        IWorker worker;
        ResultHandler handler;

        // Private data to keep track of boxOutlines for debug purposes only
        List<BoxOutline> results = new List<BoxOutline>();


        // Start is called before the first frame update
        void Awake()
        {
            inputSize = 0;
            MINIMUM_CONFIDENCE = 0.0f;
            boxOutlineTexture = new Texture2D(1, 1);
            boxOutlineTexture.SetPixel(0, 0, Color.green);
            boxOutlineTexture.Apply();

            labelStyle = new GUIStyle();
            labelStyle.fontSize = 50;
            labelStyle.normal.textColor = Color.green;
            DrawBoxes = false;
            UpdatingBoxes = true;

            ShiftX = 0;
            ShiftY = 0;
            scaleFactor = 1;
        }

        private void OnGUI()
        {
            if (DrawBoxes && !UpdatingBoxes && boidEyes.enabled)
            {
                // Recalculate change in Screen Size
                UpdateScreen();
                foreach (BoxOutline box in ModelResults)
                {
                    //DebugBox(box);
                    DrawBoxOutline(box);
                    break;
                }
            }
        }

        /// <summary>
        /// Helper function called by HeartOfTheSwarm to initialize a model for the graph
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="inputDim"></param>
        /// <param name="threshold"></param>
        public void initializeModel(Model model, int inputDim, float threshold, float captureFrequency, string name, bool debug=false)
        {
            // Configure parameters for model
            inputSize = inputDim;
            MINIMUM_CONFIDENCE = threshold;
            DrawBoxes = debug;

            // Insert model
            graph = model;
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, graph);

            // Create handler corresponding to the type of CV model we are using
            SetResultHandler(name);

            // Debug model by inspecting input/output
            // InspectModel();

            // Render camera to a RenderTexture
            rt = new RenderTexture(inputDim, inputDim, 24);

            // Once we have initialized the model, we repeatedly invoke the capture function
            InvokeRepeating("detectObjects", 0.1f, captureFrequency);
        }

        void detectObjects()
        {
            // Each frame, we capture the rendertexture as a tensor and feed it as input into our model
            // May want to also use IEnumerator
            StartCoroutine(DetectAsync());
            //StartCoroutine(DetectAsync(results =>
            //{
            //    // TODO: USE RESULTS TO MAP OUT NEW LOCATION FOR FOOD/FEAR
            //    ModelResults = new List<BoxOutline>(results);
            //}));
        }


        void SetResultHandler(string name)
        {
            //Debug.Log(name);
            switch (name)
            {
                case "tiny_yolo":
                case "tinyyolov2-8":
                    handler = gameObject.AddComponent<YOLOv2Handler>();
                    break;

                default:
                    handler = gameObject.AddComponent<ResultHandler>();
                    break;
            }
        }

        public IEnumerator DetectAsync()
        {
            boidEyes.targetTexture = rt;
            boidEyes.Render(); // Called in case the boid is not displaying its camera on the GUI
            using (var tensor = TransformInput(toTexture2D(), inputSize, inputSize))
            {

                // IMPORTANT NOTE: The output names of the graphs depend on which .pb file you are using
                // Outputs are a set of boxes of large, medium and small (in that order) for images detected
                // yolov3-tiny only outputs large and medium (for fast detection)

                // For reference ([INPUT NAME] [OUTPUT NAMES])
                //      -- yolov3-tiny: ['image_input'] ['conv2d_9/BiasAdd', 'conv2d_12/BiasAdd']
                //      -- yolov3-spp: ['image_input'] ['conv2d_59/BiasAdd', 'conv2d_67/BiasAdd', 'conv2d_75/BiasAdd']
                //      -- yolov3: ['image_input'] ['conv2d_58/BiasAdd', 'conv2d_66/BiasAdd', 'conv2d_74/BiasAdd']
                //      -- yolov4: ['image_input_3'] ['conv2d_109/BiasAdd', 'conv2d_101/BiasAdd', 'conv2d_93/BiasAdd']
                //      -- yolov3_test: ['image_input'] ['conv_81/BiasAdd', 'conv_93/BiasAdd', 'conv_105/BiasAdd']
                //      -- yolov2_tiny: ['image'] ['grid']

                var inputs = new Dictionary<string, Tensor>
                {
                    { graph.inputs[0].name, tensor }
                };
                yield return StartCoroutine(worker.ExecuteAsync(inputs));
                
                // IMPORTANT: Set the targetTexture to null to get the camera to render back onto the game editor
                // Ignore error on screen- appears when targetTexture is set to renderTexture, which is when we are sending data to model
                boidEyes.targetTexture = null;

                // Get all different bounding box outputs
                // InspectOutputs for debugging
                Tensor outputs = worker.PeekOutput();
                //InspectOutputs(outputs);

                CoroutineWithData boxHandler = new CoroutineWithData(this, handler.handleOutput?.Invoke(outputs, MINIMUM_CONFIDENCE));
                yield return boxHandler.coroutine;

                //Debug.Log(((List<BoxOutline>)boxHandler.result).Count);

                if (boidEyes.enabled)
                {
                    UpdatingBoxes = true;
                    ModelResults = (List<BoxOutline>)boxHandler.result;
                    //yield return OutputBoxes((List<BoxOutline>)boxHandler.result, DrawBoxes);
                    UpdatingBoxes = false;
                }

            }
        }

        /// <summary>
        /// Helper function to asynchronously update the boxes to be drawn on the GUI
        /// </summary>
        /// <param name="boxes"></param>
        /// <param name="drawBox"></param>
        /// <returns></returns>
        IEnumerator OutputBoxes(List<BoxOutline> boxes, bool drawBox=false)
        {
            foreach (BoxOutline box in boxes)
            {
                //DebugBox(box);
                ModelResults.Add(box);
            }

            UpdatingBoxes = false;
            yield return null;
        }

        #region DebugFunctions
        private static Texture2D boxOutlineTexture;
        private static GUIStyle labelStyle;
        /// <summary>
        /// Debug function to verify and visualize the imported input and output names of the NNModel exported from PB Graph in TF
        /// </summary>
        void InspectModel()
        {
            List<Model.Input> inputNames = graph.inputs;
            List<string> outputNames = graph.outputs;

            foreach (Model.Input inputNode in inputNames)
            {
                Debug.Log(inputNode.name);
            }

            foreach (string name in outputNames)
            {
                Debug.Log(name);
            }
        }

        /// <summary>
        /// Debug function to verify and visualize the outputs of the NNModel exported from PB Graph in TF
        /// </summary>
        /// <param name="output"></param>
        void InspectOutputs(Tensor output)
        {
            var shape = output.shape;
            Debug.Log("Barracuda Tensor outputs: " + shape + " or " + shape.batch + shape.height + shape.width + shape.channels);

        }

        /// <summary>
        /// Converts our RenderTexture from the camera to a color32 array
        /// </summary>
        /// <returns>Color32 Array representation of the RenderTexture rt</returns>
        Color32[] toTexture2D()
        {
            Texture2D text = new Texture2D(inputSize, inputSize, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            text.ReadPixels(new Rect(0, 0, inputSize, inputSize), 0, 0);
            text.Apply();

            byte[] bytes;
            bytes = text.EncodeToPNG();
            System.IO.File.WriteAllBytes(Application.persistentDataPath + "/temp.png", bytes);

            return text.GetPixels32();
        }

        private void DrawBoxOutline(BoxOutline outline)
        {
            var x = outline.MidPoint.x * scaleFactor + ShiftX;
            var width = (outline.point2.x - outline.point1.x) * scaleFactor;
            var y = outline.MidPoint.y * scaleFactor + ShiftY;
            var height = (outline.point2.y - outline.point1.y) * scaleFactor;

            DrawRectangle(new Rect(x, y, width, height), 4, Color.red);
            DrawLabel(new Rect(x + 10, y + 10, 200, 20), $"{outline.Label}: {(int)(outline.Score * 100)}%");
        }


        public static void DrawRectangle(Rect area, int frameWidth, Color color)
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


        private static void DrawLabel(Rect position, string text)
        {
            GUI.Label(position, text, labelStyle);
        }

        void DebugBox(BoxOutline box)
        {
            Debug.Log(string.Format("Box-- Location: <{0},{1}>, Label: {2}, Confidence: {3}", box.MidPoint.x, box.MidPoint.y, box.Label, box.Score));
        }

        public static Tensor TransformInput(Color32[] pic, int width, int height)
        {
            float[] floatValues = new float[width * height * 3];

            for (int i = 0; i < pic.Length; ++i)
            {
                var color = pic[i];

                floatValues[i * 3 + 0] = (color.r - IMAGE_MEAN) / IMAGE_STD;
                floatValues[i * 3 + 1] = (color.g - IMAGE_MEAN) / IMAGE_STD;
                floatValues[i * 3 + 2] = (color.b - IMAGE_MEAN) / IMAGE_STD;
            }

            return new Tensor(1, height, width, 3, floatValues);
        }

        void UpdateScreen()
        {
            if (Screen.width < Screen.height)
            {
                ShiftY = (Screen.height - Screen.width) * 0.5f;
                //scaleFactor = Screen.width / (float)inputSize;
            }
            else
            {
                ShiftX = (Screen.width - Screen.height) * 0.5f;
                //scaleFactor = Screen.height / (float)inputSize;
            }
        }
        #endregion DebugFunctions
    }
}