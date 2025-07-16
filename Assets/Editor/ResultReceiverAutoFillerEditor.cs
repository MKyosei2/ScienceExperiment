using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ResultReceiverAutoFillerEditor : EditorWindow
{
    [MenuItem("ChemLab/Auto Fill ResultReceiver")]
    public static void ShowWindow()
    {
        GetWindow<ResultReceiverAutoFillerEditor>("Auto Fill ResultReceiver");
    }

    private GameObject resultReceiver;

    void OnGUI()
    {
        GUILayout.Label("ResultReceiver 自動フィールド埋めツール", EditorStyles.boldLabel);

        resultReceiver = (GameObject)EditorGUILayout.ObjectField("ResultReceiver オブジェクト", resultReceiver, typeof(GameObject), true);

        if (resultReceiver == null)
        {
            EditorGUILayout.HelpBox("ResultReceiver をヒエラルキーからドラッグしてください。", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("自動フィールド埋め"))
        {
            var rr = resultReceiver.GetComponent<ResultReceiver>();
            if (rr == null)
            {
                Debug.LogError("このオブジェクトには ResultReceiver スクリプトがありません。");
                return;
            }

            Undo.RecordObject(rr, "Auto Fill ResultReceiver");

            rr.toolObjects = GameObject.FindGameObjectsWithTag("Tool");
            rr.elementObjects = GameObject.FindGameObjectsWithTag("Element");
            rr.conditionObjects = GameObject.FindGameObjectsWithTag("Condition");
            rr.effectProfiles = FindAssets<ShaderEffectData>("t:ShaderEffectData");

            EditorUtility.SetDirty(rr);
            Debug.Log("ResultReceiver のフィールドを自動設定しました。Tool/Element/Condition/EffectProfiles を反映済み。");
        }
    }

    T[] FindAssets<T>(string filter) where T : UnityEngine.Object
    {
        var guids = AssetDatabase.FindAssets(filter);
        var list = new List<T>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) list.Add(asset);
        }
        return list.ToArray();
    }
}