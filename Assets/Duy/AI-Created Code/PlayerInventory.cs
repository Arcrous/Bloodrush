using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Place this script on the player GameObject
public class PlayerInventory : MonoBehaviour
{
    private int keyCount = 0;
    public int KeyCount { get { return keyCount; } }

    public void AddKey()
    {
        keyCount++;
        Debug.Log("Key added to inventory! Total keys: " + keyCount);
    }

    public bool UseKey()
    {
        if (keyCount > 0)
        {
            keyCount--;
            Debug.Log("Used a key. Remaining keys: " + keyCount);
            return true;
        }
        Debug.Log("No keys in inventory!");
        return false;
    }
}