// CamDelta.cs
public struct CamDelta
{
    public float  fovAdd;     // existing
    public UnityEngine.Vector3 posAdd;    // NEW: local-position additive (x,y,z)
    public UnityEngine.Vector3 eulerAdd;  // NEW: local-rotation additive in degrees (pitch,x / yaw,y / roll,z)

    public static CamDelta Zero => new CamDelta { fovAdd = 0f, posAdd = default, eulerAdd = default };
}

public interface ICameraEffect
{
    string Tag { get; }
    bool IsFinished { get; }
    void OnStart(CameraEffectsManager ctx);
    CamDelta Tick(float dt);
    void Cancel();
}