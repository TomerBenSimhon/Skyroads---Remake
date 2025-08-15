using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject player = other.GetComponentInParent<PlayerController>().gameObject;
            
            CheckpointManager.Instance.SetSpawnPoint(transform.position, Quaternion.identity, player);
            Debug.Log(transform.position);
        }
    }
}
