using Improbable.Gdk.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Cookiedragon.Gdk.Stamina
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ServerStaminaModifierSystem : ComponentSystem
    {
        private struct EntitiesWithModifiedStamina
        {
            public readonly int Length;
            [ReadOnly] public EntityArray Entity;
            public ComponentDataArray<StaminaComponent.Component> Stamina;
            [ReadOnly] public ComponentDataArray<StaminaComponent.CommandRequests.ModifyStamina> ModifyStaminaRequests;
            public ComponentDataArray<StaminaComponent.EventSender.StaminaModified> StaminaModifiedEventSenders;
        }

        [Inject] private EntitiesWithModifiedStamina entitiesWithModifiedStamina;

        protected override void OnUpdate()
        {
            for (var i = 0; i < entitiesWithModifiedStamina.Length; i++)
            {
                var health = entitiesWithModifiedStamina.Stamina[i];
                var healthModifiedEventSender = entitiesWithModifiedStamina.StaminaModifiedEventSenders[i];

                foreach (var request in entitiesWithModifiedStamina.ModifyStaminaRequests[i].Requests)
                {
                    var modifier = request.Payload;

                    var healthModifiedInfo = new StaminaModifiedInfo
                    {
                        Modifier = modifier,
                        StaminaBefore = health.Stamina
                    };

                    //take either new stamina or max stamina, which ever is smaller
                    health.Stamina = Mathf.Min(health.Stamina + modifier.Amount, health.MaxStamina);
                    healthModifiedInfo.StaminaAfter = health.Stamina;

                    healthModifiedEventSender.Events.Add(healthModifiedInfo);
                }

                entitiesWithModifiedStamina.Stamina[i] = health;
            }
        }
    }
}
