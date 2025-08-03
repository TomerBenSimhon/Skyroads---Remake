using UnityEngine;

public class PlayerShootPickup : MonoBehaviour, IPickupsInterface
{
    public void OnPickup(GameObject player)
    {
        if(!player.TryGetComponent(out PlayerShoot playerShoot)) return;
        playerShoot.enabled = true;
        
        Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
