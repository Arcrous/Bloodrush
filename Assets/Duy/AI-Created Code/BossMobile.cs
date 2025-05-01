using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(BossController))]
    public class BossMobile : EnemyMobile
    {
        public enum BossState
        {
            Inactive,
            Patrol,
            Follow,
            MeleeAttack,
            RangedAttack,
            Spawning
        }

        [Header("Boss Animations")]
        [Tooltip("Animation for boss activation")]
        public string ActivationAnimationTrigger = "Activate";

        [Header("Boss Effects")]
        [Tooltip("VFX for melee attack")]
        public ParticleSystem MeleeAttackVfx;

        [Tooltip("VFX for ranged attack")]
        public ParticleSystem RangedAttackVfx;

        [Tooltip("Audio for boss activation")]
        public AudioClip BossActivationSfx;

        public BossState CurrentBossState { get; private set; }

        BossController m_BossController;

        protected override void Start()
        {
            // Get references
            m_BossController = GetComponent<BossController>();
            DebugUtility.HandleErrorIfNullGetComponent<BossController, BossMobile>(m_BossController, this, gameObject);

            // Subscribe to events
            m_BossController.onBossActivated += OnBossActivated;
            m_BossController.onBossAttack += OnBossAttack;

            // Initial state
            CurrentBossState = BossState.Inactive;

            // Call base Start last, since it starts AI behavior
            base.Start();
        }

        new void Update()
        {
            // Don't run the base update if inactive
            if (CurrentBossState == BossState.Inactive)
                return;

            base.Update();

            // Update boss state based on current attack
            UpdateBossState();
        }

        void UpdateBossState()
        {
            // Update based on AI state and boss controller
            if (AiState == AIState.Patrol)
            {
                CurrentBossState = BossState.Patrol;
            }
            else if (AiState == AIState.Follow)
            {
                CurrentBossState = BossState.Follow;
            }
        }

        void OnBossActivated()
        {
            // Change state
            CurrentBossState = BossState.Patrol;

            // Play activation animation
            if (Animator != null)
                Animator.SetTrigger(ActivationAnimationTrigger);

            // Play activation sound
            if (BossActivationSfx != null)
                AudioUtility.CreateSFX(BossActivationSfx, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
        }

        void OnBossAttack(BossController.BossAttackType attackType)
        {
            switch (attackType)
            {
                case BossController.BossAttackType.Melee:
                    CurrentBossState = BossState.MeleeAttack;
                    if (MeleeAttackVfx != null)
                        MeleeAttackVfx.Play();
                    break;

                case BossController.BossAttackType.Ranged:
                    CurrentBossState = BossState.RangedAttack;
                    if (RangedAttackVfx != null)
                        RangedAttackVfx.Play();
                    break;

                case BossController.BossAttackType.SpawnEnemies:
                    CurrentBossState = BossState.Spawning;
                    // Animation is triggered in BossController
                    break;
            }
        }

        // Override to add extra debug visuals
        void OnDrawGizmosSelected()
        {
            // If we have a boss controller, draw its ranges
            BossController bossController = GetComponent<BossController>();
            if (bossController != null)
            {
                // Draw melee range
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, bossController.MeleeAttackRange);

                // Draw ranged attack range
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, bossController.RangedAttackRange);

                // Draw spawn positions
                if (bossController.SpawnPositions != null)
                {
                    Gizmos.color = Color.green;
                    foreach (Transform spawnPos in bossController.SpawnPositions)
                    {
                        if (spawnPos != null)
                        {
                            Gizmos.DrawSphere(spawnPos.position, 0.5f);
                            Gizmos.DrawLine(transform.position, spawnPos.position);
                        }
                    }
                }
            }
        }
    }
}