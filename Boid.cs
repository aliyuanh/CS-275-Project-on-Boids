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
    private Vector3 toPredator;
    private Vector3 toLand;
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


    Vector3 escapePredator()
    {
        Vector3 myVelocity = transform.eulerAngles;
        Collider[] Predators = Physics.OverlapSphere(transform.position, cohesionRadius * 2);
        Vector3 PredatorsVec = new Vector3(1000, 1000, 1000);
        foreach (var thing in Predators)
        {
            if (thing.gameObject.tag.Contains("Obstacle"))
            {
                Debug.Log("Predators in sphere!!!");
                //calculate angle to see if in FOV
                Vector3 targetDir = thing.transform.position - transform.position;
                float angle = Vector3.Angle(myVelocity, targetDir);
                angle = System.Math.Abs(angle);
                Debug.Log(angle);
                if (angle < 60)
                {
                    Debug.Log("found predators in FOV!");
                    Vector3 toObstacle = thing.transform.position - transform.position;
                    if (toPredator.magnitude < PredatorsVec.magnitude)
                    {
                        PredatorsVec = -toPredator;
                        //cohesion = Vector3.zero       Adjustable for how cohesive the flock is 
                    }
                }
            }
        }
        if (PredatorsVec.magnitude > 500)
        {
            PredatorsVec = Vector3.zero;
        }
        return PredatorsVec;
    }

    float RotateAngles(float Angles) //helper func for turning 
    {
        float angles = 0;
        if (Angles >= 0)
        {
            angles = Angles - ((int)(Angles / 360)) * 360;
        }
        else
        {
            angles = (((int)(Angles / 360)) + 1) * 360 - Angles;
        }
        return angles;
    }

    float RandomM1T1() //generate random -1 to 1
    {
        float index = Random.Range(-1, 1);
        if (index == 0)
        {
            index = RandomM1T1();
        }
        return index;
    }

    //Please use the computer vision part to get where the obstacle is
    private bool IsForward = false; //Need these four to determine whether to turn and where to turn
    private bool IsInLeft = false; 
    private bool IsInRight = false; 
    private bool IsObstacle = false;

    private Vector3 pos;// = transform.localEulerAngles;     update this on detection of obstacle

    
    Vector3 avoidObstacle()
    {
        if (IsForward)
        {
            if (IsInLeft && !IsInRight)
            {
                float rotateAnglesY = RotateAngles(pos.y + 90);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,rotateAnglesY,transform.localEulerAngles.z);
            }
            else if (!IsInLeft && IsInRight)
            {
                float rotateAnglesY = RotateAngles(pos.y - 90);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,rotateAnglesY,transform.localEulerAngles.z);
            }
            else if (IsInLeft && IsInRight)
            {
                float rotateAnglesY = RotateAngles(pos.y + 180);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,rotateAnglesY,transform.localEulerAngles.z);
            }
            else
            {
                float rotateAnglesY = RotateAngles(pos.y + RandomM1T1() * 90);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,rotateAnglesY,transform.localEulerAngles.z);
            }
        }
        else
        {
            if (!IsInLeft && !IsInRight)
            {
                if (IsObstacle)
                {
                    timer += Time.deltaTime;
                    if (timer > 0.8f)
                    {
                        timer = 0;
                        IsObstacle = false;
                    }
                }
            }
        }
    }

    Vector3 landHelp(Vector3 targetPosition)
    {
        Vector3 diff = targetPosition - transform.position;
        float dist = diff.magnitude;
        if (dist <= 0)
        {
            return Vector3.zero;
        }
        float speed = dist / 1.5f; //adjustable depending on how fast you want to land
        if (speed > maxSpeed) speed = maxSpeed;
        return diff.normalized * speed - transform.eulerAngles;
    }

    Vector3 land()
    {
        Vector3 myVelocity = transform.eulerAngles;
        Collider[] landingPoint = Physics.OverlapSphere(transform.position, cohesionRadius * 2);
        Vector3 landingVec = new Vector3(1000, 1000, 1000);
        foreach (var thing in landingPoint)
        {
            if (thing.gameObject.tag.Contains("Landing"))
            {
                Debug.Log("Landing Point in sphere!!!");
                //calculate angle to see if in FOV
                Vector3 targetDir = thing.transform.position - transform.position;
                float angle = Vector3.Angle(myVelocity, targetDir);
                angle = System.Math.Abs(angle);
                Debug.Log(angle);
                if (angle < 60)
                {
                    Debug.Log("found landing point in FOV!");
                    landingVec = landHelp(thing.transform.position);
                }
            }
        }
        if (landingVec.magnitude > 500)
        {
            landingVec = Vector3.zero;
        }
        return landingVec;
    }

    //Made by mistake, delete if you want
    /*Vector3 Wander()
    {
        Vector3 wanderVec = new Vector3(1000, 1000, 1000);
        Vector3 v = Random.onUnitSphere;
        Vector3 wanderTarget = transform.forward + v;
        wanderTarget += transform.position;
        Vector3 towander = wanderTarget - transform.position;
        if (towander.magnitude < wanderVec.magnitude)
        {
             wanderVec = towander;
        }
        return wanderVec;
    }*/

    void CalculateVelocity()
    {
        toFood = findFood();
        toPredator = escapePredator();
        toLand = land();
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
            if (boid.gameObject.name.Contains("BoidPrefab"))
            {
                myBoids.Add(boid);
            }
        }
        cohesion = CalculateCohesion(myBoids);
        // Debug.Log(cohesion);
        separation = CalculateSeparation(myBoids);
        alignment = CalculateAlignment(myBoids);
        velocity = cohesion * .2f + separation + alignment * 1.2f + toFood * .3f; // + toLand * 1f + toPredator * 1f;
    }

    void Update()
    {
        if ((transform.position - origin).magnitude > maxDistance)
        {
            velocity += -transform.position.normalized * 5;
        }
        transform.position += velocity * Time.deltaTime;
        //transform.rotation = Quaternion.LookRotation(velocity);
        //avoidObstacle();

        Debug.DrawRay(transform.position, alignment, Color.blue);
        Debug.DrawRay(transform.position, separation, Color.green);
        Debug.DrawRay(transform.position, cohesion, Color.magenta);
        Debug.DrawRay(transform.position, velocity, Color.yellow);
        Debug.DrawRay(transform.position, toFood, Color.cyan);
    }
}