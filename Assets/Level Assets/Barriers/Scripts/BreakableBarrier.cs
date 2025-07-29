using UnityEngine;

public class BreakableBarrier : MonoBehaviour, IBulletInteractable
{
    public float health = 100f;


    public void OnBulletHit(Bullet bullet)
    {
        health -= bullet.Damage;
        
        if(health > 0) return;
        Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
