using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform _spawnPoint;
    [SerializeField][Range(0f,100f)] private float fuelAmount;


    void Awake()
    {
        if (_spawnPoint == null)
            _spawnPoint = transform.Find("Spawnpoint");
        if (_spawnPoint == null)
        {
            Debug.LogError("No Spawn Point Found On Checkpoint");
            Debug.Log(transform.position);
        }
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GlobalEvents.Raise(GlobalEvents.Id.CheckpointTriggered, gameObject);
            GameObject player = other.GetComponentInParent<PlayerController>().gameObject;
            
            if (player.TryGetComponent(out PlayerFuel playerFuel))
                playerFuel.SetCheckpointFuel(fuelAmount);
            
            CheckpointManager.Instance.SetSpawnPoint(_spawnPoint.position, Quaternion.identity, player);
            Debug.Log(_spawnPoint.position);
        }
    }
}
