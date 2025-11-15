#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// シーン内すべての ChemElementSpawner に、
/// 118 元素の overrideNames / overrideColors を
/// 「実物の見た目にできるだけ近い色」で一括設定する一度きりスクリプト。
/// 実行後は削除してOK。
/// </summary>
public static class ChemElementSpawnerOverrideFiller
{
    // 118 元素記号
    private static readonly string[] ElementSymbols = new string[]
    {
        "H","He",
        "Li","Be","B","C","N","O","F","Ne",
        "Na","Mg","Al","Si","P","S","Cl","Ar",
        "K","Ca",
        "Sc","Ti","V","Cr","Mn","Fe","Co","Ni","Cu","Zn",
        "Ga","Ge","As","Se","Br","Kr",
        "Rb","Sr",
        "Y","Zr","Nb","Mo","Tc","Ru","Rh","Pd","Ag","Cd",
        "In","Sn","Sb","Te","I","Xe",
        "Cs","Ba",
        "La","Ce","Pr","Nd","Pm","Sm","Eu","Gd","Tb","Dy",
        "Ho","Er","Tm","Yb","Lu",
        "Hf","Ta","W","Re","Os","Ir","Pt","Au","Hg",
        "Tl","Pb","Bi","Po","At","Rn",
        "Fr","Ra",
        "Ac","Th","Pa","U","Np","Pu","Am","Cm","Bk","Cf",
        "Es","Fm","Md","No","Lr",
        "Rf","Db","Sg","Bh","Hs","Mt","Ds","Rg","Cn","Nh","Fl","Mc","Lv","Ts","Og"
    };

    [MenuItem("VRC Lab/一度きり/Fill ChemElementSpawner Overrides (Realistic Colors)")]
    private static void FillOverrides()
    {
        var spawners = Object.FindObjectsOfType<ChemElementSpawner>(true);
        if (spawners == null || spawners.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "ChemElementSpawnerOverrideFiller",
                "シーン内に ChemElementSpawner が見つかりませんでした。",
                "OK");
            return;
        }

        int changedCount = 0;

        foreach (var spawner in spawners)
        {
            if (spawner == null) continue;

            Undo.RecordObject(spawner, "Fill ChemElementSpawner Overrides (Realistic Colors)");

            spawner.overrideNames = new string[ElementSymbols.Length];
            spawner.overrideColors = new Color[ElementSymbols.Length];

            for (int i = 0; i < ElementSymbols.Length; i++)
            {
                string symbol = ElementSymbols[i];
                spawner.overrideNames[i] = symbol;
                spawner.overrideColors[i] = GetElementColor(symbol);
            }

            EditorUtility.SetDirty(spawner);
            changedCount++;
        }

        if (changedCount > 0)
            EditorSceneManager.MarkAllScenesDirty();

