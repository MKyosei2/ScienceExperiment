#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class CONICALFlaskAutoFix : EditorWindow
{
    [MenuItem("VRC ChemLab/Fix: Force Body Renderer First")]
    public static void FixOrder()
    {
        GameObject flask = GameObject.Find("CONICAL_FLASK");

        if (flask == null)
        {
            Debug.LogError("[FixBodyOrder] CONICAL_FLASK が見つかりません");
            return;
        }

        Transform body = flask.transform.Find("Body");
        if (body == null)
        {
            Debug.LogError("[FixBodyOrder] Body が存在しません");
            return;
        }

        // Body を子階層の一番上に移動（MeshRenderer の優先順を確定）
        body.SetSiblingIndex(0);

        Debug.Log("[FixBodyOrder] Body を最優先の子へ移動しました（Shader が確実に適用されます）");
    }
}
#endif