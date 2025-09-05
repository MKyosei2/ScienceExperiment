using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class SelectionStatusUI : UdonSharpBehaviour
{
    public SelectedObjectHolder selected;
    public Text text;
    public bool liveUpdate = true;

    private void OnEnable()
    {
        if (!liveUpdate) Refresh();
    }

    private void Update()
    {
        if (liveUpdate) Refresh();
    }

    public void Refresh()
    {
        if (!text || !selected) return;
        text.text = selected.ToSummaryString();
    }

    public void Show(string t)
    {
        if (text) text.text = t;
    }
}
