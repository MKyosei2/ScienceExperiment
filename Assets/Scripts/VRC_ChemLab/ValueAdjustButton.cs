using UdonSharp;
using UnityEngine;

[AddComponentMenu("VRC Lab/ValueAdjustButton")]
public class ValueAdjustButton : UdonSharpBehaviour
{
    public ChemEnvironmentManager envManager;
    public string type;  // "Temperature" / "Pressure" / "Humidity"
    public float step = 1f;

    public override void Interact()
    {
        _OnClick();
    }

    public void _OnClick()
    {
        if (envManager == null)
        {
            Debug.LogWarning("[ValueAdjustButton] envManager 未設定");
            return;
        }

        if (type == "Temperature")
            envManager._AdjustTemperature(step);
        else if (type == "Pressure")
            envManager._AdjustPressure(step);
        else if (type == "Humidity")
            envManager._AdjustHumidity(step);
        else
            Debug.LogWarning("[ValueAdjustButton] type が不正です");

        Debug.Log($"[ValueAdjustButton] {type} を {step} だけ変更");
    }
}
