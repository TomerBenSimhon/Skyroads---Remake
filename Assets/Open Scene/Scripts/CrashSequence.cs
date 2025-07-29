using UnityEngine;
using UnityEngine.Playables;

public class CrashSequence : MonoBehaviour
{
    public PlayableDirector[] loopedDirectors;
    public PlayableDirector crashDirector;

    public void StartCrash()
    {
        crashDirector.Play();
        crashDirector.stopped += OnCrashFinished;
    }

    private void OnCrashFinished(PlayableDirector dir)
    {
        foreach (var director in loopedDirectors)
        {
            director.Stop();
        }
        crashDirector.stopped -= OnCrashFinished;
    }
}
