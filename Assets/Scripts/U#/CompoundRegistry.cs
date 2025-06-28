using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class CompoundRegistry : UdonSharpBehaviour
{
    [Header("UI")]
    public InputField inputName;
    public Text feedback;

    private string GetTagPrefix()
    {
        if (Networking.LocalPlayer != null)
        {
            string tag = Networking.LocalPlayer.GetPlayerTag("CurrentRoom");
            return tag != null ? tag : "";
        }
        return "";
    }

    public void SaveToSlot(int slotIndex, string formula)
    {
        if (string.IsNullOrWhiteSpace(inputName.text))
        {
            feedback.text = "名前を入力してください";
            return;
        }

        string key = GetTagPrefix() + "_slot" + slotIndex;
        string value = inputName.text + ":" + formula;

        if (Networking.LocalPlayer != null)
        {
            Networking.LocalPlayer.SetPlayerTag(key, value);
        }

        feedback.text = "保存完了: " + value;
    }

    public void LoadFromSlot(int slotIndex)
    {
        string key = GetTagPrefix() + "_slot" + slotIndex;
        string value = "";

        if (Networking.LocalPlayer != null)
        {
            value = Networking.LocalPlayer.GetPlayerTag(key);
        }

        if (string.IsNullOrEmpty(value))
        {
            feedback.text = "未登録スロットです";
        }
        else
        {
            feedback.text = "読み込み: " + value;
        }
    }

    public string GetFormulaOnly(int slotIndex)
    {
        string key = GetTagPrefix() + "_slot" + slotIndex;
        string value = "";

        if (Networking.LocalPlayer != null)
        {
            value = Networking.LocalPlayer.GetPlayerTag(key);
        }

        if (!string.IsNullOrEmpty(value) && value.Contains(":"))
        {
            int index = value.IndexOf(":") + 1;
            return value.Substring(index);
        }

        return "";
    }
}