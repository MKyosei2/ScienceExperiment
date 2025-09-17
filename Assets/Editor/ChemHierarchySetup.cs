#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ChemHierarchySetup
{
    [MenuItem("ChemLab/Auto Setup Hierarchy Parents")]
    public static void AutoSetupHierarchy()
    {
        ChemEnvironmentManager mgr = Object.FindObjectOfType<ChemEnvironmentManager>();
        if (mgr == null)
        {
            Debug.LogError("ChemEnvironmentManager がシーンに見つかりません。先に配置してください。");
            return;
        }

        Transform mgrRoot = mgr.transform;

        // --- FlaskParent ---
        if (mgr.flaskParent == null)
        {
            GameObject go = new GameObject("FlaskParent");
            go.transform.SetParent(mgrRoot, false);
            mgr.flaskParent = go.transform;
            Debug.Log("FlaskParent を追加しました。");
        }

        // --- LabelParent ---
        if (mgr.labelParent == null)
        {
            GameObject go = new GameObject("LabelParent");
            go.transform.SetParent(mgrRoot, false);
            go.transform.localPosition = new Vector3(0, 2f, 0);
            mgr.labelParent = go.transform;
            Debug.Log("LabelParent を追加しました。");
        }

        // --- AtomsParent ---
        if (mgr.atomsParent == null)
        {
            GameObject go = new GameObject("AtomsParent");
            go.transform.SetParent(mgrRoot, false);
            mgr.atomsParent = go.transform;
            Debug.Log("AtomsParent を追加しました。");
        }

        // --- BondsParent ---
        if (mgr.bondsParent == null)
        {
            GameObject go = new GameObject("BondsParent");
            go.transform.SetParent(mgrRoot, false);
            mgr.bondsParent = go.transform;
            Debug.Log("BondsParent を追加しました。");
        }

        // --- playerView ---
        if (mgr.playerView == null)
        {
            Camera cam = Camera.main;
            if (cam != null) mgr.playerView = cam.transform;
            Debug.Log("playerView に MainCamera を割り当てました。");
        }

        EditorUtility.SetDirty(mgr);
        Debug.Log("ChemEnvironmentManager の Hierarchy セットアップが完了しました！");
    }
}
#endif
