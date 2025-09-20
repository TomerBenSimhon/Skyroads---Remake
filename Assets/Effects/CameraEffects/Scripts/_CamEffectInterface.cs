// CamDelta.cs
public struct CamDelta
{
    public float fovAdd;      // extend later with posAdd/eulerAdd, etc.
}

public interface ICameraEffect
{
    string Tag { get; }
    bool IsFinished { get; }
    void OnStart(CameraEffectsManager ctx);
    CamDelta Tick(float dt);
    void Cancel();
}