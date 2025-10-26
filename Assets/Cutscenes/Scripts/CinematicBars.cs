using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CinematicBars : MonoBehaviour
{
    public Image topBar;
    public Image bottomBar;

    public void FadeInBars()
    {
        StartCoroutine(FadeBar(topBar));
        StartCoroutine(FadeBar(bottomBar));
    }

    private IEnumerator FadeBar(Image bar)
    {
        float duration = 1.5f;
        float time = 0f;
        Color color = bar.color;

        while (time < duration)
        {
            float alpha = Mathf.Lerp(0f, 1f, time / duration);
            bar.color = new Color(color.r, color.g, color.b, alpha);
            time += Time.deltaTime;
            yield return null;
        }

        bar.color = new Color(color.r, color.g, color.b, 1f);
    }
}
