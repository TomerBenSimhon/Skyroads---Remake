using System;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    public Vector3 bulletSpawnOffset;
    
    [Header("Settings")]
    public float fireRate;
    public float bulletSpeed;
    public float bulletDamage;
    public float bulletLifeTime;
    
    private PlayerInput _playerInput;

    private float _lastFireTime;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }
    private void Update()
    {
        if(!_playerInput.ShootHeld) return;
        
        TryShoot();
    }

    void TryShoot()
    {
        if (Time.time - _lastFireTime < fireRate) return;

        Shoot();
        _lastFireTime = Time.time;
    }
    
    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, transform.position + bulletSpawnOffset, Quaternion.Euler(90f, 0f, 0f));
        
        if (!bullet.TryGetComponent(out Bullet bulletScript)) return;
        bulletScript.SetVariables(bulletSpeed, bulletDamage, bulletLifeTime);
    }

    #if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + bulletSpawnOffset, 0.05f);
    }

    #endif
}
