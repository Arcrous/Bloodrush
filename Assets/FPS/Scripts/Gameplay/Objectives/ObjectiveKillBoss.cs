using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine.AI;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class ObjectiveKillBoss : Objective
    {
        [Tooltip("The boss that needs to be killed to complete this objective")]
        public GameObject TargetBoss;

        protected override void Start()
        {
            base.Start();

            // Set default title and description if not set
            if (string.IsNullOrEmpty(Title))
                Title = "Eliminate the Boss";

            if (string.IsNullOrEmpty(Description))
                Description = "Kill the boss to complete the objective";

            // Subscribe to boss's health component death event
            if (TargetBoss != null)
            {
                TargetBoss.GetComponent<Health>().OnDie += OnBossKilled;
            }
            else
            {
                Debug.LogError("No target boss assigned to ObjectiveKillBoss!");
            }
        }

        void OnBossKilled()
        {
            CompleteObjective(string.Empty, string.Empty, "Boss eliminated!");
        }

        void OnDestroy()
        {
            // Cleanup subscription if the boss still exists
            if (TargetBoss != null)
            {
                var health = TargetBoss.GetComponent<Health>();
                if (health != null)
                    health.OnDie -= OnBossKilled;
            }
        }
    }
}