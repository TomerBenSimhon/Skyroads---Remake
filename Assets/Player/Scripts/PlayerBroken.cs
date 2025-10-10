
using UnityEngine;

[DefaultExecutionOrder(1)]
public class PlayerBroken : MonoBehaviour
{
    [SerializeField] PlayerControllerSettings controllerSettings;
    
    private PlayerController _playerController;

    void OnEnable()
    {
        GlobalEvents.Raised += OnRespawn;
    }

    void OnDisable()
    {
        GlobalEvents.Raised -= OnRespawn;
    }

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        controllerSettings.CopyTo(_playerController.RuntimeSettings);
        
        GlobalEvents.Raise(GlobalEvents.Id.PlayerBroken);
    }

    void OnRespawn(GlobalEvents.Id id, GameObject sender)
    {
        if((id & GlobalEvents.Id.PlayerRespawned) == 0) return;
        controllerSettings.CopyTo(_playerController.RuntimeSettings);
        
        GlobalEvents.Raise(GlobalEvents.Id.PlayerBroken);
    }
}
