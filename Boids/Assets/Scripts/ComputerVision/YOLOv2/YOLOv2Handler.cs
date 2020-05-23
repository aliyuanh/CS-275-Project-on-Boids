using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Linq;
using System;
using System.Text.RegularExpressions;


namespace YOLO
{
    public class YOLOv2Handler : ResultHandler
    {
        #region ModelParameters
        public const int ROW_COUNT = 13;
        public const int COL_COUNT = 13;
        public const int BOXES_PER_CELL = 5;
        public const int BOX_INFO_FEATURE_COUNT = 5;
        public const int CLASS_COUNT = 20;
        public const float CELL_WIDTH = 32;
        public const float CELL_HEIGHT = 32;
        public const string YOLOV2_LABEL_PATH = "yolov2_tiny";

        private float[] anchors = new float[]
        {
            1.08F, 1.19F, 3.42F, 4.41F, 6.63F, 11.38F, 9.42F, 5.11F, 16.62F, 10.52F
        };
        private string[] labels;
        #endregion ModelParameters

        private void Awake()
        {
            string file = (Resources.Load(string.Format("Labels/{0}", YOLOV2_LABEL_PATH)) as TextAsset).text;
            labels = Regex.Split(file, ",");
            //foreach (string label in labels)
            //    Debug.Log(label);
            handleOutput += ParseOutputs;
         }

        #region BoxParsingFunctions
        private IEnumerator ParseOutputs(Tensor yoloModelOutput, float threshold = .3F)
        {
            var boxes = new List<MLVisionController.BoxOutline>();
            for (int cy = 0; cy < COL_COUNT; cy++)
            {
                for (int cx = 0; cx < ROW_COUNT; cx++)
                {
                    for (int box = 0; box < BOXES_PER_CELL; box++)
                    {
                        var channel = (box * (CLASS_COUNT + BOX_INFO_FEATURE_COUNT));

                        // Start coroutines to synthesize bounding boxes, their classes, and dimensions
                        MLVisionController.CoroutineWithData BBDHandler = new MLVisionController.CoroutineWithData(this, ExtractBoundingBoxDimensions(yoloModelOutput, cx, cy, channel));
                        MLVisionController.CoroutineWithData ConfidenceHandler = new MLVisionController.CoroutineWithData(this, GetConfidence(yoloModelOutput, cx, cy, channel));
                        MLVisionController.CoroutineWithData ClassHandler = new MLVisionController.CoroutineWithData(this, ExtractClasses(yoloModelOutput, cx, cy, channel));

                        // Get confidence
                        yield return ConfidenceHandler.coroutine;
                        float confidence = (float)ConfidenceHandler.result;

                        if (confidence >= threshold)
                        {
                            // Get the predicted classes
                            yield return ClassHandler.coroutine;
                            MLVisionController.CoroutineWithData TopResultHandler =
                                new MLVisionController.CoroutineWithData(this, GetTopResult((float[])ClassHandler.result));

                            yield return TopResultHandler.coroutine;
                            var (topResultIndex, topResultScore) = (ValueTuple<int, float>)TopResultHandler.result;
                            float topScore = topResultScore * confidence;

                            //Debug.Log(topScore);
                            if (topScore >= threshold)
                            {
                                Debug.Log("Confident bounding box detected!");
                                yield return BBDHandler.coroutine;
                                MLVisionController.CoroutineWithData OutlineHandler = 
                                    new MLVisionController.CoroutineWithData(this, MapBoundingBoxToCell(cx, cy, box, (BoundingBoxDimensions)BBDHandler.result));

                                yield return OutlineHandler.coroutine;
                                var mappedBoxOutline = (CellDimensions)OutlineHandler.result;
                                boxes.Add(new MLVisionController.BoxOutline
                                {
                                    Label = labels[topResultIndex],
                                    Score = topScore,
                                    MidPoint = new Vector2(mappedBoxOutline.X - mappedBoxOutline.Width / 2, mappedBoxOutline.Y - mappedBoxOutline.Height / 2),
                                    point1 = new Vector2(mappedBoxOutline.X - mappedBoxOutline.Width, mappedBoxOutline.Y - mappedBoxOutline.Height),
                                    point2 = new Vector2(mappedBoxOutline.X, mappedBoxOutline.Y)
                                });
                            }
                        }
                    }
                }
            }

            yield return boxes;
        }

        private IEnumerator ExtractBoundingBoxDimensions(Tensor modelOutput, int x, int y, int channel)
        {
            yield return new BoundingBoxDimensions
            {
                X = modelOutput[0, x, y, channel],
                Y = modelOutput[0, x, y, channel + 1],
                Width = modelOutput[0, x, y, channel + 2],
                Height = modelOutput[0, x, y, channel + 3]
            };
        }

        private IEnumerator GetConfidence(Tensor modelOutput, int x, int y, int channel)
        {
            yield return Sigmoid(modelOutput[0, x, y, channel + 4]);
        }

        public IEnumerator ExtractClasses(Tensor modelOutput, int x, int y, int channel)
        {
            float[] predictedClasses = new float[CLASS_COUNT];
            int predictedClassOffset = channel + BOX_INFO_FEATURE_COUNT;

            for (int predictedClass = 0; predictedClass < CLASS_COUNT; predictedClass++)
            {
                predictedClasses[predictedClass] = modelOutput[0, x, y, predictedClass + predictedClassOffset];
            }

            yield return Softmax(predictedClasses);
        }

        private IEnumerator GetTopResult(float[] predictedClasses)
        {
            yield return predictedClasses
                .Select((predictedClass, index) => (Index: index, Value: predictedClass))
                .OrderByDescending(result => result.Value)
                .First();
        }

        private IEnumerator MapBoundingBoxToCell(int x, int y, int box, BoundingBoxDimensions boxDimensions)
        {
            yield return new CellDimensions
            {
                X = ((float)y + Sigmoid(boxDimensions.X)) * CELL_WIDTH,
                Y = ((float)x + Sigmoid(boxDimensions.Y)) * CELL_HEIGHT,
                Width = (float)Math.Exp(boxDimensions.Width) * CELL_WIDTH * anchors[box * 2],
                Height = (float)Math.Exp(boxDimensions.Height) * CELL_HEIGHT * anchors[box * 2 + 1],
            };
        }
        #endregion BoxParsingFunctions

        #region ActivationFunctions
        private float Sigmoid(float value)
        {
            var k = (float)Math.Exp(value);

            return k / (1.0f + k);
        }


        private float[] Softmax(float[] values)
        {
            var maxVal = values.Max();
            var exp = values.Select(v => Math.Exp(v - maxVal));
            var sumExp = exp.Sum();

            return exp.Select(v => (float)(v / sumExp)).ToArray();
        }
        #endregion ActivationFunctions

        #region DimensionBoxClasses
        public class DimensionsBase
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Height { get; set; }
            public float Width { get; set; }
        }


        public class BoundingBoxDimensions : DimensionsBase { }

        class CellDimensions : DimensionsBase { }
        #endregion DimensionBoxClasses
    }
}