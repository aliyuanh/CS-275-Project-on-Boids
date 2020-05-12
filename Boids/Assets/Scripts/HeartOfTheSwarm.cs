using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartOfTheSwarm : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform boidPrefab;
    public GameObject CameraController;
    public int swarmCount;
    private int maxDistance = 45;
    private Vector3 origin;
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

    // Update is called once per frame
    void Update()
    {

    }
}
