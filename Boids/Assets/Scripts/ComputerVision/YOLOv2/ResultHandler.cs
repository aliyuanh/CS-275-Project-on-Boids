using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;

namespace YOLO
{
    public class ResultHandler : MonoBehaviour
    {
        public delegate IEnumerator ModelHandler(Tensor modelOutput, float threshold = .3F);
        public ModelHandler handleOutput;
    }
}