// ParticlePlayTypes.cs
using UnityEngine;

public enum ParticlePlayMode { OneShot, Loop }
public enum ParticleStopMode { StopEmitting, Immediate }

// Which “owner” to resolve anchors against (matches your EffectCall context)
public enum ParticleScope
{
    Global,     // ignore owner; play all anchors with AnchorId
    UseSource,  // use call.source as owner
    UseTarget   // use call.target as owner
}