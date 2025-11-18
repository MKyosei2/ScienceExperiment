#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class AutoFixFlask : EditorWindow
{
    [MenuItem("VRC ChemLab/FORCE FIX Flask In Scene")]
    public static void ForceFix()
    {
        // シーン内の CONICAL_FLASK をすべて取得
        GameObject[] flasks = GameObject.FindObjectsOfType<GameObject>();

        GameObject target = null;

        foreach (GameObject go in flasks)
        {
            if (go.name == "CONICAL_FLASK")
            {
                target = go;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogError("[FORCE FIX] Scene 内に CONICAL_FLASK が存在しません。");
            return;
        }

        Debug.Log("[FORCE FIX] Scene 内の CONICAL_FLASK を強制修正開始");

        // ========== BODY 修正 ==========
        Transform body = target.transform.Find("Body");
        Transform wire = target.transform.Find("WireframeShell");

        if (body == null)
        {
            Debug.LogError("[FORCE FIX] Body が見つからないため作成します。");
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = "Body";
            b.transform.SetParent(target.transform, false);
            body = b.transform;
        }

        // MeshRenderer の強制取得
        MeshRenderer bodyMR = body.GetComponent<MeshRenderer>();
        if (bodyMR == null) bodyMR = body.gameObject.AddComponent<MeshRenderer>();

        // MeshFilter コピー
        MeshFilter wireMF = null;
        if (wire != null)
            wireMF = wire.GetComponent<MeshFilter>();

        MeshFilter bodyMF = body.GetComponent<MeshFilter>();
        if (wireMF != null)
        {
            bodyMF.sharedMesh = wireMF.sharedMesh;
            Debug.Log("[FORCE FIX] Body Mesh を WireframeShell と同じにしました。");
        }

        // Body の Material を透明
        Material insideMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/InsideContent.mat");
        if (insideMat != null)
        {
            bodyMR.sharedMaterial = insideMat;
            Color c = insideMat.color;
            c.a = 0.05f;
            insideMat.color = c;

            Debug.Log("[FORCE FIX] Body の Material を InsideContent に更新しました。");
        }

        // Body を最優先に移動
        body.SetSiblingIndex(0);

        // Collider 削除
        foreach (Collider col in body.GetComponents<Collider>())
            GameObject.DestroyImmediate(col);

        Debug.Log("[FORCE FIX] Body Collider を削除");

        // Particle System 調整
        Transform ps = target.transform.Find("Particle System");
        if (ps != null)
        {
            ps.localPosition = new Vector3(0, 0.8f, 0);
            ps.localScale = Vector3.one * 0.5f;
        }

        // spawner に自動登録
        var spawner = FindObjectOfType<ChemElementSpawner>();
        if (spawner != null)
        {
            spawner.sourceVessel = target;
            Debug.Log("[FORCE FIX] ChemElementSpawner に CONICAL_FLASK を自動設定");
        }

        Debug.Log("[FORCE FIX] === 完全修正完了 ===");
    }
}
#endif