using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartOfTheSwarm : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform boidPrefab;
    public int swarmCount;
    private int maxDistance = 25;
    void Start()
    {
        for(var i = 0; i < swarmCount; i++)
        {
            Instantiate(boidPrefab, Random.insideUnitSphere * maxDistance, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
         
    }
}
