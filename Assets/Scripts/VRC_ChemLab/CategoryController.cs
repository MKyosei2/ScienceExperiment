using UdonSharp;
using UnityEngine;

public class CategoryController : UdonSharpBehaviour
{
    public GameObject elementPanel;
    public GameObject toolPanel;
    public GameObject conditionPanel;

    public void ShowElement()
    {
        elementPanel.SetActive(true);
        toolPanel.SetActive(false);
        conditionPanel.SetActive(false);
    }

    public void ShowTool()
    {
        elementPanel.SetActive(false);
        toolPanel.SetActive(true);
        conditionPanel.SetActive(false);
    }

    public void ShowCondition()
    {
        elementPanel.SetActive(false);
        toolPanel.SetActive(false);
        conditionPanel.SetActive(true);
    }
}
