using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidPheromonePerception : MonoBehaviour
{
    public Boid boid;

    float _currentSmellDistance;

    float _currentSmellAngle;

    GlobalBoidConfiguration _boidConfiguration;

    // get the closest pheromone 
    // todo: add pheromone strengths
    Pheromone _closestPheromone;

    // attract / repell flag
    bool canAttractBoid; 
    // Start is called before the first frame update
    void Awake()
    { 
        _boidConfiguration = FindObjectOfType(typeof(GlobalBoidConfiguration)) as GlobalBoidConfiguration;
    }
    void Start()
    {
        _currentSmellDistance = 0f;
    }

    // Update is called once per frame
    // updates movement based on pheromones 
    void Update()
    {
        _currentSmellAngle = Mathf.Cos(_boidConfiguration.smellAngle);
        // vector for pheromones set depending on nearest
        PheromoneTypes type = PheromoneTypes.None;
        Vector3 movementDirection = Vector3.zero;

        // check if pheromone still exists 
        if(!_closestPheromone) { 
            return;
        }
        else { 
            type = _closestPheromone.type;            
        }

        switch(type) { 
            case PheromoneTypes.Food: 
                // returns attraction vector 
                movementDirection = FindMovmementDirection(type);
                canAttractBoid = true; 
            break; 
            case PheromoneTypes.Fear: 
                // returns repellent vector
                movementDirection = FindMovmementDirection(type);
                canAttractBoid = false; 
            break;
            default: 

            break;
        }

        // if movement direction changes
        if(movementDirection.sqrMagnitude>0.0f) { 
            // set direction of the boid weight depends on movement sign
            if (canAttractBoid) 
                boid.toPheromone = movementDirection * .05f;
            else if (!canAttractBoid) 
                boid.toPheromone = movementDirection * .99f;
        }

    }

    Vector3 FindMovmementDirection(PheromoneTypes pheromoneType) {
		Pheromone pheromone = _closestPheromone;

		Vector3 movementVector = new Vector3();

		Vector3 position = transform.position;

		// check that pheromone still exists
        if(!pheromone) {
            return Vector3.zero;
        }

        Vector3 pheromonePosition = pheromone.transform.position;

        Vector3 pheromoneVector = (pheromonePosition - position).normalized;
        // find angle of pheromone
        float pheromoneAngle = Vector3.Dot(transform.forward, pheromoneVector);

        if (pheromoneAngle > _currentSmellAngle) {
            // weight added to concentration
            movementVector += pheromoneVector * pheromone.currentConcentration;
        }

		if (pheromoneType == PheromoneTypes.Fear || pheromoneType == PheromoneTypes.Pain) {
			return -movementVector;
		}

		return movementVector;
	}
    public void PheromoneEntered(Pheromone pheromone) {
        _closestPheromone = pheromone;
    }

    // TODO: make pheromone exits fade, (should leave an impression on boids)
    public void PheromoneExited(Pheromone pheromone) {
		_closestPheromone = pheromone;
	}

}
