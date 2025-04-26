using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactionDistance = 2f;
    public LayerMask interactableLayers;

    private PlayerInventory inventory;

    void Start()
    {
        inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = gameObject.AddComponent<PlayerInventory>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * interactionDistance, Color.red);
            // Cast a ray forward to check for interactable objects
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, interactionDistance, interactableLayers))
            {
                // Check if we hit a door
                DoorController door = hit.collider.GetComponent<DoorController>();
                if (door != null)
                {
                    door.Interact(inventory);
                }
                else
                {
                    Debug.Log("Nothing");
                }
            }
        }
    }
}