using System;
using UnityEngine;

public class BarrierDetection : MonoBehaviour
{
    [SerializeField] LayerMask barrierLayer;
    
    private PlayerDeath _playerDeath;

    private void Awake()
    {
        _playerDeath = GetComponent<PlayerDeath>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & barrierLayer) == 0) return;
        
        _playerDeath.Die();
    }
}
