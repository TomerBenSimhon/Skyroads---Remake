using System.Collections;
using UnityEngine;

public enum RightScreenMat
{
    Fixed, Shoot
}

public class DashboardEffects : MonoBehaviour
{
    [Header("Screen Renderer reference")]
    [SerializeField] private Renderer renderer;
    
    [Header("Screen materials references")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material fixedMaterial;
    [SerializeField] private Material shootMaterial;
    
    private Coroutine _coroutine;


    public void SwitchScreenMat(RightScreenMat matID, float time)
    {
        if(_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(SwitchScreenMatCR(matID, time));
    }
    
    private IEnumerator SwitchScreenMatCR(RightScreenMat matID, float time)
    {
        renderer.material = matID == RightScreenMat.Fixed ? fixedMaterial : shootMaterial;
        yield return new WaitForSeconds(time);
        renderer.material = defaultMaterial;
        
        _coroutine = null;
    }
}
