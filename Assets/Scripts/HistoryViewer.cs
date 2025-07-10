using UnityEngine;
using TMPro;

public class HistoryViewer : MonoBehaviour
{
    public ExperimentHistory history;
    public TextMeshProUGUI output;
    private int current = 0;

    public void ShowNext()
    {
        if (current < history.count - 1) current++;
        UpdateView();
    }

    public void ShowPrev()
    {
        if (current > 0) current--;
        UpdateView();
    }

    void UpdateView()
    {
        output.text = history.GetFormattedEntry(current);
    }
}