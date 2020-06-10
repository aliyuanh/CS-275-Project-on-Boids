using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodScript : MonoBehaviour
{
    // Start is called before the first frame update
    //number of "pecks" it can take from boids
    public int sizeOfFood = 20;
    public int currentSize;
    void Start()
    {
        currentSize = sizeOfFood;
    }

    // Update is called once per frame
    void Update()
    {
        if(currentSize < 0)
        {
            Destroy(gameObject);
        }
    }
    public void getPecked()
    {
        currentSize--;
    }
}
