using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Place this script on the door GameObject
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public bool isLocked = true;
    public int requiredKeys = 1;  // Number of keys required to unlock the door
    public float slideDistance = 3f;
    public float openSpeed = 2f;

    [Header("UI Elements")]
    public Canvas keyRequirementCanvas;
    public TextMeshProUGUI keyRequirementText;
    public float textFlashDuration = 1.5f;
    public Color insufficientKeyColor = Color.red;

    [Header("Audio")]
    public AudioClip lockedSound;
    public AudioClip unlockSound;
    public AudioClip openSound;

    private AudioSource audioSource;
    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector3 initialPosition;
    private Color originalTextColor;
    private Coroutine flashTextCoroutine;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        initialPosition = transform.position;

        // Initialize UI
        if (keyRequirementCanvas != null && keyRequirementText != null)
        {
            originalTextColor = keyRequirementText.color;
            UpdateKeyRequirementText();
        }
        else
        {
            Debug.LogWarning("Key requirement UI elements not assigned on " + gameObject.name);
        }
    }

    void Update()
    {
        // Make UI face the camera
        /*  if (keyRequirementCanvas != null && keyRequirementCanvas.gameObject.activeSelf)
         {
             keyRequirementCanvas.transform.rotation = Quaternion.LookRotation(
                 keyRequirementCanvas.transform.position - Camera.main.transform.position
             );
         } */
    }

    public void Interact(PlayerInventory playerInventory)
    {
        if (isAnimating) return;

        if (isLocked)
        {
            if (playerInventory.KeyCount >= requiredKeys)
            {
                // Player has enough keys
                Unlock(playerInventory);
                StartCoroutine(AnimateDoor(slideDistance, openSpeed));
                PlaySound(openSound);
            }
            else
            {
                // Not enough keys
                PlaySound(lockedSound);
                Debug.Log($"You need {requiredKeys} keys to unlock this door. You only have {playerInventory.KeyCount}.");

                // Flash text red
                if (keyRequirementText != null)
                {
                    if (flashTextCoroutine != null)
                    {
                        StopCoroutine(flashTextCoroutine);
                    }
                    flashTextCoroutine = StartCoroutine(FlashText());
                }
            }
        }
        else
        {
            // Door is unlocked
            if (!isOpen)
            {
                StartCoroutine(AnimateDoor(slideDistance, openSpeed));
                PlaySound(openSound);
            }
            else
            {
                StartCoroutine(AnimateDoor(0f, openSpeed));
                PlaySound(openSound);
            }
        }
    }

    public void Unlock(PlayerInventory playerInventory)
    {
        if (isLocked)
        {
            // Use the required number of keys
            for (int i = 0; i < requiredKeys; i++)
            {
                playerInventory.UseKey();
            }

            isLocked = false;
            PlaySound(unlockSound);
            Debug.Log("You unlocked the door!");

            // Hide key requirement text when unlocked
            if (keyRequirementCanvas != null)
            {
                keyRequirementCanvas.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator AnimateDoor(float targetHeight, float speed)
    {
        isAnimating = true;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = initialPosition + Vector3.up * targetHeight;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime);
            elapsedTime += Time.deltaTime * speed;
            yield return null;
        }

        transform.position = targetPosition;
        isOpen = !isOpen;
        isAnimating = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void UpdateKeyRequirementText()
    {
        if (keyRequirementText != null && isLocked)
        {
            keyRequirementText.text = $"Press F to interact \n Required: {requiredKeys} Key{(requiredKeys > 1 ? "s" : "")}";
            keyRequirementCanvas.gameObject.SetActive(true);
        }
        else if (keyRequirementCanvas != null && !isLocked)
        {
            keyRequirementCanvas.gameObject.SetActive(false);
        }
    }

    private IEnumerator FlashText()
    {
        keyRequirementText.color = insufficientKeyColor;
        yield return new WaitForSeconds(textFlashDuration);
        keyRequirementText.color = originalTextColor;
    }

    // Call this if the required keys change during gameplay
    public void SetRequiredKeys(int newKeyCount)
    {
        requiredKeys = newKeyCount;
        UpdateKeyRequirementText();
    }
}