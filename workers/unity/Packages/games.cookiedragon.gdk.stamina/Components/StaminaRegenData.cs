using Unity.Entities;

namespace Cookiedragon.Gdk.Stamina
{
    public struct StaminaRegenData : IComponentData
    {
        // Timers used for health regeneration
        public float DamagedRecentlyTimer;
        public float NextSpatialSyncTimer;
        public float NextRegenTimer;
    }
}
