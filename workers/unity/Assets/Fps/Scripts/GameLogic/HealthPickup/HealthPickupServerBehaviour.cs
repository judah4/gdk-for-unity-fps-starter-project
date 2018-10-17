
using System.Collections;

using Improbable.Gdk.Core;

using Improbable.Gdk.GameObjectRepresentation;

using Improbable.Gdk.Health;

using Pickups;

using UnityEngine;



namespace Fps

{

    [WorkerType(WorkerUtils.UnityGameLogic)]

    public class HealthPickupServerBehaviour : MonoBehaviour

    {

        [Require] private HealthPickup.Requirable.Writer healthPickupWriter;

        [Require] private HealthComponent.Requirable.CommandRequestSender healthCommandRequestSender;

        private Coroutine respawnCoroutine;

        private void OnEnable()
        {

            // If the pickup is inactive on initial checkout - turn off collisions and start the respawning process.

            if (!healthPickupWriter.Data.IsActive)
            {

                respawnCoroutine = StartCoroutine(RespawnCubeRoutine());

            }

        }



        private void OnDisable()
        {

            if (respawnCoroutine != null)
            {

                StopCoroutine(respawnCoroutine);

            }

        }



        private void OnTriggerEnter(Collider other)

        {

            // OnTriggerEnter is fired regardless of whether the MonoBehaviour is enabled or disabled.

            if (healthPickupWriter == null)

            {

                return;

            }



            if (!other.CompareTag("Player"))

            {

                return;

            }



            HandleCollisionWithPlayer(other.gameObject);

        }



        private void SetIsActive(bool isActive)

        {

            // Replace this comment with your code for updating the health pack component's "active" property.
            healthPickupWriter?.Send(new HealthPickup.Update
            {
                IsActive = new Option<BlittableBool>(isActive)
            });

        }



        private void HandleCollisionWithPlayer(GameObject player)

        {

            var playerSpatialOsComponent = player.GetComponent<SpatialOSComponent>();



            if (playerSpatialOsComponent == null)

            {

                return;

            }

            var healthcomp = playerSpatialOsComponent.Worker.GetComponentDataFromEntity<HealthComponent.Component>();
            var health = healthcomp[playerSpatialOsComponent.Entity];
            if(health.Health >= health.MaxHealth)
            {
                return;
            }

            // Replace this comment with your code for notifying the Player entity that it will receive health.
            healthCommandRequestSender.SendModifyHealthRequest(playerSpatialOsComponent.SpatialEntityId, new HealthModifier
            {
                Amount = healthPickupWriter.Data.HealthValue
            });


            // Toggle health pack to its "consumed" state.

            SetIsActive(false);

            // Begin cool-down period before re-activating health pack
            respawnCoroutine = StartCoroutine(RespawnCubeRoutine());

        }

        private IEnumerator RespawnCubeRoutine()
        {

            yield return new WaitForSeconds(15f);

            SetIsActive(true);

        }

    }

}