        EditorUtility.DisplayDialog(
            "ChemElementSpawnerOverrideFiller",
            $"ChemElementSpawner {changedCount} 件に対して\n" +
            $"118 元素の overrideNames / overrideColors を設定しました。\n\n" +
            "シーンを保存したら、このスクリプトは削除して構いません。",
            "OK");
    }

    // ■ RGB Helper
    private static Color RGB(byte r, byte g, byte b) => new Color32(r, g, b, 255);

    /// <summary>
    /// 元素の「見た目に近い色」を返す
    /// </summary>
    private static Color GetElementColor(string s)
    {
        switch (s)
        {
            case "H": return RGB(240, 240, 240);
            case "He": return RGB(235, 245, 255);

            case "Li": return RGB(180, 180, 190);
            case "Be": return RGB(196, 201, 206);
            case "B": return RGB(80, 80, 80);
            case "C": return RGB(30, 30, 30);
            case "N": return RGB(220, 230, 255);
            case "O": return RGB(180, 210, 255);
            case "F": return RGB(202, 255, 112);
            case "Ne": return RGB(255, 90, 60);

            case "Na": return RGB(250, 230, 130);
            case "Mg": return RGB(190, 190, 195);
            case "Al": return RGB(210, 210, 215);
            case "Si": return RGB(90, 90, 95);
            case "P": return RGB(255, 255, 255);
            case "S": return RGB(255, 240, 70);
            case "Cl": return RGB(205, 255, 112);
            case "Ar": return RGB(210, 230, 255);

            case "K": return RGB(160, 140, 115);
            case "Ca": return RGB(200, 200, 200);
            case "Sc": return RGB(190, 190, 200);
            case "Ti": return RGB(185, 190, 195);
            case "V": return RGB(170, 175, 180);
            case "Cr": return RGB(200, 200, 205);
            case "Mn": return RGB(180, 180, 185);
            case "Fe": return RGB(170, 170, 170);
            case "Co": return RGB(170, 175, 185);
            case "Ni": return RGB(185, 185, 190);
            case "Cu": return RGB(198, 120, 70);
            case "Zn": return RGB(200, 200, 205);
            case "Ga": return RGB(210, 210, 220);
            case "Ge": return RGB(105, 105, 110);
            case "As": return RGB(145, 140, 150);
            case "Se": return RGB(150, 40, 40);
            case "Br": return RGB(150, 40, 0);
            case "Kr": return RGB(220, 235, 255);

            case "Rb": return RGB(170, 145, 125);
            case "Sr": return RGB(220, 220, 230);
            case "Y": return RGB(195, 200, 210);
            case "Zr": return RGB(200, 200, 210);
            case "Nb": return RGB(175, 180, 190);
            case "Mo": return RGB(185, 190, 200);
            case "Tc": return RGB(160, 165, 175);
            case "Ru": return RGB(195, 200, 210);
            case "Rh": return RGB(200, 205, 215);
            case "Pd": return RGB(200, 205, 210);
            case "Ag": return RGB(230, 230, 235);
            case "Cd": return RGB(210, 210, 220);
            case "In": return RGB(210, 215, 225);
            case "Sn": return RGB(195, 200, 210);
            case "Sb": return RGB(170, 175, 185);
            case "Te": return RGB(95, 100, 110);
            case "I": return RGB(80, 0, 120);
            case "Xe": return RGB(200, 220, 255);

            case "Cs": return RGB(170, 150, 120);
            case "Ba": return RGB(210, 220, 230);
            case "La": return RGB(200, 205, 215);
            case "Ce": return RGB(190, 195, 205);
            case "Pr": return RGB(190, 195, 205);
            case "Nd": return RGB(185, 190, 200);
            case "Pm": return RGB(180, 185, 195);
            case "Sm": return RGB(190, 195, 205);
            case "Eu": return RGB(245, 245, 255);
            case "Gd": return RGB(190, 195, 205);
            case "Tb": return RGB(190, 195, 205);
            case "Dy": return RGB(190, 195, 205);
            case "Ho": return RGB(190, 195, 205);
            case "Er": return RGB(190, 195, 205);
            case "Tm": return RGB(190, 195, 205);
            case "Yb": return RGB(230, 235, 245);
            case "Lu": return RGB(195, 200, 210);

            case "Hf": return RGB(190, 195, 205);
            case "Ta": return RGB(115, 120, 130);
            case "W": return RGB(150, 150, 160);
            case "Re": return RGB(140, 140, 150);
            case "Os": return RGB(130, 135, 145);
            case "Ir": return RGB(200, 205, 215);
            case "Pt": return RGB(210, 210, 220);
            case "Au": return RGB(212, 175, 55);
            case "Hg": return RGB(210, 210, 220);

            case "Tl": return RGB(160, 165, 175);
            case "Pb": return RGB(125, 130, 140);
            case "Bi": return RGB(190, 195, 210);
            case "Po": return RGB(140, 140, 150);
            case "At": return RGB(100, 90, 110);
            case "Rn": return RGB(220, 230, 245);

            case "Fr": return RGB(180, 170, 160);
            case "Ra": return RGB(220, 230, 230);

            case "Ac": return RGB(170, 175, 185);
            case "Th": return RGB(180, 185, 195);
            case "Pa": return RGB(90, 95, 105);
            case "U": return RGB(70, 90, 40);
            case "Np": return RGB(100, 105, 115);
            case "Pu": return RGB(110, 115, 125);
            case "Am": return RGB(130, 135, 145);
            case "Cm": return RGB(150, 155, 165);
            case "Bk": return RGB(160, 165, 175);
            case "Cf": return RGB(170, 175, 185);
            case "Es": return RGB(180, 185, 195);
            case "Fm": return RGB(185, 190, 200);
            case "Md": return RGB(190, 195, 205);
            case "No": return RGB(195, 200, 210);
            case "Lr": return RGB(200, 205, 215);

            // 超重元素：実物不明 → 銀灰色
            case "Rf": return RGB(180, 185, 195);
            case "Db": return RGB(180, 185, 195);
            case "Sg": return RGB(180, 185, 195);
            case "Bh": return RGB(180, 185, 195);
            case "Hs": return RGB(180, 185, 195);
            case "Mt": return RGB(180, 185, 195);
            case "Ds": return RGB(180, 185, 195);
            case "Rg": return RGB(210, 190, 120);
            case "Cn": return RGB(180, 185, 195);
            case "Nh": return RGB(180, 185, 195);
            case "Fl": return RGB(180, 185, 195);
            case "Mc": return RGB(180, 185, 195);
            case "Lv": return RGB(180, 185, 195);
            case "Ts": return RGB(180, 185, 195);
            case "Og": return RGB(220, 230, 245);
        }

        // 未定義 → 銀灰色
        return RGB(185, 185, 195);
    }
}
#endif
