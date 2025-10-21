using UnityEngine;

public class ActivateTrigger : MonoBehaviour
{
    
    [SerializeField] GameObject[] activateTriggers;
    [SerializeField] GameObject[] deactivateTriggers;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (var obj in activateTriggers)
            {
                obj.SetActive(true);
            }

            foreach (var obj in deactivateTriggers)
            {
                obj.SetActive(false);
            }
        }
    }
}
