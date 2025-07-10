using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class GroupElementAssets : MonoBehaviour
{
    [System.Serializable]
    public class ElementInfo
    {
        public string symbol;
        public string name;
        public int atomicNumber;
        public int group;

        public ElementInfo(string symbol, string name, int atomicNumber, int group)
        {
            this.symbol = symbol;
            this.name = name;
            this.atomicNumber = atomicNumber;
            this.group = group;
        }
    }

    static readonly List<ElementInfo> elements = new List<ElementInfo>
    {
        // Group 1
        new("H","Hydrogen",1,1), new("Li","Lithium",3,1), new("Na","Sodium",11,1), new("K","Potassium",19,1),
        new("Rb","Rubidium",37,1), new("Cs","Cesium",55,1), new("Fr","Francium",87,1), new("La","Lanthanum",57,1), new("Ac","Actinium",89,1),

        // Group 2
        new("Be","Beryllium",4,2), new("Mg","Magnesium",12,2), new("Ca","Calcium",20,2), new("Sr","Strontium",38,2),
        new("Ba","Barium",56,2), new("Ra","Radium",88,2), new("Ce","Cerium",58,2), new("Th","Thorium",90,2),

        // Group 3
        new("Sc","Scandium",21,3), new("Y","Yttrium",39,3), new("Pr","Praseodymium",59,3), new("Pa","Protactinium",91,3),

        // Group 4
        new("Ti","Titanium",22,4), new("Zr","Zirconium",40,4), new("Hf","Hafnium",72,4), new("Rf","Rutherfordium",104,4),
        new("Nd","Neodymium",60,4), new("U","Uranium",92,4),

        // Group 5
        new("V","Vanadium",23,5), new("Nb","Niobium",41,5), new("Ta","Tantalum",73,5), new("Db","Dubnium",105,5),
        new("Pm","Promethium",61,5), new("Np","Neptunium",93,5),

        // Group 6
        new("Cr","Chromium",24,6), new("Mo","Molybdenum",42,6), new("W","Tungsten",74,6), new("Sg","Seaborgium",106,6),
        new("Sm","Samarium",62,6), new("Pu","Plutonium",94,6),

        // Group 7
        new("Mn","Manganese",25,7), new("Tc","Technetium",43,7), new("Re","Rhenium",75,7), new("Bh","Bohrium",107,7),
        new("Eu","Europium",63,7), new("Am","Americium",95,7),

        // Group 8
        new("Fe","Iron",26,8), new("Ru","Ruthenium",44,8), new("Os","Osmium",76,8), new("Hs","Hassium",108,8),
        new("Gd","Gadolinium",64,8), new("Cm","Curium",96,8),

        // Group 9
        new("Co","Cobalt",27,9), new("Rh","Rhodium",45,9), new("Ir","Iridium",77,9), new("Mt","Meitnerium",109,9),
        new("Tb","Terbium",65,9), new("Bk","Berkelium",97,9),

        // Group 10
        new("Ni","Nickel",28,10), new("Pd","Palladium",46,10), new("Pt","Platinum",78,10), new("Ds","Darmstadtium",110,10),
        new("Dy","Dysprosium",66,10), new("Cf","Californium",98,10),

        // Group 11
        new("Cu","Copper",29,11), new("Ag","Silver",47,11), new("Au","Gold",79,11), new("Rg","Roentgenium",111,11),
        new("Ho","Holmium",67,11), new("Es","Einsteinium",99,11),

        // Group 12
        new("Zn","Zinc",30,12), new("Cd","Cadmium",48,12), new("Hg","Mercury",80,12), new("Cn","Copernicium",112,12),
        new("Er","Erbium",68,12), new("Fm","Fermium",100,12),

        // Group 13
        new("B","Boron",5,13), new("Al","Aluminium",13,13), new("Ga","Gallium",31,13), new("In","Indium",49,13),
        new("Tl","Thallium",81,13), new("Nh","Nihonium",113,13), new("Tm","Thulium",69,13), new("Md","Mendelevium",101,13),

        // Group 14
        new("C","Carbon",6,14), new("Si","Silicon",14,14), new("Ge","Germanium",32,14), new("Sn","Tin",50,14),
        new("Pb","Lead",82,14), new("Fl","Flerovium",114,14), new("Yb","Ytterbium",70,14), new("No","Nobelium",102,14),

        // Group 15
        new("N","Nitrogen",7,15), new("P","Phosphorus",15,15), new("As","Arsenic",33,15), new("Sb","Antimony",51,15),
        new("Bi","Bismuth",83,15), new("Mc","Moscovium",115,15), new("Lu","Lutetium",71,15), new("Lr","Lawrencium",103,15),

        // Group 16
        new("O","Oxygen",8,16), new("S","Sulfur",16,16), new("Se","Selenium",34,16), new("Te","Tellurium",52,16),
        new("Po","Polonium",84,16), new("Lv","Livermorium",116,16),

        // Group 17
        new("F","Fluorine",9,17), new("Cl","Chlorine",17,17), new("Br","Bromine",35,17), new("I","Iodine",53,17),
        new("At","Astatine",85,17), new("Ts","Tennessine",117,17),

        // Group 18
        new("He","Helium",2,18), new("Ne","Neon",10,18), new("Ar","Argon",18,18), new("Kr","Krypton",36,18),
        new("Xe","Xenon",54,18), new("Rn","Radon",86,18), new("Og","Oganesson",118,18),
    };

    [MenuItem("Tools/ChemLab/Generate All ElementData")]
    public static void GenerateAllElements()
    {
        string basePath = "Assets/Scripts/ElementData";
        foreach (var element in elements)
        {
            string groupPath = $"{basePath}/族{element.group}";
            if (!AssetDatabase.IsValidFolder(groupPath))
                AssetDatabase.CreateFolder(basePath, $"族{element.group}");

            string assetPath = $"{groupPath}/{element.name}.asset";
            if (File.Exists(assetPath)) continue;

            ElementData data = ScriptableObject.CreateInstance<ElementData>();
            data.elementID = element.symbol;
            data.elementName = element.name;
            data.atomicNumber = element.atomicNumber;
            data.displayPrefab = null;

            AssetDatabase.CreateAsset(data, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ 全元素を族ごとに生成完了！");
    }
}
