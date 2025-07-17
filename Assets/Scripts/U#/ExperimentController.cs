using UdonSharp;
using UnityEngine;

public class ExperimentController : UdonSharpBehaviour
{
    public SelectedObjectHolder holder;
    public AIRequestSender requestSender;

    public void CollectFromTable(Transform tableRoot)
    {
        holder.ClearAll();
        foreach (Transform child in tableRoot)
        {
            string objName = child.gameObject.name.ToLower();
            if (objName.Contains("element")) holder.AddElement(child.gameObject.name);
            else if (objName.Contains("tool")) holder.AddTool(child.gameObject.name);
        }
    }

    public void RunExperiment()
    {
        bool hasElement = holder.selectedElementIDs.Length > 0 && !string.IsNullOrEmpty(holder.selectedElementIDs[0]);
        bool hasTool = holder.selectedToolIDs.Length > 0 && !string.IsNullOrEmpty(holder.selectedToolIDs[0]);
        bool hasCondition = !string.IsNullOrEmpty(holder.selectedConditionID);

        if (!hasElement || !hasTool || !hasCondition)
        {
            Debug.Log("⚠️ 実験条件が揃っていません");
            return;
        }

        requestSender.SendToAI(0);
    }
}
