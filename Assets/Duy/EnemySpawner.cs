using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The enemy prefab to spawn")]
    public GameObject enemyPrefab;

    [Tooltip("The particle effect prefab to play when spawning")]
    public ParticleSystem spawnParticlePrefab;

    [Tooltip("Radius in which enemies can spawn when player enters")]
    public float triggerRadius = 5f;

    [Tooltip("Delay between triggering and enemy spawn")]
    public float spawnDelay = 1.5f;

    [Header("Linking Settings")]
    [Tooltip("Other spawners to trigger when this one activates")]
    public EnemySpawner[] linkedSpawners;

    [Tooltip("Delay between triggering linked spawners")]
    public float linkedSpawnDelay = 0.5f;

    [Tooltip("Layer mask for detecting player")]
    public LayerMask playerLayer;

    private bool hasSpawned = false;

    private void OnDrawGizmosSelected()
    {
        // Visualize the trigger radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // Visualize links to other spawners
        if (linkedSpawners != null)
        {
            Gizmos.color = Color.green;
            foreach (var spawner in linkedSpawners)
            {
                if (spawner != null)
                    Gizmos.DrawLine(transform.position, spawner.transform.position);
            }
        }
    }

    private void Update()
    {
        if (hasSpawned)
            return;

        // Check for player in range
        Collider[] colliders = Physics.OverlapSphere(transform.position, triggerRadius, playerLayer);
        if (colliders.Length > 0)
        {
            TriggerSpawn();
        }
    }

    public void TriggerSpawn()
    {
        if (hasSpawned)
            return;

        StartCoroutine(SpawnSequence());
        StartCoroutine(TriggerLinkedSpawners());
    }

    private IEnumerator SpawnSequence()
    {
        hasSpawned = true; // Mark as spawned

        // Play the particle effect if it exists as a child
        if (spawnParticlePrefab != null)
        {
            spawnParticlePrefab.Play(); // Play the existing particle system
        }

        // Wait for delay
        yield return new WaitForSeconds(spawnDelay);

        // Spawn enemy
        if (enemyPrefab != null)
        {
            Instantiate(enemyPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning("Enemy prefab not assigned to spawner!");
        }
    }

    private IEnumerator TriggerLinkedSpawners()
    {
        if (linkedSpawners == null)
            yield break;

        foreach (var spawner in linkedSpawners)
        {
            if (spawner != null && !spawner.hasSpawned)
            {
                yield return new WaitForSeconds(linkedSpawnDelay);
                spawner.TriggerSpawn();
            }
        }
    }
}