using Unity.FPS.Game;

namespace Unity.FPS.Game
{
    public class AbilityPickupEvent : GameEvent
    {
        public string AbilityName;

        public AbilityPickupEvent(string abilityName)
        {
            AbilityName = abilityName;
        }
    }
}