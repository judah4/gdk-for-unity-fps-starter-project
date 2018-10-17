using Improbable.Gdk.GameObjectRepresentation;
using Pickups;
using UnityEngine;

namespace Fps
{
    [WorkerType(WorkerUtils.UnityClient)]
    public class HealthPickupClientVisibility : MonoBehaviour
    {
        [Require] private HealthPickup.Requirable.Reader healthPickupReader;
        private MeshRenderer cubeMeshRenderer;

        private void OnEnable()
        {
            cubeMeshRenderer = GetComponentInChildren<MeshRenderer>();
            healthPickupReader.ComponentUpdated += OnHealthPickupComponentUpdated;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            cubeMeshRenderer.enabled = healthPickupReader.Data.IsActive;
        }

        private void OnHealthPickupComponentUpdated(HealthPickup.Update update)
        {
            UpdateVisibility();
        }

    }

}

