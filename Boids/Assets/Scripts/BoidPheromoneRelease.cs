using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// manages pheromone release
[RequireComponent (typeof(GlobalPheromoneConfiguration))]

public class BoidPheromoneRelease : MonoBehaviour
{
    GlobalBoidConfiguration _boidConfiguration;
    GlobalPheromoneConfiguration _pheromoneConfiguration;
    // stores a reference to the prefab
    public GameObject pheromonePrefab;
    // pheromone particle system (visual)
    ParticleSystem _particleSystem;

    // determines whether or not to release a pheromone
    bool _canReleasePheromone; 
    // determines type of pheromone to relase 
    PheromoneTypes _type;

    // measure release rate
    float _placementTimer;

    void Awake() {
        _pheromoneConfiguration = GetComponent<GlobalPheromoneConfiguration> ();
        _type = PheromoneTypes.None;
        _canReleasePheromone = false;
        _placementTimer = 0.0f;
    }


    void Update() { 
        // TODO: get pheromones to be placed in trail
        // TODO: couldn't get timer to work yet
        // _placementTimer = _placementTimer + .05f;
        // determines when to release pheromone if pheromone type is set
        // bool timer = (placementTimer >= _boidConfiguration.pheromonePlacementRate);
		if (_canReleasePheromone && _type != PheromoneTypes.None) {
            // if pheromone is set to a type, then place it
            Vector3 pheromonePosition = new Vector3 (transform.position.x, transform.position.y, transform.position.z);
            pheromonePosition = pheromonePosition - transform.forward;

            PlacePheromone(pheromonePosition, _type);
            // after it's placed, mark pheromone as released, can't release pheromone anymore
            // _placementTimer = 0.0f;
            _canReleasePheromone = false; 
		}

    }
    // call to place specific type of pheromone
    public void PlacePheromone(Vector3 position, PheromoneTypes type) { 
        PheromoneConfiguration pheromoneConfiguration = _pheromoneConfiguration.configs[type];

        float initialIntensity = pheromoneConfiguration.initialIntensity;
        float initialScale = pheromoneConfiguration.initialRadius;
        float diffusionRate = pheromoneConfiguration.diffusionRate;
        float minimumConcentration = pheromoneConfiguration.minimumConcentration;
        Color color = pheromoneConfiguration.color;
        // instantiates the pheromone prefab in position
        GameObject newPheromone = Instantiate(pheromonePrefab, position, new Quaternion ()) as GameObject;
        // initialize and visualizes new pheromone gameobject through pheromone script
        newPheromone.GetComponent<Pheromone>().Initialize(type, initialIntensity, initialScale, diffusionRate, minimumConcentration, color);
    }

    public void SetPheromoneType(PheromoneTypes type) { 
        _type = type; 
    }

    public void SetCanReleasePheromone(bool releasePheromone) { 
        _canReleasePheromone = releasePheromone;
    }

}
