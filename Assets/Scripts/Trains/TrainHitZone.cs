using UnityEngine;

namespace RunningLate
{
    public class TrainHitZone : MonoBehaviour
    {
        public enum ZoneType
        {
            Front,
            Side
        }

        private TrainVehicle owner;
        private ZoneType zoneType;

        public void Initialize(
            TrainVehicle trainOwner,
            ZoneType type
        )
        {
            owner = trainOwner;
            zoneType = type;

            BoxCollider box =
                GetComponent<BoxCollider>();

            if (box == null)
            {
                box =
                    gameObject.AddComponent<BoxCollider>();
            }

            box.isTrigger = true;
        }

        private void OnTriggerEnter(
            Collider other
        )
        {
            if (owner == null)
            {
                return;
            }

            PlayerController player =
                other.GetComponentInParent<PlayerController>();

            if (player == null)
            {
                return;
            }

            if (zoneType == ZoneType.Front)
            {
                owner.RegisterFrontHit(player);
            }
            else
            {
                owner.RegisterSideHit(player);
            }
        }
    }
}