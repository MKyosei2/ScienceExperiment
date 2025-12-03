using UdonSharp;
using UnityEngine;

public class SelectionActionController : UdonSharpBehaviour
{
    public SpawnSelectorButton[] buttons;

    public void SelectIndex(int index)
    {
        if (buttons == null || index < 0 || index >= buttons.Length)
        {
            Debug.LogError("Button index out of range");
            return;
        }

        // 新仕様：UI の Press ではなく Interact() を直接呼ぶ
        buttons[index].Interact();
    }
}
