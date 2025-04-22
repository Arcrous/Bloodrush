using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class BossController : EnemyController
    {
        public enum BossAttackType
        {
            Melee,
            Ranged,
            SpawnEnemies
        }
        [Header("Boss Parameters")]
        [Tooltip("Whether the boss is currently active")]
        public bool IsActive = false;

        [Tooltip("The trigger collider that activates the boss when player enters")]
        public Collider BossRoomTrigger;

        [Tooltip("The melee attack range")]
        public float MeleeAttackRange = 3f;

        [Tooltip("The ranged attack range")]
        public float RangedAttackRange = 15f;

        [Tooltip("The enemy prefab to spawn")]
        public GameObject[] EnemyPrefabsToSpawn;

        [Tooltip("The maximum number of enemies that can be spawned at once")]
        public int MaxSpawnedEnemies = 5;

        [Tooltip("Positions where enemies can be spawned")]
        public Transform[] SpawnPositions;

        [Tooltip("Cooldown between attacks")]
        public float AttackCooldown = 3f;

        [Tooltip("Duration of the spawn animation")]
        public float SpawnAnimationDuration = 2f;

        [Header("Boss Weapons")]
        [Tooltip("Reference to the melee weapon controller")]
        public BossWeaponController MeleeWeapon;

        [Tooltip("Reference to the ranged weapon controller")]
        public BossWeaponController RangedWeapon;
        public BossWeaponController RangedWeapon2;

        [Header("Boss VFX")]
        [Tooltip("VFX played when boss is activated")]
        public ParticleSystem BossActivationVfx;

        [Tooltip("VFX played when boss spawns enemies")]
        public ParticleSystem SpawnEnemiesVfx;

        [Header("Boss Animations")]
        [Tooltip("Animator component for the boss")]
        public Animator Animator;
        [Tooltip("Animation for spawning enemies")]
        public string SpawnAnimationTrigger = "SpawnEnemies";
        [Tooltip("Animation trigger parameter for melee attack")]
        public string MeleeAttackTrigger = "MeleeAttack";

        [Tooltip("Animation trigger parameter for ranged attack")]
        public string RangedAttackTrigger = "RangedAttack";

        [Tooltip("Animation trigger parameter for dash")]
        public string DashTrigger = "Dash";

        [Header("Dash Parameters")]
        [Tooltip("Minimum distance the boss wants to keep from target during ranged attacks")]
        public float MinRangedAttackDistance = 10f;

        [Tooltip("Force applied when dashing backwards")]
        public float DashForce = 30f;

        [Tooltip("Duration of the dash")]
        public float DashDuration = 0.5f;

        [Tooltip("Layer mask for dash obstacle detection")]
        public LayerMask DashObstacleLayers = Physics.DefaultRaycastLayers;

        public UnityAction onBossActivated;
        public UnityAction<BossAttackType> onBossAttack;

        private List<GameObject> m_SpawnedEnemies = new List<GameObject>();
        private BossAttackType m_CurrentAttackType;
        private float m_LastAttackTime = float.NegativeInfinity;
        private bool m_IsAttacking = false;
        private bool m_WasActivated = false;
        private Rigidbody m_Rigidbody;

        protected override void Start()
        {
            base.Start();

            m_Rigidbody = GetComponent<Rigidbody>();

            // Setup boss room trigger
            if (BossRoomTrigger != null)
            {
                // Make sure it's a trigger
                BossRoomTrigger.isTrigger = true;

                // Add a trigger component if it doesn't exist
                var triggerComponent = BossRoomTrigger.gameObject.GetComponent<BossRoomTrigger>();
                if (triggerComponent == null)
                {
                    triggerComponent = BossRoomTrigger.gameObject.AddComponent<BossRoomTrigger>();
                }

                triggerComponent.SetBossController(this);
            }
            else
            {
                Debug.LogWarning("No boss room trigger assigned to BossController!");
            }

            // Configure initial state
            if (!IsActive)
            {
                // Disable navmesh agent if boss is inactive
                NavMeshAgent.enabled = false;
            }

            // Setup weapon controllers
            if (MeleeWeapon != null)
            {
                MeleeWeapon.Owner = gameObject;
            }

            if (RangedWeapon != null)
            {
                RangedWeapon.Owner = gameObject;
            }
        }

        new void Update()
        {
            if (!IsActive && !NavMeshAgent.enabled)
                return;

            base.Update();

            // Check if we can attack
            if (!m_IsAttacking && Time.time >= m_LastAttackTime + AttackCooldown && KnownDetectedTarget != null && NavMeshAgent.enabled)
            {
                // Choose a random attack type
                ChooseAttackType();

                // Perform the attack
                StartCoroutine(PerformAttack());
            }

            // Clean up any destroyed enemies from our list
            m_SpawnedEnemies.RemoveAll(enemy => enemy == null);
        }

        void ChooseAttackType()
        {
            // If we've reached max spawned enemies, don't choose spawn attack
            if (m_SpawnedEnemies.Count >= MaxSpawnedEnemies)
            {
                m_CurrentAttackType = Random.value < 0.5f ? BossAttackType.Melee : BossAttackType.Ranged;
                return;
            }

            // Choose a random attack type
            int rand = Random.Range(1, 4); // 1 to 3
            if (rand == 1)
            {
                m_CurrentAttackType = BossAttackType.Melee;
                Debug.Log("Melee attack chosen");
            }
            else if (rand == 2)
            {
                m_CurrentAttackType = BossAttackType.Ranged;
                Debug.Log("Ranged attack chosen");
            }
            else if (rand == 3)
            {
                m_CurrentAttackType = BossAttackType.SpawnEnemies;
                Debug.Log("Spawn enemies attack chosen");
            }
            else
            {
                Debug.LogWarning("Invalid attack type chosen, defaulting to spawn enemies.");
                m_CurrentAttackType = BossAttackType.SpawnEnemies;
            }
        }

        IEnumerator PerformAttack()
        {
            m_IsAttacking = true;
            m_LastAttackTime = Time.time;

            if (onBossAttack != null)
                onBossAttack.Invoke(m_CurrentAttackType);

            switch (m_CurrentAttackType)
            {
                case BossAttackType.Melee:
                    // Set detection module attack range to melee range
                    DetectionModule.AttackRange = MeleeAttackRange;

                    yield return new WaitForSeconds(.1f);

                    // Wait until we're in melee range or timeout
                    float meleeStartTime = Time.time;
                    float meleeTimeout = 15f; // 10 seconds timeout

                    while (!DetectionModule.IsTargetInAttackRange && Time.time - meleeStartTime < meleeTimeout)
                    {
                        if (KnownDetectedTarget == null)
                            break;

                        SetNavDestination(KnownDetectedTarget.transform.position);
                        yield return null;
                    }

                    // Check if we timed out
                    if (Time.time - meleeStartTime >= meleeTimeout)
                    {
                        Debug.Log("Melee attack timed out - target not reached");
                        break;
                    }

                    // If target is in range, perform melee attack
                    if (DetectionModule.IsTargetInAttackRange)
                    {
                        SetNavDestination(transform.position); // Stop moving

                        // Play melee attack animation
                        if (Animator != null)
                        {
                            Animator.SetTrigger(MeleeAttackTrigger);
                        }

                        // Use our custom melee weapon
                        if (MeleeWeapon != null)
                        {
                            MeleeWeapon.MeleeAttack(KnownDetectedTarget.transform.position);
                        }
                    }
                    break;

                case BossAttackType.Ranged:
                    // Set detection module attack range to ranged range
                    DetectionModule.AttackRange = RangedAttackRange;

                    yield return new WaitForSeconds(.1f);

                    // Move to ranged attack range
                    float rangedStartTime = Time.time;
                    while (!DetectionModule.IsTargetInAttackRange && Time.time - rangedStartTime < 5f)
                    {
                        if (KnownDetectedTarget == null)
                            break;

                        SetNavDestination(KnownDetectedTarget.transform.position);
                        yield return null;
                    }

                    // If target is in range, perform ranged attack
                    if (DetectionModule.IsTargetInAttackRange)
                    {
                        //if too close to Target, dash away from the target
                        float distanceToTarget = Vector3.Distance(transform.position, KnownDetectedTarget.transform.position);
                        if (distanceToTarget < MinRangedAttackDistance)
                        {
                            // Calculate dash direction away from target
                            Vector3 dashDirection = (transform.position - KnownDetectedTarget.transform.position).normalized;

                            // Play dash animation
                            if (Animator != null)
                            {
                                Animator.SetTrigger(DashTrigger);
                            }

                            // Apply dash force
                            StartCoroutine(DashCoroutine(dashDirection));
                            yield return new WaitForSeconds(DashDuration);
                        }

                        SetNavDestination(transform.position); // Stop moving

                        // Play ranged attack animation
                        if (Animator != null)
                        {
                            Animator.SetTrigger(RangedAttackTrigger);
                        }
                        yield return new WaitForSeconds(.25f); // Wait for animation to play
                        // Use our custom ranged weapon
                        if (RangedWeapon != null)
                        {
                            RangedWeapon.Shoot();
                        }
                        if (RangedWeapon2 != null)
                        {
                            RangedWeapon2.Shoot();
                        }
                    }
                    break;

                case BossAttackType.SpawnEnemies:
                    // Stop moving
                    SetNavDestination(transform.position);

                    // Play spawn animation
                    if (Animator != null)
                    {
                        Animator.SetTrigger(SpawnAnimationTrigger);
                    }

                    // Play spawn VFX
                    if (SpawnEnemiesVfx != null)
                        SpawnEnemiesVfx.Play();

                    // Wait for animation
                    yield return new WaitForSeconds(SpawnAnimationDuration);

                    // Spawn enemies
                    SpawnEnemies();
                    break;
            }

            // Reset attack range
            yield return new WaitForSeconds(1f);
            m_IsAttacking = false;
        }

        private IEnumerator DashCoroutine(Vector3 direction)
        {
            Debug.Log($"Starting dash with force {DashForce}, duration {DashDuration}");
            float startTime = Time.time;
            NavMeshAgent.enabled = false;
            m_Rigidbody.isKinematic = true;

            Vector3 startPosition = transform.position;
            float targetDashDistance = DashForce;

            // Check for obstacles in dash path
            RaycastHit hit;
            bool hitObstacle = Physics.Raycast(
                startPosition,
                direction,
                out hit,
                targetDashDistance,
                DashObstacleLayers,
                QueryTriggerInteraction.Ignore
            );

            // If we hit something, adjust dash distance
            if (hitObstacle)
            {
                targetDashDistance = Mathf.Max(0, hit.distance - 1f); // Stop 1 unit before the obstacle
                Debug.Log($"Obstacle detected, adjusting dash distance to {targetDashDistance}");
            }

            float elapsedTime = 0;
            while (elapsedTime < DashDuration)
            {
                elapsedTime = Time.time - startTime;
                float normalizedTime = elapsedTime / DashDuration;

                // Use NavMesh to find valid position
                NavMeshHit navHit;
                Vector3 targetPosition = startPosition + (direction * targetDashDistance);
                if (NavMesh.SamplePosition(targetPosition, out navHit, targetDashDistance, NavMesh.AllAreas))
                {
                    targetPosition = navHit.position;
                }

                transform.position = Vector3.Lerp(startPosition, targetPosition, normalizedTime);
                yield return null;
            }

            // Ensure final position is on NavMesh
            NavMeshHit finalHit;
            if (NavMesh.SamplePosition(transform.position, out finalHit, 2f, NavMesh.AllAreas))
            {
                transform.position = finalHit.position;
            }

            float finalDistance = Vector3.Distance(startPosition, transform.position);
            Debug.Log($"Dash completed. Total distance: {finalDistance}");

            m_Rigidbody.isKinematic = false;
            NavMeshAgent.enabled = true;
        }

        void SpawnEnemies()
        {
            if (EnemyPrefabsToSpawn == null || EnemyPrefabsToSpawn.Length == 0 || SpawnPositions.Length == 0)
                return;

            // Spawn 1-3 enemies
            int enemiesToSpawn = Random.Range(1, 4);

            // Limit by max enemies
            enemiesToSpawn = Mathf.Min(enemiesToSpawn, MaxSpawnedEnemies - m_SpawnedEnemies.Count);

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                // Choose a random spawn position
                Transform spawnPos = SpawnPositions[Random.Range(0, SpawnPositions.Length)];

                // Choose a random enemy prefab
                GameObject prefabToSpawn = EnemyPrefabsToSpawn[Random.Range(0, EnemyPrefabsToSpawn.Length)];

                // Spawn the enemy
                GameObject enemy = Instantiate(prefabToSpawn, spawnPos.position, spawnPos.rotation);

                // Add to our list
                m_SpawnedEnemies.Add(enemy);
            }
        }

        public void ActivateBoss()
        {
            if (m_WasActivated)
                return;

            m_WasActivated = true;
            IsActive = true;

            Invoke("EnableNavMeshAgent", 2f);

            // Play activation VFX
            if (BossActivationVfx != null)
                BossActivationVfx.Play();

            // Trigger event
            if (onBossActivated != null)
                onBossActivated.Invoke();
        }
        void EnableNavMeshAgent()
        {
            NavMeshAgent.enabled = true;
            if (BossRoomTrigger != null)
            {
                BossRoomTrigger.enabled = false; // Disable trigger after activation
            }
        }

        // Override to add extra debug visuals
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw melee range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, MeleeAttackRange);

            // Draw ranged attack range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, RangedAttackRange);

            // Draw spawn positions
            if (SpawnPositions != null)
            {
                Gizmos.color = Color.green;
                foreach (Transform spawnPos in SpawnPositions)
                {
                    if (spawnPos != null)
                    {
                        Gizmos.DrawSphere(spawnPos.position, 0.5f);
                        Gizmos.DrawLine(transform.position, spawnPos.position);
                    }
                }
            }

            if (DetectionModule != null)
            {
                // Detection range
                Gizmos.color = DetectionRangeColor;
                Gizmos.DrawWireSphere(transform.position, DetectionModule.DetectionRange);

                // Attack range
                Gizmos.color = AttackRangeColor;
                Gizmos.DrawWireSphere(transform.position, DetectionModule.AttackRange);
            }
        }
    }

    // Trigger class for boss room
    public class BossRoomTrigger : MonoBehaviour
    {
        private BossController m_BossController;

        public void SetBossController(BossController bossController)
        {
            m_BossController = bossController;
        }

        void OnTriggerEnter(Collider other)
        {
            // Check if it's the player
            if (other.GetComponent<PlayerCharacterController>() != null)
            {
                if (m_BossController != null)
                {
                    Debug.Log("Player entered boss room trigger, activating boss.");
                    m_BossController.ActivateBoss();
                }
            }
        }
    }
}