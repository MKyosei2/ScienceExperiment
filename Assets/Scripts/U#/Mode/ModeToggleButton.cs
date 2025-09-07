using UdonSharp;
using UnityEngine;
using TMPro;

[AddComponentMenu("VRC Lab/Mode/ModeToggleButton")]
public class ModeToggleButton : UdonSharpBehaviour
{
    public ModeRouter router;
    public TextMeshProUGUI label; // 任意

    public override void Interact() { Toggle(); }

    public void Toggle()
    {
        if (router == null) return;
        router.Toggle();
        UpdateLabel();
    }

    private void OnEnable() { UpdateLabel(); }

    private void UpdateLabel()
    {
        if (label == null || router == null) return;
        label.text = router.IsVR() ? "Mode: VR" : "Mode: PC";
    }
}
