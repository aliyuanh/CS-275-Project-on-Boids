using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredatorScript : MonoBehaviour
{
    [SerializeField]
    float DetectionRadius = 20.0f;

    [SerializeField]
    float PredatorSpeed = 5.0f;

    [SerializeField]
    float EatDistance = 0.5f;

    // Update is called once per frame
    void Update()
    {
        Vector3 targetDir = Vector3.zero;
        Collider[] possiblePredators = Physics.OverlapSphere(transform.position, DetectionRadius * 2);
        foreach (var thing in possiblePredators)
        {
            if (thing.gameObject.tag.Contains("Boid"))
            {
                Vector3 temp = thing.transform.position - transform.position;

                // If the predator is too close to the boid, it will eat the boid, thereby destroying it
                if (temp.magnitude < EatDistance) {

                    // Boid releases pain pheromone before it has been eaten
                    BoidPheromoneRelease releaser = thing.gameObject.GetComponent<Boid>()._pheromonePlacement;
                    releaser.SetCanReleasePheromone(true);
                    releaser.SetPheromoneType(PheromoneTypes.Pain);

                    // Waits one second to release pheromones before it is destroyed
                    Destroy(thing.gameObject, 1);
                    continue;
                }

                // Inversely weight by distance
                targetDir += temp.normalized / temp.magnitude * 10.0f;
            }
        }

        if (possiblePredators.Length > 0) {
            targetDir /= possiblePredators.Length;
        }

        transform.position += PredatorSpeed * targetDir * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime);

        Debug.DrawRay(transform.position, targetDir, Color.red);
    }
}
