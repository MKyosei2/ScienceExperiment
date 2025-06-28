using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class CompoundRegistry : UdonSharpBehaviour
{
    [Header("UI")]
    public InputField inputName;
    public Text feedback;

    private string tagPrefix => Networking.LocalPlayer?.GetPlayerTag("CurrentRoom") ?? "";

    public void SaveToSlot(int slotIndex, string formula)
    {
        if (string.IsNullOrWhiteSpace(inputName.text))
        {
            feedback.text = "名前を入力してください";
            return;
        }

        string key = $"{tagPrefix}_slot{slotIndex}";
        string value = $"{inputName.text}:{formula}";
        Networking.LocalPlayer?.SetPlayerTag(key, value);
        feedback.text = $"保存完了: {value}";
    }

    public void LoadFromSlot(int slotIndex)
    {
        string key = $"{tagPrefix}_slot{slotIndex}";
        string value = Networking.LocalPlayer?.GetPlayerTag(key);

        if (string.IsNullOrEmpty(value))
            feedback.text = "未登録スロットです";
        else
            feedback.text = $"読み込み: {value}";
    }

    public string GetFormulaOnly(int slotIndex)
    {
        string key = $"{tagPrefix}_slot{slotIndex}";
        string value = Networking.LocalPlayer?.GetPlayerTag(key);

        if (!string.IsNullOrEmpty(value) && value.Contains(":"))
            return value.Substring(value.IndexOf(":") + 1);

        return "";
    }
}
