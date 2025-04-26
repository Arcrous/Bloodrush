using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.FPS.Gameplay;

namespace Unity.FPS.UI
{
    public class UltGaugeUI : MonoBehaviour
    {
        [Tooltip("Image component displaying the ult gauge fill")]
        public Image ultGaugeFill;

        [Tooltip("Color when ult is ready")]
        public Color readyColor = Color.magenta;

        [Tooltip("Default color of the ult gauge")]
        public Color defaultColor = Color.yellow;

        PlayerCharacterController m_PlayerCharacterController;

        void Start()
        {
            // Find the player character controller
            m_PlayerCharacterController = FindObjectOfType<PlayerCharacterController>();
        }

        void Update()
        {
            if (m_PlayerCharacterController)
            {
                // Update fill amount (0 to 1)
                ultGaugeFill.fillAmount = m_PlayerCharacterController.ultGauge / 100f;

                // Change color when ult is ready
                ultGaugeFill.color = m_PlayerCharacterController.ultGauge >= 100f ? readyColor : defaultColor;
            }
        }
    }
}
