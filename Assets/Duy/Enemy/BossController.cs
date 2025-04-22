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
        public GameObject EnemyPrefabToSpawn;

        [Tooltip("The maximum number of enemies that can be spawned at once")]
        public int MaxSpawnedEnemies = 5;

        [Tooltip("Positions where enemies can be spawned")]
        public Transform[] SpawnPositions;

        [Tooltip("Cooldown between attacks")]
        public float AttackCooldown = 3f;

        [Tooltip("Duration of the spawn animation")]
        public float SpawnAnimationDuration = 2f;

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
        public float DashForce = 10f;

        [Tooltip("Duration of the dash")]
        public float DashDuration = 0.5f;

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
        }

        new void Update()
        {
            if (!IsActive)
                return;

            base.Update();

            // Check if we can attack
            if (!m_IsAttacking && Time.time >= m_LastAttackTime + AttackCooldown && KnownDetectedTarget != null)
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
            int rand = Random.Range(1, 3);
            if (rand == 1)
                m_CurrentAttackType = BossAttackType.Melee;
            else if (rand == 2)
                m_CurrentAttackType = BossAttackType.Ranged;
            else
                m_CurrentAttackType = BossAttackType.SpawnEnemies;
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
                    Debug.Log("Melee attack range is now: " + DetectionModule.AttackRange);

                    yield return new WaitForSeconds(.1f);

                    // Wait until we're in melee range or timeout
                    float meleeStartTime = Time.time;
                    while (!DetectionModule.IsTargetInAttackRange && Time.time - meleeStartTime < 5f)
                    {
                        Debug.Log("Is NOT in melee range");
                        if (KnownDetectedTarget == null)
                            break;

                        SetNavDestination(KnownDetectedTarget.transform.position);
                        yield return null;
                    }

                    // If target is in range, perform melee attack
                    if (DetectionModule.IsTargetInAttackRange)
                    {
                        Debug.Log("Is in melee range");
                        SetNavDestination(transform.position); // Stop moving

                        // Play melee attack animation
                        if (Animator != null)
                        {
                            Debug.Log("Melee attack animation triggered");
                            Animator.SetTrigger(MeleeAttackTrigger);
                        }

                        TryAtack(KnownDetectedTarget.transform.position);
                    }
                    break;

                case BossAttackType.Ranged:
                    // Set detection module attack range to ranged range
                    DetectionModule.AttackRange = RangedAttackRange;
                    Debug.Log("Ranged attack range is now: " + DetectionModule.AttackRange);

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
                        //if too close to Target, dash backwards using AddForce 
                        float distanceToTarget = Vector3.Distance(transform.position, KnownDetectedTarget.transform.position);
                        if (distanceToTarget < MinRangedAttackDistance)
                        {
                            // Calculate dash direction (away from target)
                            Vector3 dashDirection = (transform.position - KnownDetectedTarget.transform.position).normalized;

                            // Disable NavMeshAgent speed during dash
                            float originalSpeed = NavMeshAgent.speed;
                            NavMeshAgent.speed = 0f;

                            //Play dash animation
                            Animator.SetTrigger(DashTrigger);

                            // Apply dash force
                            m_Rigidbody.AddForce(dashDirection * DashForce, ForceMode.Impulse);

                            // Wait for dash duration
                            yield return new WaitForSeconds(DashDuration);

                            // Re-enable NavMeshAgent
                            m_Rigidbody.velocity = Vector3.zero;
                            NavMeshAgent.speed = originalSpeed;
                        }

                        SetNavDestination(transform.position); // Stop moving

                        // Play ranged attack animation
                        if (Animator != null)
                        {
                            Debug.Log("Ranged attack animation triggered");
                            Animator.SetTrigger(RangedAttackTrigger);
                        }

                        TryAtack(KnownDetectedTarget.transform.position);
                    }
                    break;

                case BossAttackType.SpawnEnemies:
                    // Stop moving
                    SetNavDestination(transform.position);

                    // Play spawn animation
                    if (Animator != null)
                    {
                        Debug.Log("Spawn enemies animation triggered");
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

        void SpawnEnemies()
        {
            if (EnemyPrefabToSpawn == null || SpawnPositions.Length == 0)
                return;

            // Spawn 1-3 enemies
            int enemiesToSpawn = Random.Range(1, 4);

            // Limit by max enemies
            enemiesToSpawn = Mathf.Min(enemiesToSpawn, MaxSpawnedEnemies - m_SpawnedEnemies.Count);

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                // Choose a random spawn position
                Transform spawnPos = SpawnPositions[Random.Range(0, SpawnPositions.Length)];

                // Spawn the enemy
                GameObject enemy = Instantiate(EnemyPrefabToSpawn, spawnPos.position, spawnPos.rotation);

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

            // Enable navmesh agent
            NavMeshAgent.enabled = true;

            // Play activation VFX
            if (BossActivationVfx != null)
                BossActivationVfx.Play();

            // Trigger event
            if (onBossActivated != null)
                onBossActivated.Invoke();
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
                    m_BossController.ActivateBoss();
                }
            }
        }
    }
}