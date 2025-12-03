using UdonSharp;
using UnityEngine;

public class CategoryController : UdonSharpBehaviour
{
    public ButtonCategory category = ButtonCategory.Element;
    public SpawnSelectorButton[] buttons;

    public override void Interact()
    {
        ApplyCategory();
    }

    public void ApplyCategory()
    {
        Debug.Log("[CategoryController] カテゴリ適用: " + category);

        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogError("[CategoryController] ボタン配列が空です");
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            SpawnSelectorButton b = buttons[i];
            if (b == null)
            {
                Debug.LogError("[CategoryController] buttons[" + i + "] が null");
                continue;
            }

            // ここがエラー原因 → ButtonCategory を使うよう統一
            b.category = category;

            Debug.Log("[CategoryController] 設定: buttons[" + i + "] に " + category + " を適用");
        }
    }
}
