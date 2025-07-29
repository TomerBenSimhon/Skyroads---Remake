using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Range(0f, 5f)] public float deathTimer;
    [Range(0f, 5f)] public float noFuelTimer;
    
    public bool IsDead { get; private set; }
    
    private PlayerController _player;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
    }
    public void Die()
    {
        IsDead = true;
        DeathEffect();
        GameManager.Instance.RestartLevel(deathTimer);
    }

    public void DieNoFuel()
    {
        IsDead = true;
        NoFuelEffect();
        GameManager.Instance.RestartLevel(noFuelTimer);
    }

    private void DeathEffect()
    {
        Destroy(gameObject);
    }

    private void NoFuelEffect()
    {
        _player.RuntimeSettings.forwardSpeed = 0f;
        _player.RuntimeSettings.horizontalSpeed = 0f;
        _player.RuntimeSettings.jumpHeight = 0f;
        _player.RuntimeSettings.turningAngle = 0f;
    }
}
