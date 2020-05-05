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
    private float maxSpeed = 15;
    private float maxDistance = 35;

    private void Start()
    {
        InvokeRepeating("CalculateVelocity", .01f, .1f);
    }
    Vector3 CalculateCohesion(List<Collider> boids)
    {
        Vector3 currCohesion = Vector3.zero;
        int numBoids = 0;
        foreach(var boid in boids)
        {
            Vector3 diffBw = boid.transform.position - transform.position;
            if ((diffBw).magnitude > 0 && diffBw.magnitude < cohesionRadius)
            {
                currCohesion += boid.transform.position;
                numBoids++;
            }
        }
        if(numBoids > 0)
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
        foreach(var boid in boids)
        {
            Vector3 diff = transform.position - boid.transform.position;
            if (diff.magnitude <= separationDistance && diff.magnitude > .01f)
            {
                currSep = currSep - (transform.position - boid.transform.position);
                separationCount++;
            }
        }
        //if(separationCount > 0)
        //{
        //    currSep /= separationCount;
        //}
        return -currSep;
    }
    Vector3 CalculateAlignment(List<Collider> boids)
    {
        Vector3 currAlign = Vector3.zero;
        int countAlign = 0;
        foreach(var boid in boids)
        {
            Vector3 diffBw = boid.transform.position - transform.position;
            if ((diffBw).magnitude > 0 && diffBw.magnitude < cohesionRadius)
            {
                currAlign += boid.gameObject.GetComponent<Boid>().velocity;
                countAlign++;
            }
        }
        if(countAlign > 0)
        {
            currAlign /= countAlign;
        }
        return Vector3.ClampMagnitude(currAlign, maxSpeed);
    }
    void CalculateVelocity()
    {
        velocity = Vector3.zero;
        cohesion = Vector3.zero;
        separation = Vector3.zero;
        separationCount = 0;
        alignment = Vector3.zero;
        boids = Physics.OverlapSphere(transform.position, cohesionRadius);
        List<Collider> myBoids = new List<Collider>();
        foreach(var boid in boids)
        {
            //Debug.Log(boid.gameObject.name);
            if (boid.gameObject.name.Contains("BoidPrefab"))
            {
                myBoids.Add(boid);
            }
        }
        cohesion = CalculateCohesion(myBoids);
        Debug.Log(cohesion);
        separation = CalculateSeparation(myBoids);
        alignment = CalculateAlignment(myBoids);
        velocity = cohesion*.2f + separation + alignment*1.2f;
       // //Debug.Log("Number of boids: " + boids.Length);
       // foreach (var boid in myBoids)
       // {
       //     cohesion += boid.transform.position;
       //     //Debug.Log(boid.gameObject.name);
       //     //Debug.Log(boid.gameObject.GetComponentInParent<Boid>());
       //     alignment += boid.gameObject.GetComponent<Boid>().velocity;
       //     if ((transform.position - boid.transform.position).magnitude < separationDistance && (transform.position - boid.transform.position).magnitude > 1.0f)
       //     {
       //         //Debug.Log(transform.position - boid.transform.position);
       //         Vector3 sep = (transform.position - boid.transform.position);
       //         separation += new Vector3(1/sep.x, 1/sep.y, 1/sep.z);
       //         separationCount++;
       //        // Debug.Log("separation found: " + separation);
       //     }
       // }
       // //Debug.Log("Cohesion is" + cohesion);
       // cohesion = cohesion / boids.Length;
       // //Debug.Log("Cohesion after divide: " + cohesion);
       // cohesion = cohesion - transform.position;
       // //Debug.Log("Cohesion after subtract: " + cohesion);
       // cohesion = Vector3.ClampMagnitude(cohesion, maxSpeed);
       //// Debug.Log("Separation count is " + separationCount);
       // if (separationCount > 0)
       // {
       //     separation = separation / separationCount;
       //     separation = Vector3.ClampMagnitude(separation, maxSpeed);
       //    // Debug.Log("Sep count is: " + separationCount);
       // }
       // //Debug.Log("Separation is: "+separation);
       // alignment = alignment / boids.Length;
       // alignment = Vector3.ClampMagnitude(alignment, maxSpeed);
       // velocity += cohesion + separation + alignment*1.5f;
       // velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
       // //Debug.Log("calculating velocity: " + velocity.magnitude);
    }

    void Update()
    {
        if (transform.position.magnitude > maxDistance)
        {
            velocity += -transform.position.normalized * 5;
        }
        Vector3 nextPosition = transform.position + velocity * .2f;
        transform.position += velocity * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(velocity);

//        Debug.DrawRay(transform.position, alignment, Color.blue);
        Debug.DrawRay(transform.position, separation, Color.green);
        Debug.DrawRay(transform.position, cohesion, Color.magenta);
    }
}