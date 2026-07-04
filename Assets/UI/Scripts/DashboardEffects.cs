using System;
using System.Collections;
using TMPro;
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
    
    [Header("Progression bar references")]
    [SerializeField] private Transform levelStart;
    [SerializeField] private Transform levelEnd;
    [SerializeField] private TextMeshPro progressText;
    
    private Coroutine _coroutine;
    private Transform _playerTransform;

    void Start()
    {
        _playerTransform = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include).transform;
    }

    private void Update()
    {
        ShowProgress();
    }

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

    void ShowProgress()
    {
        float visualVal = Helper.MapValue(_playerTransform.position.z, levelStart.position.z, levelEnd.position.z, 0, 1);
        
        float visualValLerp = defaultMaterial.GetFloat("_Move");
        visualValLerp = Mathf.Lerp(visualValLerp, visualVal, Time.deltaTime * 5f);

        int textValLerp = Mathf.RoundToInt(visualValLerp * 100f);
        
        defaultMaterial.SetFloat("_Move", visualValLerp);
        progressText.text = textValLerp.ToString("D2") + "%";
    }
    
    
}
