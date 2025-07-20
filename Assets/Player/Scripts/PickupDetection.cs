using UnityEngine;

public class PickupDetection : MonoBehaviour
{
    [SerializeField] LayerMask pickupLayer;

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & pickupLayer) == 0) return;
        Debug.Log("Pickup detected");
        if(!other.transform.parent.TryGetComponent(out IPickupsInterface pickup)) return;
        Debug.Log("Pickup picked");
        
        pickup.OnPickup(gameObject);
    }
}
