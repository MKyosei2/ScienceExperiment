using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ElementDataExporter : MonoBehaviour
{
    [Header("対象の ElementData 配列")]
    public ElementData[] elements;

    [Header("出力先 Text コンポーネント")]
    public Text jsonOutput;

    [ContextMenu("Export ElementData to JSON")]
    public void ExportToJson()
    {
        if (elements == null || elements.Length == 0)
        {
            Debug.LogWarning("ElementData が設定されていません");
            return;
        }

        List<Dictionary<string, object>> outputList = new List<Dictionary<string, object>>();

        foreach (ElementData e in elements)
        {
            if (e == null) continue;

            var dict = new Dictionary<string, object>
            {
                { "name", e.elementName },
                { "symbol", e.symbol },
                { "atomicNumber", e.atomicNumber },
                { "group", e.group },
                { "period", e.period },
                { "category", e.category.ToString() },

                { "electronConfiguration", e.electronConfiguration },
                { "phase", e.phase },
                { "atomicMass", e.atomicMass },
                { "atomicRadius", e.atomicRadius },
                { "density", e.density },
                { "meltingPoint", e.meltingPoint },
                { "boilingPoint", e.boilingPoint },

                { "electronegativity", e.electronegativity },
                { "valenceElectrons", e.valenceElectrons },
                { "ionizationEnergy", e.ionizationEnergy },
                { "electronAffinity", e.electronAffinity },
                { "isRadioactive", e.isRadioactive },

                { "commonUses", e.commonUses },
                { "discoveredBy", e.discoveredBy },
                { "discoveryYear", e.discoveryYear },

                { "isEssentialToLife", e.isEssentialToLife },
                { "toxicityInfo", e.toxicityInfo },

                { "displayColor", ColorUtility.ToHtmlStringRGB(e.displayColor) },
                { "displayPrefabName", e.displayPrefab != null ? e.displayPrefab.name : "" }
            };

            outputList.Add(dict);
        }

        string json = JsonHelper.ToJson(outputList.ToArray(), true);
        if (jsonOutput != null)
        {
            jsonOutput.text = json;
        }

#if UNITY_EDITOR
        Debug.Log("ElementData JSON:\n" + json);
#endif
    }
}
