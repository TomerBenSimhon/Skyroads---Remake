using UnityEditor;
using UnityEngine;

public class LevelMeshCombinerWindow : EditorWindow
{
    public Transform root;
    public bool includeInactive = false;
    public bool onlyStatic = false;
    public bool perMaterial = true;
    public bool addMeshCollider = false;
    public bool disableOriginalRenderers = true;

    public enum ChunkMode { CountPerChunk, SpatialGrid }
    public ChunkMode chunkMode = ChunkMode.CountPerChunk;

    public int maxPerChunk = 20;
    public float gridCellSize = 50f;

    public string containerName = "COMBINED (Generated)";
    public bool markStatic = true;

    public enum LodHandling { Skip, CombinePerLodPerGroup }
    public LodHandling lodHandling = LodHandling.Skip;

    [MenuItem("Tools/Level Mesh Combiner")]
    public static void ShowWindow()
    {
        var win = GetWindow<LevelMeshCombinerWindow>("Level Mesh Combiner");
        win.minSize = new Vector2(380, 360);
        win.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Source", EditorStyles.boldLabel);
        root = (Transform)EditorGUILayout.ObjectField("Root (optional)", root, typeof(Transform), true);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        onlyStatic = EditorGUILayout.Toggle("Only Static Objects", onlyStatic);

        GUILayout.Space(8);
        GUILayout.Label("Combine Rules", EditorStyles.boldLabel);
        perMaterial = EditorGUILayout.Toggle("Group By Material", perMaterial);
        addMeshCollider = EditorGUILayout.Toggle("Add MeshCollider", addMeshCollider);
        disableOriginalRenderers = EditorGUILayout.Toggle("Disable Originals", disableOriginalRenderers);
        markStatic = EditorGUILayout.Toggle("Mark Combined Static", markStatic);

        GUILayout.Space(8);
        lodHandling = (LodHandling)EditorGUILayout.EnumPopup("LOD Handling", lodHandling);
        EditorGUILayout.HelpBox(
            "Skip: Leaves any LODGroups untouched.\nCombine Per LOD (per group): For each LODGroup, combine meshes inside each LOD level and rebuild a new LODGroup.",
            MessageType.Info
        );

        GUILayout.Space(8);
        chunkMode = (ChunkMode)EditorGUILayout.EnumPopup("Chunking Mode", chunkMode);
        if (chunkMode == ChunkMode.CountPerChunk)
            maxPerChunk = Mathf.Max(1, EditorGUILayout.IntField("Max Per Chunk", maxPerChunk));
        else
            gridCellSize = Mathf.Max(1f, EditorGUILayout.FloatField("Grid Cell Size (m)", gridCellSize));

        GUILayout.Space(10);
        containerName = EditorGUILayout.TextField("Container Name", containerName);

        GUILayout.Space(12);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Combine Selected (or Root/Scene)", GUILayout.Height(30)))
        {
            LevelMeshCombiner.CombineScene(
                root,
                Selection.transforms,
                includeInactive,
                onlyStatic,
                perMaterial,
                chunkMode == ChunkMode.SpatialGrid,
                maxPerChunk,
                gridCellSize,
                addMeshCollider,
                disableOriginalRenderers,
                markStatic,
                containerName,
                (LevelMeshCombiner.LodHandling)lodHandling
            );
        }

        if (GUILayout.Button("Revert Combined (Delete Container)", GUILayout.Height(30)))
        {
            LevelMeshCombiner.RevertCombined(containerName, disableOriginalRenderers);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Tips:\n• Use Group By Material for best batching.\n• For big unique setpieces, keep LODs. This tool respects them if you choose Combine Per LOD (per group).\n• For tile spam, consider CountPerChunk = 20–50.",
            MessageType.None
        );
    }
}
