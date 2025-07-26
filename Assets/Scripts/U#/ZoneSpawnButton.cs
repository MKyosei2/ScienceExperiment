using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// ボタンを押すことで Prefab を指定位置に1つだけ生成し、選択情報を記録する
public class ZoneSpawnButton : UdonSharpBehaviour
{
    [Header("ゾーン種別 (Element / Tool / Condition)")]
    public string objectType; // "Element" など。大文字で！

    [Header("生成するプレハブ")]
    public GameObject spawnPrefab;

    [Header("生成先 (例: ElementExperimentZone)")]
    public Transform spawnZone;

    [Header("データ記録先")]
    public SelectedObjectHolder holder;

    [Header("登録するオブジェクトID (例: H, O, Gasburner)")]
    public string objectID; // ← これが SelectedObjectHolder に登録される

    private bool hasSpawned = false;

    public override void Interact()
    {
        Debug.Log("🧪 ZoneSpawnButton: Interact() 実行");

        // 多重防止
        if (hasSpawned)
        {
            Debug.Log("⛔ Interact() はすでに実行済みです。無視します。");
            return;
        }

        // nullチェック
        if (spawnPrefab == null || spawnZone == null || holder == null)
        {
            Debug.LogError("❌ 必要な設定がされていません");
            return;
        }

        // すでに同名オブジェクトが存在していたら生成スキップ
        string expectedName = objectID + "(Clone)";
        GameObject existing = GameObject.Find(expectedName);
        if (existing != null)
        {
            Debug.Log($"⛔ 既に {expectedName} が存在しているため生成しません");
            return;
        }

        // プレハブ生成
        GameObject instance = VRCInstantiate(spawnPrefab);
        if (instance == null)
        {
            Debug.LogError("❌ VRCInstantiate に失敗しました（Prefabが無効）");
            return;
        }

        instance.transform.SetPositionAndRotation(spawnZone.position, spawnZone.rotation);

        // 名前を強制設定（これが重要！）
        instance.name = objectID + "(Clone)";
        hasSpawned = true;

        // 保険：生成されたPrefabにZoneSpawnButtonがあれば削除
        ZoneSpawnButton zb = instance.GetComponent<ZoneSpawnButton>();
        if (zb != null) Destroy(zb);

        // データ登録
        switch (objectType)
        {
            case "Element":
                holder.AddElement(objectID);
                break;
            case "Tool":
                holder.AddTool(objectID);
                break;
            case "Condition":
                holder.SetCondition(objectID);
                break;
            default:
                Debug.LogWarning($"⚠️ 未対応の objectType: {objectType}");
                break;
        }

        Debug.Log($"✅ {objectType} {objectID} を生成し、名前を {instance.name} に設定、登録完了しました");
    }
}
