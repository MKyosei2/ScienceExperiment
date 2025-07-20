using UnityEditor;
using UnityEngine;

public class ResizeToUnityCube : MonoBehaviour
{
    [MenuItem("Tools/Resize To Unity Cube Height and Center")]
    static void ResizeSelectedObjects()
    {
        float targetHeight = 1.0f; // Unity Cubeの高さに揃える

        foreach (GameObject obj in Selection.gameObjects)
        {
            Bounds bounds = GetCombinedBounds(obj);
            if (bounds.size == Vector3.zero)
            {
                Debug.LogWarning("Rendererが見つかりません: " + obj.name);
                continue;
            }

            float currentHeight = bounds.size.y;
            if (currentHeight <= 0.0001f)
            {
                Debug.LogWarning("高さが極端に小さい: " + obj.name);
                continue;
            }

            // スケーリング倍率を計算して適用
            float scaleFactor = targetHeight / currentHeight;
            obj.transform.localScale = obj.transform.localScale * scaleFactor;

            // スケーリング後のBounds再取得（正しい中心を得るため）
            bounds = GetCombinedBounds(obj);

            // バウンディングボックスの中心を原点に合わせるよう位置を調整
            Vector3 offset = bounds.center - obj.transform.position;
            obj.transform.position -= offset;

            Debug.Log(obj.name + " を高さ1にスケーリングし、中心を原点に移動しました。倍率: " + scaleFactor);
        }
    }

    static Bounds GetCombinedBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }
        return bounds;
    }
}
