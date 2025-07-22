// Assets/Editor/AutoAssignElements.cs

using UnityEngine;
using UnityEditor;

public class AutoAssignElements : EditorWindow
{
    [MenuItem("CHEMLAB/自動登録/Element候補をExperimentControllerに登録")]
    public static void AssignElements()
    {
        var controller = FindObjectOfType<ExperimentController>();
        if (controller == null)
        {
            Debug.LogError("ExperimentController がシーンに見つかりません。");
            return;
        }

        GameObject elementRoot = GameObject.Find("Element");
        if (elementRoot == null)
        {
            Debug.LogError("Hierarchy に 'Element' という名前のルートオブジェクトが見つかりません。");
            return;
        }

        var elementList = new System.Collections.Generic.List<GameObject>();

        foreach (Transform group in elementRoot.transform)
        {
            foreach (Transform element in group)
            {
                if (element.gameObject != null)
                {
                    elementList.Add(element.gameObject);
                }
            }
        }

        controller.candidateElements = elementList.ToArray();
        EditorUtility.SetDirty(controller);
        Debug.Log($"✅ Element候補として {elementList.Count} 個を登録しました。");
    }
}
