// ChemElementSpawner.cs
// UdonSharp制約対応：Instantiate/AddComponent禁止。事前に用意したオブジェクト群を有効化するだけ。

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
#if CHEM_RUNTIME
using UdonSharp;
#endif

#if CHEM_RUNTIME
public class ChemElementSpawner : UdonSharpBehaviour
#else
public class ChemElementSpawner : MonoBehaviour
#endif
{
    [Header("Pool（事前にHierarchyへ配置し非表示）")]
    public GameObject[] spawnPool;            // ここにChemVisualController付きのオブジェクトを並べておく
    public ChemEnvironmentManager envManager; // Manager参照（必須）
    public string elementId = "C";            // 追加時の初期元素
    private int cursor = 0;

    public void SpawnOne()
    {
        int i = NextInactiveIndex();
        if (i < 0) return; // プール満杯

        GameObject go = spawnPool[i];
        go.SetActive(true);

        // 事前付与の ChemVisualController を利用（AddComponent禁止）
        ChemVisualController ctrl = go.GetComponent<ChemVisualController>();
        if (ctrl != null)
        {
            ctrl.env = envManager;
            ctrl.SetElementId(elementId);
        }

        if (envManager != null) envManager.Relayout("auto");
    }

    private int NextInactiveIndex()
    {
        int n = spawnPool != null ? spawnPool.Length : 0;
        int k = 0;
        for (k = 0; k < n; k++)
        {
            int idx = (cursor + k) % n;
            if (spawnPool[idx] != null && !spawnPool[idx].activeSelf) { cursor = (idx + 1) % n; return idx; }
        }
        return -1;
    }
}
