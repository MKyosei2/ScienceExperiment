// ChemElementSpawner.cs
// 元素ボタン用スパウナ：CONICAL_FLASK を ExperimentTable に複数生成し、元素IDと環境参照を付与

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
    [Header("生成プレハブ")]
    public GameObject conicalFlaskPrefab;

    [Header("配置先（ExperimentTable）")]
    public Transform experimentTable;

    [Header("環境マネージャ参照（シーン上の1つ）")]
    public ChemEnvironmentManager envManager;

    [Header("配置オフセット")]
    public Vector3 localPositionOffset = Vector3.zero;
    public Vector3 localEulerOffset = Vector3.zero;

    // Udon ボタン等から呼ぶ
    public void SpawnElementWithId(string elementId)
    {
        if (conicalFlaskPrefab == null || experimentTable == null) return;

        var go = Instantiate(conicalFlaskPrefab);
        go.transform.SetParent(experimentTable, false);
        go.transform.localPosition = localPositionOffset;
        go.transform.localEulerAngles = localEulerOffset;

        var ctrl = go.GetComponent<ChemVisualController>();
        if (ctrl != null)
        {
            ctrl.env = envManager;          // マネージャを付与（static禁止対策）
            ctrl.SetElementId(elementId);   // 記号を設定→即時シェーダ更新
            ctrl.ApplyToShaders();
        }
    }
}
