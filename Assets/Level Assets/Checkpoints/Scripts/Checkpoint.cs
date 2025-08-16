using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField][Range(0f,100f)] private float fuelAmount;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject player = other.GetComponentInParent<PlayerController>().gameObject;
            
            if (player.TryGetComponent(out PlayerFuel playerFuel))
                playerFuel.SetCheckpointFuel(fuelAmount);
            
            CheckpointManager.Instance.SetSpawnPoint(transform.position, Quaternion.identity, player);
            Debug.Log(transform.position);

            
        }
    }
}
