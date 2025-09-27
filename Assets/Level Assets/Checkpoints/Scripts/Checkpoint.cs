using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private Transform _spawnPoint;
    [SerializeField][Range(0f,100f)] private float fuelAmount;


    void Awake()
    {
        _spawnPoint = transform.Find("SpawnPoint");
        if (_spawnPoint == null)
            Debug.LogError("No Spawn Point Found On Checkpoint");
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject player = other.GetComponentInParent<PlayerController>().gameObject;
            
            if (player.TryGetComponent(out PlayerFuel playerFuel))
                playerFuel.SetCheckpointFuel(fuelAmount);
            
            CheckpointManager.Instance.SetSpawnPoint(_spawnPoint.position, Quaternion.identity, player);
            Debug.Log(_spawnPoint.position);

            GlobalEvents.Raise(GlobalEvents.Id.CheckpointTriggered,gameObject);
        }
    }
}
