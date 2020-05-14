using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartOfTheSwarm : MonoBehaviour
{
    public Transform boidPrefab;
    public GameObject CameraController;
    public int swarmCount;
    private int maxDistance = 45;
    private Vector3 origin;

    //create boids (of # swarmCount) in a sphere randomly relative to the origin. 
    //also, create the camera controller. 
    void Start()
    {
        origin = transform.position;
        for (var i = 0; i < swarmCount; i++)
        {
            var myBoid = Instantiate(boidPrefab, origin + Random.insideUnitSphere * maxDistance, Quaternion.identity);
            myBoid.gameObject.GetComponent<Boid>().origin = origin;
            myBoid.name = "Boid" + i.ToString();
        }
        Instantiate(CameraController, origin, Quaternion.identity);
    }

}
