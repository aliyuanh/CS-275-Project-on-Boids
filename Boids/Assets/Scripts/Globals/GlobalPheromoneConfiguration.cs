using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// config for pheromone types
public class GlobalPheromoneConfiguration : MonoBehaviour {
	// Dictionary that holds one pheromone configuration per pheromone type.
	public Dictionary<PheromoneTypes, PheromoneConfiguration> configs = new Dictionary<PheromoneTypes, PheromoneConfiguration>()
	{
		{ PheromoneTypes.Food, new PheromoneConfiguration(Color.green) },
		{ PheromoneTypes.Pain, new PheromoneConfiguration(Color.red) },
		{ PheromoneTypes.Fear, new PheromoneConfiguration(Color.blue) },
		{ PheromoneTypes.None, null}
	};
}

public class PheromoneConfiguration {
	public PheromoneConfiguration(Color color, float initialIntensity = 1.7809f, float diffusionRate = 0.2f, float minimumConcentration = 0.02f) {
		this.color = color;
		this.initialIntensity = initialIntensity;
		this.diffusionRate = diffusionRate;
		this.minimumConcentration = minimumConcentration;
	}
	public float initialIntensity;
	public float initialRadius = 1.0f;
	public float diffusionRate;
	public float minimumConcentration;
	public Color color;
	public bool particleSystemVisible = true;
}