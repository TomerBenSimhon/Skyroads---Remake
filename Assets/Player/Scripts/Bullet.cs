using System;
using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    private Rigidbody _rb;
    
    private float _travelSpeed = 10f;
    public float Damage {private set; get;}
    private float _lifeTime = 5f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        StartCoroutine(Lifetime());
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & LayerMask.GetMask("Player")) != 0) return;
        if (other.gameObject.layer == gameObject.layer) return;

        if (((1 << other.gameObject.layer) & LayerMask.GetMask("Barrier")) != 0 &&
            other.transform.parent.parent.TryGetComponent(out IBulletInteractable bulletInteractable))
        {
            bulletInteractable.OnBulletHit(this);
            Die();
        }
        
        Die();
    }

    
    
    
    void ApplyMovement()
    {
        Vector3 velocity = _rb.linearVelocity;
        velocity.z = _travelSpeed;
        _rb.linearVelocity = velocity;
    }

    void Die()
    {
        Destroy(gameObject);
    }

    IEnumerator Lifetime()
    {
        yield return new WaitForSeconds(_lifeTime);
        Die();
    }

    public void SetVariables(float travelSpeed, float damage, float lifeTime)
    {
        _travelSpeed = travelSpeed;
        Damage = damage;
        _lifeTime = lifeTime;
    }
}
