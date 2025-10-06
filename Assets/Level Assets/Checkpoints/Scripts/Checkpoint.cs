using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private Transform _spawnPoint;
    [SerializeField][Range(0f,100f)] private float fuelAmount;

    void Awake()
    {
        _spawnPoint = transform.Find("Spawnpoint");
        if (_spawnPoint == null)
            Debug.LogError("No Spawn Point Found On Checkpoint");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GlobalEvents.Raise(GlobalEvents.Id.CheckpointTriggered, gameObject);

            var player = other.GetComponentInParent<PlayerController>()?.gameObject;
            if (player && player.TryGetComponent(out PlayerFuel playerFuel))
                playerFuel.SetCheckpointFuel(fuelAmount);

            CheckpointManager.Instance.SetSpawnPoint(_spawnPoint.position, _spawnPoint.rotation);
        }
    }
}