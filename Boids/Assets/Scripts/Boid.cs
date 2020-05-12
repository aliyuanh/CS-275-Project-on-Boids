using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Boid : MonoBehaviour
{
    public Vector3 velocity;

    private float cohesionRadius = 15;
    private float separationDistance = 6;
    private Collider[] boids;
    private Vector3 cohesion;
    private Vector3 separation;
    private int separationCount;
    private Vector3 alignment;
    private Vector3 toFood;
    private float maxSpeed = 15;
    private float maxDistance = 45;
    public Vector3 origin;
    private void Start()
    {
        InvokeRepeating("CalculateVelocity", .01f, .1f);
        Debug.Log(origin);
    }
    Vector3 CalculateCohesion(List<Collider> boids)
    {
        Vector3 currCohesion = Vector3.zero;
        int numBoids = 0;
        foreach (var boid in boids)
        {
            Vector3 diffBw = boid.transform.position - transform.position;
            if ((diffBw).magnitude > 0 && diffBw.magnitude < cohesionRadius)
            {
                currCohesion += boid.transform.position;
                numBoids++;
            }
        }
        if (numBoids > 0)
        {
            currCohesion /= numBoids;
        }
        currCohesion -= transform.position;
        return Vector3.ClampMagnitude(currCohesion, maxSpeed);

    }
    Vector3 CalculateSeparation(List<Collider> boids)
    {
        Vector3 currSep = Vector3.zero;
        int separationCount = 0;
        foreach (var boid in boids)
        {
            Vector3 diff = transform.position - boid.transform.position;
            if (diff.magnitude <= separationDistance && diff.magnitude > .01f)
            {
                currSep = currSep - (transform.position - boid.transform.position);
                separationCount++;
            }
        }
        return -currSep;
    }
    Vector3 CalculateAlignment(List<Collider> boids)
    {
        Vector3 currAlign = Vector3.zero;
        int countAlign = 0;
        foreach (var boid in boids)
        {
            Vector3 diffBw = boid.transform.position - transform.position;
            if ((diffBw).magnitude > 0 && diffBw.magnitude < cohesionRadius)
            {
                currAlign += boid.gameObject.GetComponent<Boid>().velocity;
                countAlign++;
            }
        }
        if (countAlign > 0)
        {
            currAlign /= countAlign;
        }
        return Vector3.ClampMagnitude(currAlign, maxSpeed);
    }
    Vector3 findFood()
    {
        Vector3 myVelocity = transform.eulerAngles;
        Collider[] possibleFoods = Physics.OverlapSphere(transform.position, cohesionRadius * 2);
        Vector3 foodVec = new Vector3(1000, 1000, 1000);
        foreach (var thing in possibleFoods)
        {
            if (thing.gameObject.tag.Contains("Food"))
            {
                Debug.Log("Food in sphere!!!");
                //calculate angle to see if in FOV
                Vector3 targetDir = thing.transform.position - transform.position;
                float angle = Vector3.Angle(myVelocity, targetDir);
                angle = System.Math.Abs(angle);
                Debug.Log(angle);
                if (angle < 60)
                {
                    Debug.Log("found food in FOV!");
                    Vector3 toFood = thing.transform.position - transform.position;
                    if (toFood.magnitude < foodVec.magnitude)
                    {
                        foodVec = toFood;
                    }
                }
            }
        }
        if (foodVec.magnitude > 500)
        {
            foodVec = Vector3.zero;
        }
        return foodVec;
    }
    void CalculateVelocity()
    {
        toFood = findFood();
        velocity = Vector3.zero;
        cohesion = Vector3.zero;
        separation = Vector3.zero;
        separationCount = 0;
        alignment = Vector3.zero;
        boids = Physics.OverlapSphere(transform.position, cohesionRadius);
        List<Collider> myBoids = new List<Collider>();
        foreach (var boid in boids)
        {
            //Debug.Log(boid.gameObject.name);
            if (boid.gameObject.name.Contains("Boid"))
            {
                myBoids.Add(boid);
            }
        }
        cohesion = CalculateCohesion(myBoids);
        // Debug.Log(cohesion);
        separation = CalculateSeparation(myBoids);
        alignment = CalculateAlignment(myBoids);
        velocity = cohesion * .2f + separation + alignment * 1.2f + toFood * .3f;
    }

    void Update()
    {
        if ((transform.position - origin).magnitude > maxDistance)
        {
            velocity += -transform.position.normalized * 5;
        }
        transform.position += velocity * Time.deltaTime;
        //transform.rotation = Quaternion.LookRotation(velocity);

        Debug.DrawRay(transform.position, alignment, Color.blue);
        Debug.DrawRay(transform.position, separation, Color.green);
        Debug.DrawRay(transform.position, cohesion, Color.magenta);
        Debug.DrawRay(transform.position, velocity, Color.yellow);
        Debug.DrawRay(transform.position, toFood, Color.cyan);
    }
}