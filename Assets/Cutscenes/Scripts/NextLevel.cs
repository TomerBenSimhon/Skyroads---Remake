using UnityEngine;

public class NextLevel : MonoBehaviour
{
    public void LoadNextLevelLocal()
    {
        GameManager.Instance.LoadNextLevel();
    }
}
