// EffectCall.cs
using UnityEngine;

public struct EffectCall
{
    public Transform source;     // The object that owns the Effects component
    public Transform target;     // Optional (e.g., player)
    public Vector3 position;     // Usually source.position
    public float magnitude;      // Intensity scaling from gameplay (1 = default)
}