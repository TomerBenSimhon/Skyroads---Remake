using UnityEngine;

public class GlobalEventTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
            GlobalEvents.Raise(GlobalEvents.Id.OnEventTriggered);
    }
}
