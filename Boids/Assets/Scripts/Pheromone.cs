using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pheromone : MonoBehaviour
{
    float _diffusionRate;
    float _minimumConcentration;
    public float startIntensity;
    public float currentConcentration;
    public ParticleSystem particleSystem;
    public GameObject diffusionSphere;
    bool _currentVisibility;
    public PheromoneTypes type;

    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        calculateCurrentConcentration();
    }

    void calculateCurrentConcentration() {
		// concentration number of molecules (start intensity) / volume 
		float radius = diffusionSphere.transform.localScale.x;
		currentConcentration = startIntensity / (0.770f * radius * radius * radius * Mathf.PI);
	}

    public void Initialize(PheromoneTypes type, float intensity, float diameter, float diffusionRate, float minimumConcentration, Color color) {
		this.type = type;
		startIntensity = intensity;
		_diffusionRate = diffusionRate;
		_minimumConcentration = minimumConcentration;
		diffusionSphere.transform.localScale = new Vector3 (diameter, diameter, diameter);

		// max radius = (n / (3/4 * PI * c))^(1/3)
		float maxRadius = Mathf.Pow (startIntensity / (0.75f * Mathf.PI * _minimumConcentration), 1f / 3f);
		float ttl = maxRadius / _diffusionRate;

		// Initialize particle system
        var main = particleSystem.main;
		main.startLifetime = ttl;//Mathf.Pow ((0.75f * _startIntensity) / (_minimumConcentration * Mathf.PI), 1f / 3f) / diffusionRate + 3.0f;
		main.startSpeed = diffusionRate * 0.5f;
		main.startColor = color;

		ParticleSystem.EmissionModule emission = particleSystem.emission;
		emission.rate = new ParticleSystem.MinMaxCurve(0f);

		ParticleSystem.ShapeModule shape = particleSystem.shape;
		shape.radius = diameter * 0.5f;
		
		particleSystem.Emit((int)(50.0f * startIntensity));

		_currentVisibility = true;

		calculateCurrentConcentration();
	}


}
