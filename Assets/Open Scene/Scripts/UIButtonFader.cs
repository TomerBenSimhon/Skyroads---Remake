using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIButtonFader : MonoBehaviour
{
    public float fadeDuration = 1f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void FadeOut()
    {
        StartCoroutine(FadeToZero());
    }

    private IEnumerator FadeToZero()
    {
        float time = 0f;
        float startAlpha = canvasGroup.alpha;

        while (time < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false); // מסתיר אחרי הדעיכה
    }
}
