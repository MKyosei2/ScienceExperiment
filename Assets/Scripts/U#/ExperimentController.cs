using UdonSharp;
using UnityEngine;

public class ExperimentController : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public AIRequestSender requestSender;
    public ModeSwitcher modeSwitcher;

    [Header("Element候補")]
    public GameObject[] candidateElements;
    [Header("Tool候補")]
    public GameObject[] candidateTools;
    [Header("Condition候補")]
    public GameObject[] candidateConditions;

    public void RunExperiment()
    {
        if (holder == null || requestSender == null || modeSwitcher == null)
        {
            Debug.LogWarning("❌ 必要な参照が不足しています");
            return;
        }

        CollectFromTable();

        string elementID = holder.selectedElementID;
        string toolID = holder.selectedToolID;
        string conditionID = holder.selectedConditionID;

        if (string.IsNullOrEmpty(elementID) || string.IsNullOrEmpty(toolID) || string.IsNullOrEmpty(conditionID))
        {
            Debug.LogWarning("⚠️ 実験に必要な要素が選択されていません");
            return;
        }

        Debug.Log("🧪 実験を実行します");
        requestSender.SendToAI(elementID, toolID, conditionID);
    }

    public void CollectFromTable()
    {
        CollectByGroup(candidateElements, 0);
        CollectByGroup(candidateTools, 1);
        CollectByGroup(candidateConditions, 2);
    }

    private void CollectByGroup(GameObject[] objects, int type)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if (obj == null) continue;

            PlaceableObject placeable = obj.GetComponent<PlaceableObject>();
            if (placeable != null && placeable.isFixed)
            {
                switch (type)
                {
                    case 0: holder.AddElement(obj.name); break;
                    case 1: holder.AddTool(obj.name); break;
                    case 2: holder.SetCondition(obj.name); break;
                }
            }
        }
    }
}