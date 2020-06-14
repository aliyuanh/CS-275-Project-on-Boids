using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PheromoneSensor : MonoBehaviour
{
    [SerializeField]
	BoidPheromonePerception _pheromoneBehaviour;

    // checks if there is a pheromone nearby
	void OnTriggerEnter(Collider collider) {
		Debug.Log("enter pheromone cloud");
		Pheromone pheromone = collider.GetComponentInParent<Pheromone>();
		if (pheromone) {
			_pheromoneBehaviour.PheromoneEntered(pheromone);
		}
	}

	void OnTriggerExit(Collider collider) {
		Debug.Log("exit pheromone cloud");
		Pheromone pheromone = collider.GetComponentInParent<Pheromone>();
		if(pheromone) {
			_pheromoneBehaviour.PheromoneExited(pheromone);
		}
	}
}
