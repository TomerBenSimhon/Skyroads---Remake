using UnityEngine;

public class StartGameSequence : MonoBehaviour
{
    public DOFController dofController;
    public CinematicBars cinematicBars;
    public CrashSequence crashSequence;

    [Header("UI Buttons To Fade Out")]
    public UIButtonFader[] buttonsToFade;

    public void OnStartGamePressed()
    {
        if (dofController != null) dofController.AnimateDOF();
        if (cinematicBars != null) cinematicBars.FadeInBars();
        if (crashSequence != null) crashSequence.StartCrash();

        if (buttonsToFade != null)
        {
            foreach (var button in buttonsToFade)
            {
                if (button != null)
                    button.FadeOut();
            }
        }
    }
}
