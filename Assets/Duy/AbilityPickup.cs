using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class AbilityPickup : Pickup
    {
        [Tooltip("Whether this pickup unlocks powershot ability")]
        public bool unlockPowershot = false;

        [Tooltip("Whether this pickup unlocks binding shot ability")]
        public bool unlockBindingShot = false;

        [Tooltip("Whether this pickup unlocks ultimate ability")]
        public bool unlockUlt = false;
        [SerializeField] GameObject ultGaugeUI;

        protected override void OnPicked(PlayerCharacterController byPlayer)
        {
            if (unlockPowershot)
            {
                byPlayer.powershotUnlocked = true;
            }

            if (unlockBindingShot)
            {
                byPlayer.bindingShotUnlocked = true;
            }

            if (unlockUlt)
            {
                byPlayer.ultUnlocked = true;
                ultGaugeUI.SetActive(true);
            }

            PlayPickupFeedback();
            Destroy(gameObject);
        }
    }
}