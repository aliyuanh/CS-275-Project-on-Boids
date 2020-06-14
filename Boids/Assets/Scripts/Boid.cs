using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
//using System.Numerics;
//using System.Numerics;
using UnityEngine;
using UnityEngine.UIElements;
using BoidVision;

public class Boid : MonoBehaviour
{
    public Vector3 velocity;
    public Pigeon boidBody;
    private float cohesionRadius = 15;
    private float separationDistance = 6;
    private Collider[] boids;
    private Vector3 cohesion;
    private Vector3 separation;
    private Vector3 alignment;
    public Vector3 toFood;
    public Vector3 seenBoids;
    public Vector3 avoidPredator;
    public Vector3 toPheromone;
    public Vector3 avoidObstacle;
    public Vector3 toLand;
    public bool perching;
    public bool landing;
    private float timeSincePerch;
    private float maxSpeed = 15;
    private float maxDistance = 45;
    private float groundHeight = -24f;
    public float timeUntilTired = 0;
    List<(string name, Vector3 ray)> objectsInScene;
    public Vector3 origin;


    // pheromones
    public BoidPheromoneRelease _pheromonePlacement;

    void Awake()
    { 
        // reference to attached release script
        _pheromonePlacement = GetComponent<BoidPheromoneRelease>();
    }
    private void Start()
    {
        timeSincePerch = 10f;
        landing = false;
        toFood = Vector3.zero;
        toPheromone = Vector3.zero;
        perching = false;
        timeUntilTired = UnityEngine.Random.Range(15f, 75f);
        objectsInScene = new List<(string name, Vector3 ray)>();
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
                    Vector3 toFood = thing.transform.position - transform.position;
                    if (toFood.magnitude < foodVec.magnitude)
                    {
                        foodVec = toFood;
                    }
                
                if((transform.position - thing.transform.position).magnitude < 4f)
                {
                    FoodScript food = thing.GetComponent<FoodScript>();
                    food.getPecked();
                    // release food pheromones when food is found
                    // tell release script that it can release pheromone
                    _pheromonePlacement.SetCanReleasePheromone(true);
                    Debug.Log("Food: set can release true");
                    _pheromonePlacement.SetPheromoneType(PheromoneTypes.Food);
                }
            }
        }
        if (foodVec.magnitude > 500)
        {
            foodVec = Vector3.zero;
        }
        return foodVec;
    }

    Vector3 avoidPredators()
    {
        Vector3 myVelocity = transform.eulerAngles;
        Collider[] possiblePredators = Physics.OverlapSphere(transform.position, cohesionRadius * 2);
        Vector3 predVec = Vector3.zero;

        if (possiblePredators == null || possiblePredators.Length == 0)
            return predVec;

        foreach (var thing in possiblePredators)
        {
            if (thing.gameObject.tag.Contains("Predator"))
            {
                Debug.Log("Predator in sphere!!!");
                //calculate angle to see if in FOV
                Vector3 targetDir = transform.position - thing.transform.position;
                predVec += targetDir.normalized / targetDir.magnitude * 10f;

                // If the predator is within a certain radius, we begin releasing fear pheromones
                if (targetDir.magnitude < 10f)
                {
                    // release food pheromones when food is found
                    // tell release script that it can release pheromone
                    _pheromonePlacement.SetCanReleasePheromone(true);
                    Debug.Log("Fear: set can release true");
                    _pheromonePlacement.SetPheromoneType(PheromoneTypes.Fear);
                }
            }
        }

        //predVec /= possiblePredators.Length;

        return predVec;
    }

    /// <summary>
    /// Called by BoidVisionClient to pass detected objects as inputs
    /// </summary>
    /// <param name="inputs"></param>
    public void ParseCV(List<BoidVisionClient.BoxOutline> inputs)
    {
        seenBoids = Vector3.zero;

        if (inputs == null || inputs.Count == 0)
            return;

        foreach(var p in inputs)
        {
            Vector3 ray = p.objectRay.direction;
            seenBoids += ray.normalized / ray.magnitude * 10f;
        }

        seenBoids /= inputs.Count;
    }
    /// <summary>
    /// Helper function that is used to calculate the velocity of the boids
    /// </summary>
    void CalculateVelocity()
    {
        toFood = findFood();
        avoidPredator = avoidPredators();
        // calculated and set in boid pheromone perception script
        // weight included
        toPheromone = toPheromone;
        velocity = Vector3.zero;
        cohesion = Vector3.zero;
        separation = Vector3.zero;
        alignment = Vector3.zero;
        boids = Physics.OverlapSphere(transform.position, cohesionRadius);
        List<Collider> myBoids = new List<Collider>();
        foreach (var boid in boids)
        {
            //Debug.Log(boid.gameObject.name);
            if (boid.gameObject.name.Contains("BoidPrefab"))
            {
                myBoids.Add(boid);
            }
        }
        cohesion = CalculateCohesion(myBoids);
        separation = CalculateSeparation(myBoids);
        alignment = CalculateAlignment(myBoids);
        //up vector ensures that the boid flies upwards after perching. 
        Vector3 upVec = new Vector3(0,1,0);
        velocity = cohesion * .2f + separation + alignment * 1.7f + toFood * .3f + toPheromone + avoidPredator * .2f + upVec + seenBoids * 1.5f;
        Vector3 land = LandingVec();
        //if the boid is landing, ignore the calculated velocity and just use the landing vector. 
        if (landing)
        {
            velocity = land;
        }
        timeSincePerch += .1f;

        //if perching, keep still
        if (perching)
        {
            velocity = Vector3.zero;
            //if the boid perches for > 5 seconds, fly away! 
            if (timeSincePerch > 5f)
            {
                perching = false;
            }
        }
        //constrain velocity here
        velocity = velocity * .8f;
    }
    //ensure that boids do not go through the ground (-24)
    void checkGround()
    {
        if(transform.position.y < groundHeight)
        {
            transform.position = new Vector3(transform.position.x, groundHeight, transform.position.z);
        }
    }

    /// <summary>
    /// Used to calculate the vector for directing the boid to the ground (for landing)
    /// </summary>
    /// <returns></returns>
    Vector3 LandingVec()
    {
        //check if close to groundHeight and velocity pointing downwards
        //also check that this boid hasn't been to the ground recently
        //if so, contribute a down vector inversely proportional to the distance from the ground
        float heightDiff = (transform.position.y - groundHeight);
        //Debug.Log(heightDiff);
        toLand = new Vector3(0, -heightDiff / 3 - 2f, 0);
        if(heightDiff < 1.0f && timeSincePerch > 20f)
        {
            //start perching if you haven't perched recently and are close to the ground 
            if(perching == false)
            {
                timeSincePerch = 0;
                perching = true;
            }
            landing = false;
        }
        //fly downwards, but also slightly along the previous velocity so not flying completely downward. 
        toLand.x = velocity.x / 2;
        toLand.z = velocity.z / 2;
        return toLand;

    }
    void Update()
    {
        if ((transform.position - origin).magnitude > maxDistance)
        {
            velocity += -transform.position.normalized * 30;
        }
        transform.position += velocity * Time.deltaTime;
        checkGround();
        //ensures that boid gets tired some time within 25 to 100 seconds and starts landing. 
        timeUntilTired -= Time.deltaTime;
        if(timeUntilTired <= 0f)
        {
            timeUntilTired = UnityEngine.Random.Range(25f, 100f);
            landing = true;
        }
        //fixed -- rotate to be along the velocity vector. Maybe slerp later to make it smoother. 
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity), Time.deltaTime);

        boidBody.flapFrequency = Mathf.Clamp(velocity.magnitude, 1.8f, 5f);
        boidBody.tailFrequency = boidBody.flapFrequency * 1.8f / 2.0f;

        // Enable these features to visualize the vectors
        //Debug.DrawRay(transform.position, alignment, Color.blue);
        //Debug.DrawRay(transform.position, separation, Color.green);
        //Debug.DrawRay(transform.position, cohesion, Color.magenta);
        //Debug.DrawRay(transform.position, velocity, Color.yellow);
        //Debug.DrawRay(transform.position, toFood, Color.cyan);
        //Debug.DrawRay(transform.position, avoidPredator, Color.red);
    }
}