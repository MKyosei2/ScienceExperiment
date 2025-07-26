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
        // 多重防止
        if (hasSpawned)
        {
            Debug.Log("⛔ Interact() はすでに実行済みです。無視します。");
            return;
        }
        hasSpawned = true;

        // nullチェック
        if (spawnPrefab == null || spawnZone == null || holder == null)
        {
            Debug.LogError("❌ 必要な設定がされていません");
            return;
        }

        // 同じ名前のオブジェクトがすでにSceneに存在していたらスキップ
        string expectedName = spawnPrefab.name + "(Clone)";
        GameObject existing = GameObject.Find(expectedName);
        if (existing != null)
        {
            Debug.Log($"⛔ 既に {expectedName} が存在しているため生成しません");
            return;
        }

        // プレハブ生成
        GameObject instance = VRCInstantiate(spawnPrefab);
        instance.transform.SetPositionAndRotation(spawnZone.position, spawnZone.rotation);

        // 保険：生成されたPrefabにZoneSpawnButtonがあれば削除
        ZoneSpawnButton zb = instance.GetComponent<ZoneSpawnButton>();
        if (zb != null) Destroy(zb);

        // データ登録
        if (objectType == "Element")
        {
            holder.AddElement(objectID);
        }
        else if (objectType == "Tool")
        {
            holder.AddTool(objectID);
        }
        else if (objectType == "Condition")
        {
            holder.SetCondition(objectID);
        }

        Debug.Log($"✅ {objectType} {objectID} を生成して登録しました。");
    }
}
