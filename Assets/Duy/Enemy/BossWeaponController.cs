using System;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    public class BossWeaponController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The boss that owns this weapon")]
        public GameObject Owner;

        [Tooltip("The point where projectiles are spawned")]
        public Transform MuzzlePoint;

        [Tooltip("Optional weapon animator")]
        public Animator WeaponAnimator;

        [Header("Projectile Settings")]
        [Tooltip("The projectile prefab to fire")]
        public BossProjectile ProjectilePrefab;

        [Tooltip("Spread angle for projectiles (0 for perfect accuracy)")]
        public float SpreadAngle = 0f;

        [Tooltip("Number of projectiles to fire per shot")]
        public int ProjectilesPerShot = 1;

        [Header("Melee Attack Settings")]
        [Tooltip("Is this a melee weapon?")]
        public bool IsMelee = false;

        [Tooltip("Melee damage amount")]
        public float MeleeDamage = 40f;

        [Tooltip("Melee attack range")]
        public float MeleeRange = 3f;

        [Tooltip("Melee attack angle (for wide swings)")]
        public float MeleeAngle = 90f;

        [Header("Effects")]
        [Tooltip("VFX prefab for muzzle flash")]
        public GameObject MuzzleFlashPrefab;

        [Tooltip("Sound played when firing")]
        public AudioClip FireSfx;

        [Tooltip("Sound played for melee attacks")]
        public AudioClip MeleeSfx;

        [Header("Animation")]
        [Tooltip("Animation trigger parameter for shooting")]
        public string ShootAnimTrigger = "Fire";

        [Tooltip("Animation trigger parameter for melee attack")]
        public string MeleeAnimTrigger = "Melee";

        // Events
        public UnityAction OnShoot;
        public UnityAction OnMeleeAttack;

        // Internal variables
        private BossController m_BossController;
        private AudioSource m_AudioSource;

        private void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.spatialBlend = 1.0f;
                m_AudioSource.playOnAwake = false;
            }

            m_BossController = Owner.GetComponent<BossController>();
        }

        public void Shoot()
        {
            if (ProjectilePrefab == null || MuzzlePoint == null)
            {
                Debug.LogError("Missing projectile prefab or muzzle point!");
                return;
            }

            // Play firing animation if available
            if (WeaponAnimator != null)
            {
                WeaponAnimator.SetTrigger(ShootAnimTrigger);
            }

            // Play sound effect
            if (FireSfx != null)
            {
                m_AudioSource.PlayOneShot(FireSfx);
            }

            // Spawn muzzle flash
            if (MuzzleFlashPrefab != null)
            {
                GameObject muzzleFlash = Instantiate(MuzzleFlashPrefab, MuzzlePoint.position, MuzzlePoint.rotation);
                Destroy(muzzleFlash, 2f);
            }

            // Fire projectiles
            for (int i = 0; i < ProjectilesPerShot; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithSpread();
                BossProjectile projectile = Instantiate(ProjectilePrefab, MuzzlePoint.position, Quaternion.LookRotation(shotDirection));
                projectile.Skillshot(Owner);
            }

            // Invoke event
            OnShoot?.Invoke();
        }

        public void MeleeAttack(Vector3 targetPosition)
        {
            if (!IsMelee)
            {
                Debug.LogWarning("MeleeAttack called on a non-melee weapon!");
                return;
            }

            // Play melee animation if available
            if (WeaponAnimator != null)
            {
                WeaponAnimator.SetTrigger(MeleeAnimTrigger);
            }

            // Play sound effect
            if (MeleeSfx != null)
            {
                m_AudioSource.PlayOneShot(MeleeSfx);
            }

            // Get all potential targets in range
            Collider[] hits = Physics.OverlapSphere(transform.position, MeleeRange, LayerMask.GetMask("Player"));

            // Check each target
            foreach (var hit in hits)
            {
                Debug.Log($"Melee hit: {hit.name}");
                // Skip the boss itself
                if (hit.transform.IsChildOf(Owner.transform) || hit.gameObject == Owner)
                    continue;

                // Check if the hit is in the attack angle
                Vector3 directionToTarget = (hit.transform.position - transform.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                if (angleToTarget <= MeleeAngle * 0.5f)
                {
                    // Apply damage
                    Damageable damageable = hit.GetComponent<Damageable>();
                    if (damageable != null)
                    {
                        Debug.Log($"Melee hit: {hit.name} for {MeleeDamage} damage.");
                        damageable.InflictDamage(MeleeDamage, false, Owner);
                    }
                }
            }

            // Invoke event
            OnMeleeAttack?.Invoke();
        }

        private Vector3 GetShotDirectionWithSpread()
        {
            if (SpreadAngle <= 0f)
            {
                return MuzzlePoint.forward;
            }

            float spreadAngleRatio = SpreadAngle / 180f;
            Vector3 spreadDirection = Vector3.Slerp(MuzzlePoint.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);
            return spreadDirection.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (IsMelee)
            {
                // Draw melee range
                Gizmos.color = Color.red * 0.7f;
                Gizmos.DrawWireSphere(transform.position, MeleeRange);

                // Draw melee angle
                Vector3 rightDir = Quaternion.Euler(0, MeleeAngle * 0.5f, 0) * transform.forward;
                Vector3 leftDir = Quaternion.Euler(0, -MeleeAngle * 0.5f, 0) * transform.forward;

                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, rightDir * MeleeRange);
                Gizmos.DrawRay(transform.position, leftDir * MeleeRange);
            }
        }
    }
}