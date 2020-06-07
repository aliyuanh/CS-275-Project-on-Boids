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
        //calculate a new velocity every tenth of a second. 
        InvokeRepeating("CalculateVelocity", .01f, .1f);
        Debug.Log(origin);
    }

    // calculate the difference between other boids and this boid. 
    // move towards the rest of the swarm if it's within the cohesion radius
    // clamp the speed so no boid accelerates too fast 
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

    //makes sure boids don't collide with each other
    //if any other boids are closer to the boid than separationDistance, they're too close. 
    //fly away from them! 
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

    //boids will try to go at ~the same speed as each other
    //find all the boids in your swarm and try to match their velocity
    //clamp speed so no boids can go above max speed   
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
    //ok this doesn't work properly but it kind of works? 
    //find all gameobjects within a large radius (cohesion radius * 2)
    //if the gameobject has the tag "food", make a food ray from me to food
    //note: later, this vector will be given by CV 
    //find the angle between my current rotation and food
    //if the food is closer than some angle (60 degrees) then it's viewable in the FOV
    //set the food vector only if it's viewable and return it 
    //if no food is found, return 0
    Vector3 findFood()
    {
        Vector3 myVelocity = transform.eulerAngles;
        Collider[] possibleFoods = Physics.OverlapSphere(transform.position, cohesionRadius * 2);
        Vector3 foodVec = new Vector3(1000, 1000, 1000);
        foreach (var thing in possibleFoods)
        {
            if (thing.gameObject.tag.Contains("Food"))
            {
                //Debug.Log("Food in sphere!!!");
                //calculate angle to see if in FOV
                Vector3 targetDir = thing.transform.position - transform.position;
                float angle = Vector3.Angle(myVelocity, targetDir);
                angle = System.Math.Abs(angle);
               // Debug.Log(angle);
                if (angle < 60)
                {
                    //Debug.Log("found food in FOV!");
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
    //calculate velocity based on the rules of cohesion, separation, and alignment 
    void CalculateVelocity()
    {
        toFood = findFood();
        velocity = Vector3.zero;
        cohesion = Vector3.zero;
        separation = Vector3.zero;
        separationCount = 0;
        alignment = Vector3.zero;
        //only consider boids that are near me
        boids = Physics.OverlapSphere(transform.position, cohesionRadius);
        List<Collider> myBoids = new List<Collider>();
        //only collect boids
        foreach (var boid in boids)
        {
            if (boid.gameObject.name.Contains("Boid"))
            {
                myBoids.Add(boid);
            }
        }
        cohesion = CalculateCohesion(myBoids);
        separation = CalculateSeparation(myBoids);
        alignment = CalculateAlignment(myBoids);
        //this equation sets the velocity with different weights given to cohesion, separation, alignment, and food
        //note: changing the weight of any of these vectors will change the speed + behavior of the boids
        velocity = cohesion * .2f + separation + alignment * 1.2f + toFood * .3f;
    }

    //move every frame, restricting the boids to a sphere around their origin
    void Update()
    {
        //boids only move quickly when they are inside a sphere near the origin. 
        //if they move out of the sphere, they will slow down + rotate to be in the sphere
        if ((transform.position - origin).magnitude > maxDistance)
        {
            velocity += -transform.position.normalized * 5;
        }
        transform.position += velocity * Time.deltaTime;
        //NOTE: this rotation line turns the boids to match their velocity vector.
        //for some reason, adding the food vector broke this (fix later!)

        //transform.rotation = Quaternion.LookRotation(velocity);

        //draw rays of each important vector to each boid. 
        Debug.DrawRay(transform.position, alignment, Color.blue);
        Debug.DrawRay(transform.position, separation, Color.green);
        Debug.DrawRay(transform.position, cohesion, Color.magenta);
        Debug.DrawRay(transform.position, velocity, Color.yellow);
        Debug.DrawRay(transform.position, toFood, Color.cyan);
    }
}