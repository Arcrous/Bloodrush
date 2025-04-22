using UnityEngine;
using Unity.FPS.Game;
using System.Collections.Generic;

namespace Unity.FPS.AI
{
    public class BossProjectile : ProjectileBase
    {
        [Header("Boss Projectile Settings")]
        [Tooltip("Radius of this projectile's collision detection")]
        public float Radius = 0.5f;

        [Tooltip("Maximum lifetime of the projectile")]
        public float MaxLifeTime = 5f;

        [Tooltip("Projectile speed")]
        public float Speed = 15f;

        [Tooltip("Damage amount caused by the projectile")]
        public float Damage = 25f;

        [Tooltip("VFX prefab to spawn on impact")]
        public GameObject ImpactVfx;

        [Tooltip("Sound played on impact")]
        public AudioClip ImpactSfx;

        [Tooltip("Layers this projectile can collide with")]
        public LayerMask HittableLayers = -1;

        // Internal variables
        private Vector3 m_LastPosition;
        private List<Collider> m_IgnoredColliders;
        private const QueryTriggerInteraction k_TriggerInteraction = QueryTriggerInteraction.Collide;
        private Transform m_ProjectileTransform;

        private void OnEnable()
        {
            m_ProjectileTransform = transform;
            Destroy(gameObject, MaxLifeTime);
        }

        private void Start()
        {
            m_LastPosition = m_ProjectileTransform.position;
            m_IgnoredColliders = new List<Collider>();

            // Ignore colliders from the owner (the boss)
            if (Owner != null)
            {
                Collider[] ownerColliders = Owner.GetComponentsInChildren<Collider>();
                m_IgnoredColliders.AddRange(ownerColliders);
            }
        }

        private void Update()
        {
            // Move projectile
            Vector3 moveDirection = m_ProjectileTransform.forward;
            Vector3 moveStep = moveDirection * Speed * Time.deltaTime;
            m_ProjectileTransform.position += moveStep;

            // Check for collisions
            Vector3 currentPosition = m_ProjectileTransform.position;
            Vector3 displacementSinceLastFrame = currentPosition - m_LastPosition;

            // Perform spherecast to detect collisions
            RaycastHit[] hits = Physics.SphereCastAll(m_LastPosition, Radius,
                displacementSinceLastFrame.normalized, displacementSinceLastFrame.magnitude,
                HittableLayers, k_TriggerInteraction);

            // Sort hits by distance
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Process the closest valid hit
            foreach (var hit in hits)
            {
                if (IsValidHit(hit))
                {
                    OnHit(hit.point, hit.normal, hit.collider);
                    return;
                }
            }

            m_LastPosition = currentPosition;
        }

        private bool IsValidHit(RaycastHit hit)
        {
            // Ignore hits with ignored colliders (like the boss itself)
            if (m_IgnoredColliders.Contains(hit.collider))
            {
                return false;
            }

            // Ignore hits with an ignore component
            if (hit.collider.GetComponent<IgnoreHitDetection>())
            {
                return false;
            }

            // Ignore triggers that don't have damageable component
            if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
            {
                return false;
            }

            return true;
        }

        private void OnHit(Vector3 point, Vector3 normal, Collider collider)
        {
            // Apply damage
            Damageable damageable = collider.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.InflictDamage(Damage, false, Owner);
            }

            // Spawn impact VFX
            if (ImpactVfx != null)
            {
                GameObject impactVfxInstance = Instantiate(ImpactVfx,
                    point + (normal * 0.1f), // Small offset to prevent clipping
                    Quaternion.LookRotation(normal));

                Destroy(impactVfxInstance, 5f);
            }

            // Play impact sound
            if (ImpactSfx != null)
            {
                AudioUtility.CreateSFX(ImpactSfx, point, AudioUtility.AudioGroups.Impact, 1f, 3f);
            }

            // Destroy the projectile
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red * 0.4f;
            Gizmos.DrawSphere(transform.position, Radius);
        }
    }
}