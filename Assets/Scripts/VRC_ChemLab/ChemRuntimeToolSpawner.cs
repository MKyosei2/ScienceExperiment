using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// ChemRuntimeToolSpawner
/// - Inspectorで設定された器具Prefabを VRCInstantiate で生成し、指定親(Zone)の子として配置する。
/// - Prefab未設定/未一致の場合は null を返し、Spawner 側の既存モデルロジックにフォールバックさせる。
///
/// ★重要
/// ここは「enablePrefabSpawn のON/OFFに依存しない」実装に変更。
/// ユーザーが Inspector で toolPrefabs を設定している限り、同名(またはtoolIds一致)のPrefabを必ず優先します。
/// </summary>
public class ChemRuntimeToolSpawner : UdonSharpBehaviour
{
    [Header("Enable (Deprecated)")]
    [Tooltip("互換用。toolPrefabs が設定されている場合は、この値に関わらずPrefabスポーンを優先します。")]
    public bool enablePrefabSpawn = false;

    [Header("Tool Prefabs")]
    [Tooltip("生成できる器具のPrefab(またはFBX)を登録。IDが空なら prefab.name で照合します。")]
    public GameObject[] toolPrefabs;

    [Tooltip("toolPrefabs と同じ長さのID配列（例: BEAKER, CONICAL_FLASK）。未設定なら prefab.name を使います。")]
    public string[] toolIds;

    [Header("Spawned Instance (runtime)")]
    public bool destroyPrevious = true;

    private GameObject _spawned;
    private string _spawnedId;

    public Transform GetSpawnedTransform()
    {
        return _spawned != null ? _spawned.transform : null;
    }

    public string GetSpawnedId()
    {
        return _spawnedId;
    }

    public bool HasPrefab(string toolIdNorm)
    {
        if (string.IsNullOrEmpty(toolIdNorm)) return false;
        return FindPrefab(toolIdNorm) != null;
    }

    /// <summary>
    /// toolIdNorm（既にNormalize済みID）に一致するPrefabがあれば必ずスポーンします。
    /// 見つからなければ null を返します。
    /// </summary>
    public Transform SpawnTool(string toolIdNorm, Transform parent, Vector3 worldOffset)
    {
        if (parent == null) return null;
        if (string.IsNullOrEmpty(toolIdNorm)) return null;

        // InspectorでPrefabが設定されていないならスポーン不可
        if (toolPrefabs == null || toolPrefabs.Length == 0) return null;

        // same tool already spawned
        if (_spawned != null && !string.IsNullOrEmpty(_spawnedId) && _spawnedId == toolIdNorm)
        {
            if (_spawned.transform.parent != parent) _spawned.transform.SetParent(parent, true);
            _spawned.transform.position = parent.position + worldOffset;
            _spawned.transform.rotation = parent.rotation;
            if (!_spawned.activeSelf) _spawned.SetActive(true);
            return _spawned.transform;
        }

        GameObject prefab = FindPrefab(toolIdNorm);
        if (prefab == null) return null;

        // cleanup previous
        if (_spawned != null)
        {
            if (destroyPrevious)
            {
                Object.Destroy(_spawned);
            }
            else
            {
                _spawned.SetActive(false);
            }
            _spawned = null;
            _spawnedId = null;
        }

        // spawn
        GameObject go = VRCInstantiate(prefab);
        if (go == null) return null;

        go.transform.SetParent(parent, true);
        go.transform.position = parent.position + worldOffset;
        go.transform.rotation = parent.rotation;
        if (!go.activeSelf) go.SetActive(true);

        _spawned = go;
        _spawnedId = toolIdNorm;
        return go.transform;
    }

    private GameObject FindPrefab(string toolIdNorm)
    {
        if (toolPrefabs == null || toolPrefabs.Length == 0) return null;

        int n = toolPrefabs.Length;
        for (int i = 0; i < n; i++)
        {
            GameObject p = toolPrefabs[i];
            if (p == null) continue;

            string id = null;
            if (toolIds != null && i < toolIds.Length) id = toolIds[i];

            // ★同名運用を最優先
            if (string.IsNullOrEmpty(id)) id = p.name;

            if (NormalizeId(id) == toolIdNorm) return p;

            // 念のため：toolIds が設定されている場合でも prefab.name に一致するなら採用
            if (NormalizeId(p.name) == toolIdNorm) return p;
        }
        return null;
    }

    private string NormalizeId(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim();
        s = s.Replace(" ", "_").Replace("-", "_").ToUpperInvariant();
        return s;
    }
}
