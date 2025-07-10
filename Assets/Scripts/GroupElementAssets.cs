using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GroupElementAssets : MonoBehaviour
{
    static readonly Dictionary<string, int> elementToGroup = new Dictionary<string, int>
    {
        {"Hydrogen",1}, {"Lithium",1}, {"Sodium",1}, {"Potassium",1}, {"Rubidium",1}, {"Cesium",1}, {"Francium",1}, {"Lanthanum",1}, {"Actinium",1},
        {"Beryllium",2}, {"Magnesium",2}, {"Calcium",2}, {"Strontium",2}, {"Barium",2}, {"Radium",2},
        {"Scandium",3}, {"Yttrium",3}, {"Cerium",3}, {"Praseodymium",3}, {"Neodymium",3}, {"Promethium",3}, {"Samarium",3}, {"Europium",3},
        {"Gadolinium",3}, {"Terbium",3}, {"Dysprosium",3}, {"Holmium",3}, {"Erbium",3}, {"Thulium",3}, {"Ytterbium",3}, {"Lutetium",3},
        {"Thorium",3}, {"Protactinium",3}, {"Uranium",3}, {"Neptunium",3}, {"Plutonium",3}, {"Americium",3}, {"Curium",3}, {"Berkelium",3},
        {"Californium",3}, {"Einsteinium",3}, {"Fermium",3}, {"Mendelevium",3}, {"Nobelium",3}, {"Lawrencium",3},
        {"Titanium",4}, {"Zirconium",4}, {"Hafnium",4}, {"Rutherfordium",4},
        {"Vanadium",5}, {"Niobium",5}, {"Tantalum",5}, {"Dubnium",5},
        {"Chromium",6}, {"Molybdenum",6}, {"Tungsten",6}, {"Seaborgium",6},
        {"Manganese",7}, {"Technetium",7}, {"Rhenium",7}, {"Bohrium",7},
        {"Iron",8}, {"Ruthenium",8}, {"Osmium",8}, {"Hassium",8},
        {"Cobalt",9}, {"Rhodium",9}, {"Iridium",9}, {"Meitnerium",9},
        {"Nickel",10}, {"Palladium",10}, {"Platinum",10}, {"Darmstadtium",10},
        {"Copper",11}, {"Silver",11}, {"Gold",11}, {"Roentgenium",11},
        {"Zinc",12}, {"Cadmium",12}, {"Mercury",12}, {"Copernicium",12},
        {"Boron",13}, {"Aluminium",13}, {"Gallium",13}, {"Indium",13}, {"Thallium",13}, {"Nihonium",13},
        {"Carbon",14}, {"Silicon",14}, {"Germanium",14}, {"Tin",14}, {"Lead",14}, {"Flerovium",14},
        {"Nitrogen",15}, {"Phosphorus",15}, {"Arsenic",15}, {"Antimony",15}, {"Bismuth",15}, {"Moscovium",15},
        {"Oxygen",16}, {"Sulfur",16}, {"Selenium",16}, {"Tellurium",16}, {"Polonium",16}, {"Livermorium",16},
        {"Fluorine",17}, {"Chlorine",17}, {"Bromine",17}, {"Iodine",17}, {"Astatine",17}, {"Tennessine",17},
        {"Helium",18}, {"Neon",18}, {"Argon",18}, {"Krypton",18}, {"Xenon",18}, {"Radon",18}, {"Oganesson",18}
    };

    [MenuItem("Tools/Sort Element ScriptableObjects by Group")]
    public static void SortElementAssets()
    {
        string inputPath = "Assets/Scripts/ElementData";
        string outputBase = "Assets/Scripts/ElementData";

        if (!Directory.Exists(inputPath))
        {
            Debug.LogError("❌ 入力フォルダが存在しません: " + inputPath);
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { inputPath });
        int moved = 0;
        List<string> unknowns = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);

            if (!elementToGroup.TryGetValue(name, out int group))
            {
                unknowns.Add(name);
                continue;
            }

            string groupFolder = $"{outputBase}/族{group}";

            // フォルダが無ければUnity的に作成
            if (!AssetDatabase.IsValidFolder(groupFolder))
            {
                AssetDatabase.CreateFolder(outputBase, $"族{group}");
            }

            string newPath = $"{groupFolder}/{name}.asset";

            // 移動先がすでに同じならスキップ
            if (path == newPath) continue;

            AssetDatabase.MoveAsset(path, newPath);
            moved++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ 分類完了: {moved} 個の ScriptableObject を 族ごとに分類しました。");

        if (unknowns.Count > 0)
        {
            Debug.LogWarning($"⚠️ 未分類の元素（辞書に存在しない）: {unknowns.Count} 件\n→ {string.Join(", ", unknowns)}");
        }
    }
}
