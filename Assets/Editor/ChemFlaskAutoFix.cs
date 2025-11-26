#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LiquidFlaskAutoFix : EditorWindow
{
    [MenuItem("VRC ChemLab/Auto Fix LiquidSurface")]
    static void FixAllLiquidSurfaces()
    {
        Debug.Log("=== ChemLab LiquidSurface AutoFix Started ===");

        var all = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (var obj in all)
        {
            // フラスコ以外は触らない
            if (!obj.name.Contains("CONICAL_FLASK"))
                continue;

            Transform surfaceTr = obj.transform.Find("LiquidSurface");

            // 1. なければ生成
            if (surfaceTr == null)
            {
                GameObject surf = GameObject.CreatePrimitive(PrimitiveType.Quad);
                surf.name = "LiquidSurface";
                surf.transform.SetParent(obj.transform, false);

                // 表示位置（フラスコ上部付近）
                surf.transform.localPosition = new Vector3(0, 0.4f, 0);
                surf.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                // Renderer 最適化
                MeshRenderer mr = surf.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                surfaceTr = surf.transform;

                Debug.Log("[ChemLab] LiquidSurface created in " + obj.name);
            }

            // 2. Renderer と Material
            MeshRenderer renderer = surfaceTr.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = surfaceTr.gameObject.AddComponent<MeshRenderer>();
            }

            Material mat = renderer.sharedMaterial;

            // 3. Shader が LiquidSurface.shader でなければ修正
            if (mat == null || mat.shader == null || mat.shader.name != "VRC_ChemLab/LiquidSurface")
            {
                mat = new Material(Shader.Find("VRC_ChemLab/LiquidSurface"));
                renderer.sharedMaterial = mat;
                Debug.Log("[ChemLab] Assigned LiquidSurface Shader to " + obj.name);
            }

            // 4. _RippleStrength が存在するかチェック
            if (!mat.HasProperty("_RippleStrength"))
            {
                // Shaderが正しければ必ず存在するが念のため
                Debug.LogWarning("[ChemLab] WARNING: LiquidSurface shader missing _RippleStrength in " + obj.name);
            }

            // 5. RenderQueue / ZWrite 最適化
            mat.renderQueue = 3105;
            mat.SetInt("_ZWrite", 0);

            // 6. LiquidSurfaceController の付与
            LiquidSurfaceController controller = surfaceTr.GetComponent<LiquidSurfaceController>();
            if (controller == null)
            {
                controller = surfaceTr.gameObject.AddComponent<LiquidSurfaceController>();
                controller.liquidSurface = renderer;
                Debug.Log("[ChemLab] Added LiquidSurfaceController to " + obj.name);
            }
            else
            {
                controller.liquidSurface = renderer;
            }
        }

        Debug.Log("=== ChemLab LiquidSurface AutoFix Completed ===");
    }
}
#endif