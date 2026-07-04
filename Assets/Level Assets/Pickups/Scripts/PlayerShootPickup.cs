using UnityEngine;

public class PlayerShootPickup : MonoBehaviour, IPickupsInterface
{
    public void OnPickup(GameObject player)
    {
        if(!player.TryGetComponent(out PlayerShoot playerShoot)) return;
        playerShoot.enabled = true;
        
        GlobalEvents.Raise(GlobalEvents.Id.PowerUpApplied, gameObject);
        
        Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
