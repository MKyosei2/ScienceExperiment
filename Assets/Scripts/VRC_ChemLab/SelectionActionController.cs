using UdonSharp;
using UnityEngine;

public class SelectionActionController : UdonSharpBehaviour
{
    public SpawnSelectorButton[] buttons;

    public void SelectIndex(int index)
    {
        if (buttons == null || index < 0 || index >= buttons.Length)
        {
            Debug.LogError("[SelectionAction] ボタン index が範囲外です");
            return;
        }

        SpawnSelectorButton button = buttons[index];
        if (button == null)
        {
            Debug.LogError("[SelectionAction] buttons[" + index + "] が null");
            return;
        }

        // 新仕様 → Press() は廃止されたので Interact() を直接呼ぶ
        Debug.Log("[SelectionAction] 実行: buttons[" + index + "] → Interact()");
        button.Interact();
    }
}
