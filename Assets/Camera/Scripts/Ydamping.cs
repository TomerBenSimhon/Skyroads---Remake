using Unity.Cinemachine;
using UnityEngine;
using Unity.Cinemachine;

public class Ydamping : MonoBehaviour
{
   private Rigidbody _playerRb;
   private PlayerController _playerController;
   private CinemachineFollow _cinemachineFollow;
   private float _yDamping;
   
   public float fallDamping = 0.15f;

   void Start()
   {
      _cinemachineFollow = GetComponent<CinemachineFollow>();
      
      _playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
      _playerRb = _playerController?.GetComponent<Rigidbody>();
      
      if (_playerRb == null) Debug.LogError("No player rb found");
      if (_playerController == null) Debug.LogError("No player controller found");
      
      var ts = _cinemachineFollow.TrackerSettings;
      _yDamping = ts.PositionDamping.y;
   }

   void Update()
   {
      if(_playerController == null) return;
      
      float playerYSpeed = _playerRb.linearVelocity.y;
      float playerTVel = _playerController.DefaultSettings.terminalVelocity;

      
      var ts = _cinemachineFollow.TrackerSettings;
      ts.PositionDamping.y = Helper.MapValue(playerYSpeed, -playerTVel, 0f, fallDamping, _yDamping);
      _cinemachineFollow.TrackerSettings = ts;
   }
   
   
}
