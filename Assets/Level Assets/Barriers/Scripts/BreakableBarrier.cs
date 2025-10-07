using System.Collections.Generic;
using UnityEngine;

public class BreakableBarrier : MonoBehaviour, IBulletInteractable
{
    public float health = 100f;
    
    [SerializeField] List<GameObject> objsToDeactivate = new ();
    
    [Header("OnRespawn event")]
    [SerializeField] GlobalEvents.Id onRespawn;

    private void OnEnable()
    {
        GlobalEvents.Raised += OnPlayerRespawned;
    }

    private void OnDisable()
    {
        GlobalEvents.Raised -= OnPlayerRespawned;
    }


    private void OnPlayerRespawned(GlobalEvents.Id id, GameObject sender)
    {
        if ((id & onRespawn) == 0) return;
        
        Activate();
    }
    
    
    public void OnBulletHit(Bullet bullet)
    {
        health -= bullet.Damage;
        
        if(health > 0) return;
        Deactivate();
    }

    private void Deactivate()
    {
        foreach (var obj in objsToDeactivate)
        {
            obj.SetActive(false);
        }
    }

    private void Activate()
    {
        foreach (var obj in objsToDeactivate)
        {
            obj.SetActive(true);
        }
        
    }
    
}
