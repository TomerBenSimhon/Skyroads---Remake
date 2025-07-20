using UnityEngine;

public class PlayerFixPickup : MonoBehaviour, IPickupsInterface
{
    public void OnPickup(GameObject player)
    {
        if(!player.TryGetComponent(out PlayerController playerController)) return;
        playerController.DefaultSettings.CopyTo(playerController.RuntimeSettings);
        
        if(!player.TryGetComponent(out PlayerBroken playerBroken)) return;
        playerBroken.enabled = false;
        
        Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
