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

        protected override void Start()
        {
            base.Start();
            if (unlockUlt)
            {
                ultGaugeUI = GameObject.Find("UltGauge");
                ultGaugeUI.SetActive(false);
            }
        }

        protected override void OnPicked(PlayerCharacterController byPlayer)
        {
            if (unlockPowershot)
            {
                byPlayer.powershotUnlocked = true;
                EventManager.Broadcast(new AbilityPickupEvent("Power Shot. Press Q to use, cost HP to use"));
            }

            if (unlockBindingShot)
            {
                byPlayer.bindingShotUnlocked = true;
                EventManager.Broadcast(new AbilityPickupEvent("Binding Shot. Press E to use, cost HP to use"));
            }

            if (unlockUlt)
            {
                byPlayer.ultUnlocked = true;
                ultGaugeUI.SetActive(true);
                EventManager.Broadcast(new AbilityPickupEvent("Ultimate. Press R to use, cost full ult gauge to use"));
            }

            PlayPickupFeedback();
            Destroy(gameObject);
        }
    }
}