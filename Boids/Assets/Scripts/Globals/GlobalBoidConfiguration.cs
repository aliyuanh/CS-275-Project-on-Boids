
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalBoidConfiguration : MonoBehaviour
{
    // how far they can smell
    public float smellDistance = 2.0f;

	// in radians
	public float smellAngle = 70.0f * Mathf.Deg2Rad;
	public float pheromonePlacementRate = 0.3f;
}
