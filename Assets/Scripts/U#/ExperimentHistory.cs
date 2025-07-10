using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ExperimentHistory : UdonSharpBehaviour
{
    public string[] elementIDs = new string[32];
    public string[] toolIDs = new string[32];
    public string[] conditionIDs = new string[32];
    public string[] resultTexts = new string[32];
    public string[] triviaTexts = new string[32];
    public int count = 0;
    public int maxEntries = 32;

    public void AddEntry(string eID, string tID, string cID, string result, string trivia)
    {
        if (count >= maxEntries) return;
        elementIDs[count] = eID;
        toolIDs[count] = tID;
        conditionIDs[count] = cID;
        resultTexts[count] = result;
        triviaTexts[count] = trivia;
        count++;
    }

    public string GetFormattedEntry(int index)
    {
        if (index >= count) return "";
        return "🔬 " + elementIDs[index] + " × " + toolIDs[index] + " × " + conditionIDs[index] +
               "\n🧬 " + resultTexts[index] + "\n📖 " + triviaTexts[index];
    }
}