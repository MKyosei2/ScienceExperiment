using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class ReactionLibraryDisplay : UdonSharpBehaviour
{
    [TextArea(3, 10)]
    public string[] entryLines;

    public Text contentDisplay;
    private int currentIndex = 0;

    public void ShowNext()
    {
        currentIndex = (currentIndex + 1) % entryLines.Length;
        UpdateDisplay();
    }

    public void ShowPrevious()
    {
        currentIndex = (currentIndex - 1 + entryLines.Length) % entryLines.Length;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (entryLines.Length == 0 || contentDisplay == null) return;

        contentDisplay.text = FormatEntry(entryLines[currentIndex]);
    }

    private string FormatEntry(string raw)
    {
        string[] parts = raw.Split('=');
        return parts.Length == 2 ? parts[0].Replace("|", "\n条件: ") + "\n\n説明: " + parts[1] : raw;
    }
}
