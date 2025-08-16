using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerFuel : MonoBehaviour, ICheckpointSavable
{ 
    [Header("UI Settings")]
    [SerializeField] TextMeshProUGUI fuelText;
    
    [Header("Settings")]
    [Range(0f, 10f)] [Tooltip("Fuel per unit traveled forward")]
    public float fuelConsumption;
    [Range(0f, 100f)]
    public float startingFuel;
    
    private PlayerDeath _playerDeath;
    
    private float _lastFrameZPos;
    private float _fuel;
    private float _usedFuel;

    private float _checkpointFuel;

    void Awake()
    {
        _playerDeath = GetComponent<PlayerDeath>();
        _lastFrameZPos = transform.position.z;
        _fuel = startingFuel;
    }

    private void Start()
    {
        _fuel = _checkpointFuel <= 0 ? startingFuel : _checkpointFuel;
    }

    void Update()
    {
        UseFuel();
        DisplayFuel();
        if(_fuel <= 0f) _playerDeath.DieNoFuel();
    }

    void LateUpdate()
    {
        _lastFrameZPos = transform.position.z;
    }

    private void UseFuel()
    {
        _usedFuel = (transform.position.z - _lastFrameZPos) * fuelConsumption;
        _fuel -= _usedFuel;
        _fuel = Mathf.Clamp(_fuel, 0f, startingFuel);
    }

    public void Refuel(float amountPerUnit)
    {
        float filledFuel = (transform.position.z - _lastFrameZPos) * amountPerUnit;
        _fuel += filledFuel + _usedFuel;
        _fuel = Mathf.Clamp(_fuel, 0f, startingFuel);
    }

    public void SetCheckpointFuel(float amount)
    {
        _checkpointFuel = amount;
    }

    private void DisplayFuel()
    {
        fuelText.text = "Fuel:\n" + _fuel.ToString("F1");
    }
    
    
    //checkpoint saves
    public struct SaveData { public float newStartingFuel; }
    public string SaveKey => "PlayerFuel";
    public object CaptureState() => new SaveData {newStartingFuel = _checkpointFuel};
    
    
    public void RestoreState(object state)
    {
        var saveData = (SaveData)state;
        _checkpointFuel = saveData.newStartingFuel;
    }
}
