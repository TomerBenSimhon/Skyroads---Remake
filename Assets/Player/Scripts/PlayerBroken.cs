
using UnityEngine;

[DefaultExecutionOrder(1)]
public class PlayerBroken : MonoBehaviour, ICheckpointSavable
{
    [SerializeField] PlayerControllerSettings controllerSettings;
    
    private PlayerController _playerController;

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        controllerSettings.CopyTo(_playerController.RuntimeSettings);
    }

    public struct SaveData { public bool Enabled; }
    public string SaveKey => "PlayerBroken";
    public object CaptureState() => new SaveData {Enabled = enabled};
    
    
    public void RestoreState(object state)
    {
        var saveData = (SaveData)state;
        enabled = saveData.Enabled;
    }
}
