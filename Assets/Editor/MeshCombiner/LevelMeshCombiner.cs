using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LevelMeshCombiner
{
    public enum LodHandling { Skip, CombinePerLodPerGroup }

    public static void CombineScene(
        Transform root,
        Transform[] selection,
        bool includeInactive,
        bool onlyStatic,
        bool perMaterial,
        bool useSpatialGrid,
        int maxPerChunk,
        float gridCellSize,
        bool addMeshCollider,
        bool disableOriginalRenderers,
        bool markStatic,
        string containerName,
        LodHandling lodHandling
    )
    {
        try
        {
            var scope = DetermineScope(root, selection, includeInactive);

            // Create container
            var container = GameObject.Find(containerName);
            if (container == null)
            {
                container = new GameObject(containerName);
                Undo.RegisterCreatedObjectUndo(container, "Create Combined Container");
            }

            // 1) Handle LODGroups
            if (lodHandling == LodHandling.CombinePerLodPerGroup)
            {
                var lodGroups = Object.FindObjectsByType<LODGroup>(
                    includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

                int lgIndex = 0;
                foreach (var lg in lodGroups)
                {
                    lgIndex++;
                    EditorUtility.DisplayProgressBar("Combining LODGroups", $"Processing LODGroup {lgIndex}/{lodGroups.Length}", lgIndex / (float)lodGroups.Length);

                    if (!IsInScope(lg.transform, scope)) continue;
                    if (onlyStatic && !IsAnyStatic(lg.gameObject)) continue;

                    CombineSingleLodGroup(lg, container.transform, perMaterial, useSpatialGrid, maxPerChunk, gridCellSize,
                        addMeshCollider, disableOriginalRenderers, markStatic);
                }
            }

            // 2) Handle non-LOD renderers (or skip if under LODGroup in Skip mode)
            var toCombine = GatherNonLodSources(scope, includeInactive, onlyStatic, lodHandling);
            if (toCombine.Count > 0)
            {
                CombineBatchesIntoChildren(container.transform, toCombine, perMaterial, useSpatialGrid, maxPerChunk,
                    gridCellSize, addMeshCollider, disableOriginalRenderers, markStatic, "NonLOD");
            }

            EditorUtility.ClearProgressBar();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public static void RevertCombined(string containerName, bool reEnableOriginals)
    {
        var container = GameObject.Find(containerName);
        if (container) Undo.DestroyObjectImmediate(container);

        if (reEnableOriginals)
        {
            var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Undo.RecordObjects(allRenderers, "Re-enable Renderers");
            int count = 0;
            foreach (var mr in allRenderers)
            {
                if (!mr.enabled) { mr.enabled = true; count++; }
            }
            Debug.Log($"[LevelMeshCombiner] Re-enabled ~{count} MeshRenderers.");
        }
    }

    // -------- LOD helpers --------

    private static void CombineSingleLodGroup(
        LODGroup sourceGroup,
        Transform container,
        bool perMaterial,
        bool useSpatialGrid,
        int maxPerChunk,
        float gridCellSize,
        bool addMeshCollider,
        bool disableOriginalRenderers,
        bool markStatic)
    {
        var lods = sourceGroup.GetLODs();
        if (lods == null || lods.Length == 0) return;

        // Create a replacement parent for this LODGroup
        var newParent = new GameObject($"COMBINED_LG_{sourceGroup.name}");
        Undo.RegisterCreatedObjectUndo(newParent, "Create Combined LODGroup");
        newParent.transform.SetParent(container, worldPositionStays: true);
        newParent.transform.position = sourceGroup.transform.position;
        newParent.transform.rotation = sourceGroup.transform.rotation;
        newParent.transform.localScale = sourceGroup.transform.lossyScale; // retain world scale
        if (markStatic) GameObjectUtility.SetStaticEditorFlags(newParent, StaticAll());

        var newLodChildren = new List<Renderer>[lods.Length];
        for (int i = 0; i < lods.Length; i++)
        {
            // Collect MeshFilters for this LOD only
            var sources = new List<(MeshFilter mf, MeshRenderer mr)>();
            foreach (var r in lods[i].renderers)
            {
                if (!r) continue;
                // Only MeshRenderer + MeshFilter (skip skinned)
                var mr = r as MeshRenderer;
                if (!mr) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                if (!mf.sharedMesh.isReadable) continue; // skip non-readable
                sources.Add((mf, mr));
            }

            // Combine this LOD level into chunks
            var created = CombineBatchesIntoChildren(newParent.transform, sources, perMaterial, useSpatialGrid, maxPerChunk,
                gridCellSize, addMeshCollider, disableOriginalRenderers, markStatic, $"LOD{i}");

            newLodChildren[i] = new List<Renderer>(created.Count);
            foreach (var go in created)
            {
                var rend = go.GetComponent<MeshRenderer>();
                if (rend) newLodChildren[i].Add(rend);
            }
        }

        // Build new LODGroup with original screen percentages
        var newLG = newParent.AddComponent<LODGroup>();
        var newLods = new LOD[lods.Length];
        for (int i = 0; i < lods.Length; i++)
        {
            newLods[i] = new LOD(lods[i].screenRelativeTransitionHeight, newLodChildren[i].ToArray());
        }
        newLG.SetLODs(newLods);
        newLG.RecalculateBounds();

        // Disable original LOD renderers (and keep the original LODGroup around or disable it)
        if (disableOriginalRenderers)
        {
            foreach (var lod in lods)
            foreach (var r in lod.renderers)
                if (r) r.enabled = false;
        }
        // Optionally disable original group to avoid runtime work
        sourceGroup.enabled = false;
    }

    // Returns created combined GameObjects (children under parent)
    private static List<GameObject> CombineBatchesIntoChildren(
        Transform parent,
        List<(MeshFilter mf, MeshRenderer mr)> sources,
        bool perMaterial,
        bool useSpatialGrid,
        int maxPerChunk,
        float gridCellSize,
        bool addMeshCollider,
        bool disableOriginalRenderers,
        bool markStatic,
        string chunkLabel)
    {
        var results = new List<GameObject>();
        if (sources == null || sources.Count == 0) return results;

        // Group by (material, chunkKey)
        var groups = new Dictionary<(Material mat, string chunkKey), List<(MeshFilter mf, MeshRenderer mr)>>();

        // Stable order
        sources.Sort((a, b) =>
        {
            int matCmp = MaterialName(a.mr).CompareTo(MaterialName(b.mr));
            if (matCmp != 0) return matCmp;
            return a.mf.GetInstanceID().CompareTo(b.mf.GetInstanceID());
        });

        var perMaterialCounters = new Dictionary<Material, int>();

        foreach (var (mf, mr) in sources)
        {
            if (!mf || !mr || !mf.sharedMesh) continue;
            if (!mf.sharedMesh.isReadable) continue;

            Material mat = perMaterial ? mr.sharedMaterial : null;

            string chunkKey;
            if (useSpatialGrid)
            {
                Vector3 wp = mf.transform.position;
                int gx = Mathf.FloorToInt(wp.x / gridCellSize);
                int gy = Mathf.FloorToInt(wp.y / gridCellSize);
                int gz = Mathf.FloorToInt(wp.z / gridCellSize);
                chunkKey = $"{chunkLabel}_grid_{gx}_{gy}_{gz}";
            }
            else
            {
                if (!perMaterialCounters.ContainsKey(mat)) perMaterialCounters[mat] = 0;
                int index = perMaterialCounters[mat] / Mathf.Max(1, maxPerChunk);
                chunkKey = $"{chunkLabel}_count_{index}";
                perMaterialCounters[mat]++;
            }

            var key = (mat, chunkKey);
            if (!groups.ContainsKey(key))
                groups[key] = new List<(MeshFilter, MeshRenderer)>();
            groups[key].Add((mf, mr));
        }

        int built = 0;
        foreach (var kvp in groups)
        {
            built++;
            EditorUtility.DisplayProgressBar("Combining Meshes", $"Building {built}/{groups.Count}", built / (float)groups.Count);

            var mat = kvp.Key.mat;
            var chunk = kvp.Key.chunkKey;
            var list = kvp.Value;

            var combinedGO = new GameObject(BuildCombinedName(mat, chunk));
            Undo.RegisterCreatedObjectUndo(combinedGO, "Create Combined Mesh");
            combinedGO.transform.SetParent(parent, worldPositionStays: true);
            if (markStatic) GameObjectUtility.SetStaticEditorFlags(combinedGO, StaticAll());

            var combinedMF = combinedGO.AddComponent<MeshFilter>();
            var combinedMR = combinedGO.AddComponent<MeshRenderer>();
            if (perMaterial) combinedMR.sharedMaterial = mat;

            var combines = new List<CombineInstance>(list.Count);
            Matrix4x4 worldToCombined = combinedGO.transform.worldToLocalMatrix;

            foreach (var (mf, mr) in list)
            {
                var ci = new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = worldToCombined * mf.transform.localToWorldMatrix,
                    subMeshIndex = 0
                };
                combines.Add(ci);
            }

            var combinedMesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, name = combinedGO.name };
            combinedMesh.CombineMeshes(combines.ToArray(), true, true, false);
            combinedMesh.RecalculateBounds();

            combinedMF.sharedMesh = combinedMesh;

            if (addMeshCollider)
            {
                var mc = combinedGO.AddComponent<MeshCollider>();
                mc.sharedMesh = combinedMesh;
            }

            if (disableOriginalRenderers)
            {
                foreach (var (_, mr) in list)
                {
                    Undo.RecordObject(mr, "Disable Original Renderer");
                    mr.enabled = false;
                }
            }

            results.Add(combinedGO);
        }

        EditorUtility.ClearProgressBar();
        return results;
    }

    // -------- Collection & utility --------

    private static (HashSet<Transform> roots, bool includeInactive) DetermineScope(Transform root, Transform[] selection, bool includeInactive)
    {
        var set = new HashSet<Transform>();
        if (selection != null && selection.Length > 0)
        {
            foreach (var t in selection) if (t) set.Add(t);
        }
        else if (root)
        {
            set.Add(root);
        }
        else
        {
            // Whole scene: use all active scene roots
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                set.Add(go.transform);
        }
        return (set, includeInactive);
    }

    private static bool IsInScope(Transform t, (HashSet<Transform> roots, bool includeInactive) scope)
    {
        foreach (var r in scope.roots)
            if (t == r || t.IsChildOf(r)) return true;
        return false;
    }

    private static List<(MeshFilter mf, MeshRenderer mr)> GatherNonLodSources(
        (HashSet<Transform> roots, bool includeInactive) scope,
        bool includeInactive,
        bool onlyStatic,
        LodHandling lodHandling)
    {
        var list = new List<(MeshFilter, MeshRenderer)>();
        var mfs = Object.FindObjectsByType<MeshFilter>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var mf in mfs)
        {
            if (!mf || !mf.sharedMesh) continue;
            if (!IsInScope(mf.transform, scope)) continue;

            // Skip anything under an LODGroup if we are skipping LODs (they're handled above if CombinePerLodPerGroup)
            if (lodHandling == LodHandling.Skip && mf.GetComponentInParent<LODGroup>(true)) continue;

            var mr = mf.GetComponent<MeshRenderer>();
            if (!mr) continue;
            if (mf.GetComponent<SkinnedMeshRenderer>()) continue;
            if (onlyStatic && !IsAnyStatic(mf.gameObject)) continue;

            if (!mf.sharedMesh.isReadable) continue;

            list.Add((mf, mr));
        }

        return list;
    }

    private static StaticEditorFlags StaticAll()
    {
        return StaticEditorFlags.BatchingStatic |
               StaticEditorFlags.NavigationStatic |
               StaticEditorFlags.OccludeeStatic |
               StaticEditorFlags.OccluderStatic |
               StaticEditorFlags.OffMeshLinkGeneration |
               StaticEditorFlags.ReflectionProbeStatic;
    }

    private static bool IsAnyStatic(GameObject go)
    {
        var flags = GameObjectUtility.GetStaticEditorFlags(go);
        return (flags & (StaticEditorFlags.BatchingStatic |
                         StaticEditorFlags.OccludeeStatic |
                         StaticEditorFlags.OccluderStatic |
                         StaticEditorFlags.ReflectionProbeStatic)) != 0;
    }

    private static string MaterialName(MeshRenderer mr)
    {
        var m = mr ? mr.sharedMaterial : null;
        return m ? m.name : "NO_MATERIAL";
    }

    private static string BuildCombinedName(Material mat, string chunkKey)
    {
        var matName = mat ? mat.name : "Mixed";
        return $"Combined_{matName}_{chunkKey}";
    }
}
