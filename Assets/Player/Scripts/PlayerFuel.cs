using TMPro;
using UnityEngine;

public class PlayerFuel : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private TextMeshPro fuelText;
    [SerializeField] private Renderer fuelRenderer;

    [Header("Settings")]
    [Range(0f, 10f)]
    [Tooltip("Fuel per unit traveled forward")]
    public float fuelConsumption = 1f;
    [Range(0f, 100f)] public float startingFuel = 100f;

    [Header("Fuel Colors")]
    [SerializeField] private Color highFuelColor = Color.green;
    [SerializeField] private Color midFuelColor = Color.yellow;
    [SerializeField] private Color lowFuelColor = Color.red;

    [Header("Color Transition")]
    [Range(0.1f, 20f)]
    [SerializeField] private float colorLerpSpeed = 5f;

    [Header("Outline Pulse (Text Effect)")]
    [Tooltip("When fuel < 20, the text outline thickness will pulse from 0→1→0")]
    [SerializeField] private float outlinePulseSpeed = 2f; // מהירות הדופק
    [SerializeField] private bool pulseOnlyWhenLow = true; // מאפשר שליטה אם האפקט מופעל רק כשהדלק נמוך

    [Header("OnRespawn event")]
    [SerializeField] private GlobalEvents.Id onRespawn;

    private PlayerDeath _playerDeath;
    private float _lastFrameZPos;
    [SerializeField] private float _fuel;
    private float _usedFuel;
    private float _checkpointFuel;

    // Outline runtime cache
    private float _outlinePulseTime;

    void Awake()
    {
        _playerDeath = GetComponent<PlayerDeath>();
        _lastFrameZPos = transform.position.z;
        _fuel = startingFuel;

        if (!fuelRenderer)
            Debug.LogWarning("Fuel renderer not set on PlayerFuel.");
    }

    void OnEnable() => GlobalEvents.Raised += OnRespawn;
    void OnDisable() => GlobalEvents.Raised -= OnRespawn;

    private void OnRespawn(GlobalEvents.Id id, GameObject player)
    {
        if ((id & onRespawn) == 0) return;
        _fuel = _checkpointFuel <= 0 ? startingFuel : _checkpointFuel;
    }

    void Update()
    {
        UseFuel();
        DisplayFuel();
        AnimateOutline();
        if (_fuel <= 0f) _playerDeath.DieNoFuel();
    }

    void LateUpdate() => _lastFrameZPos = transform.position.z;

    private void UseFuel()
    {
        _usedFuel = (transform.position.z - _lastFrameZPos) * fuelConsumption;
        _fuel = Mathf.Clamp(_fuel - _usedFuel, 0f, startingFuel);
    }

    public void Refuel(float amountPerUnit)
    {
        float filledFuel = (transform.position.z - _lastFrameZPos) * amountPerUnit;
        _fuel = Mathf.Clamp(_fuel + filledFuel + _usedFuel, 0f, startingFuel);
    }

    public void SetCheckpointFuel(float amount) => _checkpointFuel = amount;

    private void DisplayFuel()
    {
        if (fuelText)
            fuelText.text = _fuel.ToString("F1");

        if (!fuelRenderer) return;

        float level = Helper.MapValue(_fuel, 0f, startingFuel, 0f, 1f);
        fuelRenderer.material.SetFloat("_Level", level);

        Color targetFillColor;
        Color targetEdgeColor;

        if (_fuel >= 60f)
        {
            float t = Mathf.InverseLerp(100f, 60f, _fuel);
            float smooth = Mathf.SmoothStep(0, 1, t * 0.5f);
            targetFillColor = Color.Lerp(highFuelColor, midFuelColor, smooth);
            targetEdgeColor = Color.Lerp(highFuelColor * 1.2f, midFuelColor * 1.2f, smooth);
        }
        else if (_fuel >= 25f)
        {
            float t = Mathf.InverseLerp(59f, 25f, _fuel);
            float smooth = Mathf.SmoothStep(0, 1, t * 0.7f);
            targetFillColor = Color.Lerp(midFuelColor, lowFuelColor, smooth);
            targetEdgeColor = Color.Lerp(midFuelColor * 1.2f, lowFuelColor * 1.2f, smooth);
        }
        else
        {
            float t = Mathf.InverseLerp(24f, 0f, _fuel);
            targetFillColor = Color.Lerp(lowFuelColor, Color.black, t);
            targetEdgeColor = Color.Lerp(lowFuelColor * 1.2f, Color.black, t);
        }

        Color currentFill = fuelRenderer.material.GetColor("_FuelColor");
        Color currentEdge = fuelRenderer.material.GetColor("_EdgeColor");

        Color smoothedFill = Color.Lerp(currentFill, targetFillColor, Time.deltaTime * colorLerpSpeed);
        Color smoothedEdge = Color.Lerp(currentEdge, targetEdgeColor, Time.deltaTime * colorLerpSpeed);

        fuelRenderer.material.SetColor("_FuelColor", smoothedFill);
        fuelRenderer.material.SetColor("_EdgeColor", smoothedEdge);
    }

    private void AnimateOutline()
    {
        if (!fuelText) return;

        // אם האפקט פועל רק מתחת ל־20% דלק
        if (pulseOnlyWhenLow && _fuel > 20f)
        {
            fuelText.outlineWidth = 0f;
            return;
        }

        _outlinePulseTime += Time.deltaTime * outlinePulseSpeed;

        // ערך בין 0 ל־1 בגל סינוסי
        float outlineValue = Mathf.Abs(Mathf.Sin(_outlinePulseTime));
        fuelText.outlineWidth = outlineValue;
    }
}
