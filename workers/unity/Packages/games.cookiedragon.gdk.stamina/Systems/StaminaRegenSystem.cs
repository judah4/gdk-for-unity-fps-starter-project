using Improbable.Gdk.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Cookiedragon.Gdk.Stamina
{
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(ServerStaminaModifierSystem))]
    public class StaminaRegenSystem : ComponentSystem
    {
        public struct EntitiesNeedingRegenData
        {
            public readonly int Length;
            [ReadOnly] public EntityArray Entities;
            [ReadOnly] public ComponentDataArray<StaminaRegenComponent.Component> StaminaRegenComponents;
            [ReadOnly] public SubtractiveComponent<StaminaRegenData> DenotesMissingData;
            [ReadOnly] public ComponentDataArray<Authoritative<StaminaComponent.Component>> DenotesAuthority;
        }

        public struct TakingDamage
        {
            public readonly int Length;
            public ComponentDataArray<StaminaRegenData> RegenData;
            public ComponentDataArray<StaminaRegenComponent.Component> StaminaRegenComponents;
            [ReadOnly] public ComponentDataArray<StaminaComponent.ReceivedEvents.StaminaModified> StaminaModifiedEvents;
            [ReadOnly] public ComponentDataArray<Authoritative<StaminaComponent.Component>> DenotesAuthority;
        }

        public struct EntitiesToRegen
        {
            public readonly int Length;
            public ComponentDataArray<StaminaComponent.CommandSenders.ModifyStamina> ModifyStaminaCommandSenders;
            public ComponentDataArray<StaminaRegenComponent.Component> StaminaRegenComponents;
            public ComponentDataArray<StaminaRegenData> RegenData;
            [ReadOnly] public ComponentDataArray<StaminaComponent.Component> StaminaComponents;
            [ReadOnly] public ComponentDataArray<SpatialEntityId> EntityId;
            [ReadOnly] public ComponentDataArray<Authoritative<StaminaComponent.Component>> DenotesAuthority;
        }

        [Inject] private EntitiesNeedingRegenData needData;
        [Inject] private TakingDamage takingDamage;
        [Inject] private EntitiesToRegen toRegen;

        protected override void OnUpdate()
        {
            // Add the StaminaRegenData if you don't currently have it.
            for (var i = 0; i < needData.Length; i++)
            {
                var healthRegenComponent = needData.StaminaRegenComponents[i];

                var regenData = new StaminaRegenData();

                if (healthRegenComponent.DamagedRecently)
                {
                    regenData.DamagedRecentlyTimer = healthRegenComponent.RegenCooldownTimer;
                    regenData.NextSpatialSyncTimer = healthRegenComponent.CooldownSyncInterval;
                }

                PostUpdateCommands.AddComponent(needData.Entities[i], regenData);
            }

            // When the StaminaComponent takes a damaging event, reset the DamagedRecently timer.
            for (var i = 0; i < takingDamage.Length; i++)
            {
                var healthModifiedEvents = takingDamage.StaminaModifiedEvents[i];
                var damagedRecently = false;

                foreach (var modifiedEvent in takingDamage.StaminaModifiedEvents[i].Events)
                {
                    var modifier = modifiedEvent.Modifier;
                    if (modifier.Amount < 0)
                    {
                        damagedRecently = true;
                        break;
                    }
                }

                if (!damagedRecently)
                {
                    continue;
                }

                var regenComponent = takingDamage.StaminaRegenComponents[i];
                var regenData = takingDamage.RegenData[i];

                regenComponent.DamagedRecently = true;
                regenComponent.RegenCooldownTimer = regenComponent.RegenPauseTime;

                regenData.DamagedRecentlyTimer = regenComponent.RegenPauseTime;
                regenData.NextSpatialSyncTimer = regenComponent.CooldownSyncInterval;

                takingDamage.StaminaRegenComponents[i] = regenComponent;
                takingDamage.RegenData[i] = regenData;
            }

            // Count down the timers, and update the StaminaComponent accordingly. 
            for (var i = 0; i < toRegen.Length; i++)
            {
                var healthComponent = toRegen.StaminaComponents[i];
                var regenComponent = toRegen.StaminaRegenComponents[i];

                var regenData = toRegen.RegenData[i];

                // If damaged recently, tick down the timer.
                if (regenComponent.DamagedRecently)
                {
                    regenData.DamagedRecentlyTimer -= Time.deltaTime;

                    if (regenData.DamagedRecentlyTimer <= 0)
                    {
                        regenData.DamagedRecentlyTimer = 0;
                        regenComponent.DamagedRecently = false;
                        regenComponent.RegenCooldownTimer = 0;
                        toRegen.StaminaRegenComponents[i] = regenComponent;
                    }
                    else
                    {
                        // Send a spatial update once every CooldownSyncInterval.
                        regenData.NextSpatialSyncTimer -= Time.deltaTime;
                        if (regenData.NextSpatialSyncTimer <= 0)
                        {
                            regenData.NextSpatialSyncTimer += regenComponent.CooldownSyncInterval;
                            regenComponent.RegenCooldownTimer = regenData.DamagedRecentlyTimer;
                            toRegen.StaminaRegenComponents[i] = regenComponent;
                        }
                    }

                    toRegen.RegenData[i] = regenData;

                    return;
                }

                // If not damaged recently, and not already fully healed, regen. 
                if (healthComponent.Stamina < healthComponent.MaxStamina)
                {
                    regenData.NextRegenTimer -= Time.deltaTime;
                    if (regenData.NextRegenTimer <= 0)
                    {
                        regenData.NextRegenTimer += regenComponent.RegenInterval;

                        // Send command to regen entity.
                        var commandSender = toRegen.ModifyStaminaCommandSenders[i];
                        var modifyStaminaRequest = StaminaComponent.ModifyStamina.CreateRequest(
                            toRegen.EntityId[i].EntityId,
                            new StaminaModifier()
                            {
                                Amount = regenComponent.RegenAmount
                            });
                        commandSender.RequestsToSend.Add(modifyStaminaRequest);
                    }

                    toRegen.RegenData[i] = regenData;
                }
            }
        }
    }
}
