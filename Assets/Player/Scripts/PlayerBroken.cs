using UnityEngine;

[DefaultExecutionOrder(1)]
public class PlayerBroken : MonoBehaviour
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
}
