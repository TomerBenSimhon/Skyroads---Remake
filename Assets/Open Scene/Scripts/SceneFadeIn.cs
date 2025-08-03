using UnityEngine;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    public CanvasGroup blackScreen;
    public float fadeDuration = 1.5f;
    public float delayBeforeFade = 0f;

    void Start()
    {
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        if (delayBeforeFade > 0f)
            yield return new WaitForSeconds(delayBeforeFade);

        float time = 0f;
        float startAlpha = blackScreen.alpha;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            blackScreen.alpha = Mathf.Lerp(startAlpha, 0f, t);
            time += Time.deltaTime;
            yield return null;
        }

        blackScreen.alpha = 0f;
        blackScreen.gameObject.SetActive(false); // מסתיר אותו לחלוטין בסוף
    }
}
