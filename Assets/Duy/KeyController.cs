using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyController : MonoBehaviour
{
    [Header("Key Settings")]
    public float rotationSpeed = 50f;
    public float bobSpeed = 1f;
    public float bobHeight = 0.2f;

    [Header("Audio")]
    public AudioClip pickupSound;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Rotate the key
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Bob the key up and down
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Add key to player's inventory
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddKey();
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
                Destroy(gameObject);
            }
        }
    }
}