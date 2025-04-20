using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class WeaponPickup : Pickup
    {
        [Tooltip("The prefab for the weapon that will be added to the player on pickup")]
        public WeaponController WeaponPrefab;
        [SerializeField] bool unlockUlt;


        protected override void Start()
        {
            base.Start();

            // Set all children layers to default (to prefent seeing weapons through meshes)
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t != transform)
                    t.gameObject.layer = 0;
            }
        }

        protected override void OnPicked(PlayerCharacterController byPlayer)
        {
            PlayerWeaponsManager playerWeaponsManager = byPlayer.GetComponent<PlayerWeaponsManager>();
            if (playerWeaponsManager)
            {
                if (unlockUlt)
                {
                    Debug.Log("Unlocking ultimate ability for player: ");
                    byPlayer.ultUnlocked = true;
                    //PlayPickupFeedback();
                    Destroy(gameObject);
                }
                if (playerWeaponsManager.AddWeapon(WeaponPrefab) && !unlockUlt)
                {
                    Debug.Log("Adding weapon to player: " + WeaponPrefab.name);
                    // Handle auto-switching to weapon if no weapons currently
                    if (playerWeaponsManager.GetActiveWeapon() == null || WeaponPrefab.IsSniper)
                    {
                        playerWeaponsManager.SwitchToWeaponIndex(3);
                    }

                    PlayPickupFeedback();
                    Destroy(gameObject);
                }
            }
        }
    }
}