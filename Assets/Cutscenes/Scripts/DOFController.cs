using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class DOFController : MonoBehaviour
{
    public Volume postProcessVolume;

    [Header("DOF Settings")]
    public float duration = 1.5f;
    public float startFocalLength = 300f;
    public float targetFocalLength = 92f;
    public float startAperture = 0f;
    public float targetAperture = 32f;

    private DepthOfField dof;

    void Start()
    {
        if (postProcessVolume.profile.TryGet(out dof))
        {
            // מאחסן את DepthOfField
        }
    }

    public void AnimateDOF()
    {
        StartCoroutine(ChangeDOF());
    }

    private IEnumerator ChangeDOF()
    {
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            dof.focalLength.value = Mathf.Lerp(startFocalLength, targetFocalLength, t);
            dof.aperture.value = Mathf.Lerp(startAperture, targetAperture, t);
            time += Time.deltaTime;
            yield return null;
        }

        dof.focalLength.value = targetFocalLength;
        dof.aperture.value = targetAperture;
    }
}
