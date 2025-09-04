using UnityEngine;
using TMPro;

public class SelectionStatusUI : MonoBehaviour
{
    [SerializeField] private SelectedObjectHolder selected;
    [SerializeField] private TMP_Text text;
    [SerializeField] private bool liveUpdate = true;

    private void OnEnable()
    {
        if (!liveUpdate) Refresh();
    }

    private void Update()
    {
        if (liveUpdate) Refresh();
    }

    public void Show(string t)
    {
        if (text) text.text = t;
    }

    public void Refresh()
    {
        if (!text || !selected) return;
        text.text = selected.ToSummaryString();
    }
}
