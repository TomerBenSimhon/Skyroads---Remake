using UnityEngine;

[ExecuteAlways]
public class RefuelPlatformController : MonoBehaviour
{
    [SerializeField] private Material platformMaterial;
    [SerializeField] private bool isPlayerOn = false;

    public float transitionTime;
    private float currentFill;
    private float targetFill;
    
    private void Update()
    {
        targetFill = isPlayerOn ? 1f : 0f;

        float speed = 1f / Mathf.Max(transitionTime, 0.01f);
        currentFill = Mathf.MoveTowards(currentFill, targetFill, Time.deltaTime * speed);

        if (platformMaterial != null)
        {
            platformMaterial.SetFloat("_IsPlayerOn", currentFill); // שים לב לשם פה!
        }
    }
}
