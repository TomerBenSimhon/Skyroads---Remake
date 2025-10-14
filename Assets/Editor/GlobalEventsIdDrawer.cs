#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GlobalEvents.Id))]
public class GlobalEventsIdDrawer : PropertyDrawer
{
    // ---- GROUP DEFINITIONS (edit here if you add new events) ----
    private static readonly string[] Player = {
        nameof(GlobalEvents.Id.PlayerOnStart),
        nameof(GlobalEvents.Id.PlayerJumped),
        nameof(GlobalEvents.Id.PlayerFired),
        nameof(GlobalEvents.Id.PlayerGrounded),
        nameof(GlobalEvents.Id.PlayerAirborne),
        nameof(GlobalEvents.Id.PlayerDied),
        nameof(GlobalEvents.Id.PlayerRespawned),
        nameof(GlobalEvents.Id.PlayerBroken),
    };

    private static readonly string[] Special = {
        nameof(GlobalEvents.Id.PowerUpApplied),
        nameof(GlobalEvents.Id.FixApplied),
        nameof(GlobalEvents.Id.CoilActivated),
        nameof(GlobalEvents.Id.CoilDeactivated),
    };

    private static readonly string[] Platform = {
        nameof(GlobalEvents.Id.BoostApplied),
        nameof(GlobalEvents.Id.BoostRemoved),
        nameof(GlobalEvents.Id.RefuelApplied),
        nameof(GlobalEvents.Id.RefuelRemoved),
        nameof(GlobalEvents.Id.SlipperyApplied),
        nameof(GlobalEvents.Id.SlipperyRemoved),
        nameof(GlobalEvents.Id.OnBarrierBreak),
        
    };

    private static readonly string[] Triggers = {
        nameof(GlobalEvents.Id.OnEventTriggered),
        nameof(GlobalEvents.Id.CheckpointTriggered)
    };

    private static readonly (string header, string[] names)[] Groups = {
        ("Player",   Player),
        ("Special",   Special),
        ("Platform", Platform),
        ("Triggers",   Triggers),
    };

    // cache enum name -> value
    private static Dictionary<string, int> _nameToValue;
    private static Dictionary<int, string> _valueToName;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureEnumMaps();

        // Read current mask (use long to be safe; your enum fits in 32 bits today)
        long current = property.hasMultipleDifferentValues ? 0 : property.longValue;

        // Button that opens our custom popup
        var btnRect = EditorGUI.PrefixLabel(position, label);
        if (EditorGUI.DropdownButton(btnRect, new GUIContent(MaskSummary((int)current)), FocusType.Keyboard))
        {
            var menu = BuildMenu((int)current, newMask =>
            {
                property.longValue = newMask;
                property.serializedObject.ApplyModifiedProperties();
            });

            // show menu under field
            menu.DropDown(btnRect);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    // ----- Helpers -----

    private static void EnsureEnumMaps()
    {
        if (_nameToValue != null) return;

        _nameToValue = new Dictionary<string, int>(StringComparer.Ordinal);
        _valueToName = new Dictionary<int, string>();

        foreach (var name in Enum.GetNames(typeof(GlobalEvents.Id)))
        {
            var value = (int)Enum.Parse(typeof(GlobalEvents.Id), name);
            _nameToValue[name] = value;
            _valueToName[value] = name;
        }
    }

    private static GenericMenu BuildMenu(int currentMask, Action<int> onChanged)
    {
        var menu = new GenericMenu();

        // Global quick actions
        menu.AddItem(new GUIContent("All"), IsAll(currentMask), () => onChanged(ComputeAllMask()));
        menu.AddItem(new GUIContent("None"), currentMask == 0, () => onChanged(0));
        menu.AddSeparator("");

        // Groups
        foreach (var (header, names) in Groups)
        {
            // Per-group quick actions
            int groupMask = MaskFromNames(names);
            bool groupAll = (currentMask & groupMask) == groupMask;
            bool groupNone = (currentMask & groupMask) == 0;

            menu.AddItem(new GUIContent($"{header}/All"), groupAll, () =>
            {
                int newMask = (currentMask | groupMask);
                onChanged(newMask);
            });
            menu.AddItem(new GUIContent($"{header}/None"), groupNone, () =>
            {
                int newMask = (currentMask & ~groupMask);
                onChanged(newMask);
            });

            // Items
            foreach (var n in names)
            {
                if (!_nameToValue.TryGetValue(n, out var v)) continue;
                bool on = (currentMask & v) != 0;
                menu.AddItem(new GUIContent($"{header}/{n}"), on, () =>
                {
                    int newMask = on ? (currentMask & ~v) : (currentMask | v);
                    onChanged(newMask);
                });
            }

            menu.AddSeparator("");
        }

        // Ungrouped (new enums you might add later but forgot to map)
        var ungrouped = CollectUngrouped();
        if (ungrouped.Count > 0)
        {
            menu.AddDisabledItem(new GUIContent("UNGROUPED"));
            foreach (var (n, v) in ungrouped)
            {
                bool on = (currentMask & v) != 0;
                menu.AddItem(new GUIContent($"Ungrouped/{n}"), on, () =>
                {
                    int newMask = on ? (currentMask & ~v) : (currentMask | v);
                    onChanged(newMask);
                });
            }
        }

        return menu;
    }

    private static bool IsAll(int mask) => mask == ComputeAllMask();

    private static int ComputeAllMask()
    {
        int m = 0;
        foreach (var kv in _nameToValue) m |= kv.Value;
        return m;
    }

    private static int MaskFromNames(string[] names)
    {
        int m = 0;
        foreach (var n in names)
            if (_nameToValue.TryGetValue(n, out var v)) m |= v;
        return m;
    }

    private static List<(string name, int value)> CollectUngrouped()
    {
        var grouped = new HashSet<string>(StringComparer.Ordinal);
        foreach (var g in Groups)
            foreach (var n in g.names) grouped.Add(n);

        var list = new List<(string, int)>();
        foreach (var kv in _nameToValue)
        {
            if (kv.Key == nameof(GlobalEvents.Id.None)) continue;
            if (!grouped.Contains(kv.Key))
                list.Add((kv.Key, kv.Value));
        }
        return list;
    }

    private static string MaskSummary(int mask)
    {
        if (mask == 0) return "None";
        if (mask == ComputeAllMask()) return "All";

        // Show up to 3 names; then "+N"
        var names = new List<string>();
        foreach (var kv in _nameToValue)
        {
            int v = kv.Value;
            if (v == 0) continue;
            if ((mask & v) != 0) names.Add(kv.Key);
        }
        names.Sort(); // stable order
        if (names.Count <= 3) return string.Join(", ", names);
        return $"{names[0]}, {names[1]}, {names[2]}  +{names.Count - 3}";
    }
}
#endif
