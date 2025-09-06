// ParticleEffectsManager.cs
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)] // ensure anchors can register first
public class ParticleEffectsManager : MonoBehaviour
{
    public static ParticleEffectsManager I { get; private set; }

    // (Owner, AnchorId) => list of anchors
    private readonly Dictionary<GameObject, Dictionary<string, List<ParticleAnchor>>> _byOwner =
        new(new ReferenceEqualityComparer());

    // Global (no owner) anchors
    private readonly Dictionary<string, List<ParticleAnchor>> _global = new();

    // Track running loops so we can cancel by tag
    private class RunningLoop
    {
        public string tag;
        public GameObject owner; // null for global
        public ParticleAnchor anchor;
        public ParticleEffectSpec spec;
    }
    private readonly List<RunningLoop> _activeLoops = new();

    // Cooldowns: (spec, owner) -> last fire time
    private readonly Dictionary<(ParticleEffectSpec, GameObject), float> _lastPlayTime = new();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // ——— Anchor registration ———

    public void Register(ParticleAnchor anchor)
    {
        if (anchor == null) return;
        var owner = anchor.Owner;

        if (owner == null)
        {
            if (!_global.TryGetValue(anchor.AnchorId, out var list))
                _global[anchor.AnchorId] = list = new List<ParticleAnchor>();
            if (!list.Contains(anchor)) list.Add(anchor);
        }
        else
        {
            if (!_byOwner.TryGetValue(owner, out var perOwner))
                _byOwner[owner] = perOwner = new Dictionary<string, List<ParticleAnchor>>();

            if (!perOwner.TryGetValue(anchor.AnchorId, out var list))
                perOwner[anchor.AnchorId] = list = new List<ParticleAnchor>();

            if (!list.Contains(anchor)) list.Add(anchor);
        }
    }

    public void Unregister(ParticleAnchor anchor)
    {
        if (anchor == null) return;
        var owner = anchor.Owner;

        if (owner == null)
        {
            if (_global.TryGetValue(anchor.AnchorId, out var list))
                list.Remove(anchor);
        }
        else if (_byOwner.TryGetValue(owner, out var perOwner)
              && perOwner.TryGetValue(anchor.AnchorId, out var list))
        {
            list.Remove(anchor);
        }

        // Also stop any loops on this anchor
        for (int i = _activeLoops.Count - 1; i >= 0; i--)
        {
            if (_activeLoops[i].anchor == anchor)
                _activeLoops.RemoveAt(i);
        }
    }

    // ——— Public API ———

    public void Play(ParticleEffectSpec spec, EffectCall call)
    {
        if (spec == null) return;

        var owner = ResolveOwner(spec.Scope, call);
        if (spec.CooldownSeconds > 0f)
        {
            var key = (spec, owner);
            if (_lastPlayTime.TryGetValue(key, out var last)
                && Time.time < last + spec.CooldownSeconds)
                return; // still on cooldown
            _lastPlayTime[key] = Time.time;
        }

        // Replace existing loops with same tag (owner-scoped) if requested
        if (spec.ReplaceExisting && spec.PlayMode == ParticlePlayMode.Loop)
            CancelTag(spec.Tag, owner, spec.StopMode);

        // Resolve anchors
        var anchors = ResolveAnchors(spec.AnchorId, owner);
        if (anchors == null || anchors.Count == 0) return;

        spec.ComputeMultipliers(call.magnitude, out var sizeMul, out var speedMul, out var rateMul);

        foreach (var anchor in anchors)
        {
            if (spec.PlayMode == ParticlePlayMode.OneShot)
            {
                anchor.PlayOneShot(sizeMul, speedMul, rateMul);
            }
            else
            {
                // Respect max active per owner
                if (CountActiveLoops(spec.Tag, owner) >= Mathf.Max(1, spec.MaxActivePerOwner))
                    continue;

                anchor.StartLoop(sizeMul, speedMul, rateMul, spec.RestartLoopIfAlreadyPlaying);

                _activeLoops.Add(new RunningLoop
                {
                    tag = spec.Tag,
                    owner = owner,
                    anchor = anchor,
                    spec = spec
                });
            }
        }
    }

    public void CancelTag(string tag, GameObject owner = null, ParticleStopMode stopMode = ParticleStopMode.StopEmitting)
    {
        if (string.IsNullOrEmpty(tag)) return;

        for (int i = _activeLoops.Count - 1; i >= 0; i--)
        {
            var loop = _activeLoops[i];
            if (loop.tag == tag && loop.owner == owner)
            {
                loop.anchor.StopLoop(stopMode);
                _activeLoops.RemoveAt(i);
            }
        }
    }

    // Overload to match your Effects.Cancel() pattern
    public void Cancel(ParticleEffectSpec spec, EffectCall call)
    {
        var owner = ResolveOwner(spec.Scope, call);
        CancelTag(spec.Tag, owner, spec.StopMode);
    }

    // ——— Helpers ———

    private int CountActiveLoops(string tag, GameObject owner)
    {
        int c = 0;
        for (int i = 0; i < _activeLoops.Count; i++)
            if (_activeLoops[i].tag == tag && _activeLoops[i].owner == owner)
                c++;
        return c;
    }

    private List<ParticleAnchor> ResolveAnchors(string anchorId, GameObject owner)
    {
        if (string.IsNullOrEmpty(anchorId)) return null;

        if (owner == null)
        {
            _global.TryGetValue(anchorId, out var list);
            return list;
        }
        if (_byOwner.TryGetValue(owner, out var perOwner)
            && perOwner.TryGetValue(anchorId, out var owned))
            return owned;

        // Fallback: if owner-scoped not found, you can decide to fall back to global
        _global.TryGetValue(anchorId, out var globalList);
        return globalList;
    }

    private static GameObject ResolveOwner(ParticleScope scope, EffectCall call)
    {
        return scope switch
        {
            ParticleScope.Global    => null,
            ParticleScope.UseSource => call.source.gameObject,
            ParticleScope.UseTarget => call.target.gameObject,
            _ => null
        };
    }

    // Reference equality comparer for owner keys
    private sealed class ReferenceEqualityComparer : IEqualityComparer<GameObject>
    {
        public bool Equals(GameObject x, GameObject y) => ReferenceEquals(x, y);
        public int GetHashCode(GameObject obj) => obj ? obj.GetHashCode() : 0;
    }
}
