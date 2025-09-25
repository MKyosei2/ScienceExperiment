#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ChemEnvironmentManager))]
public class PeriodicTableInitializerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("🧪 元素データを自動生成 (現実世界の色・118個)"))
        {
            ChemEnvironmentManager manager = (ChemEnvironmentManager)target;

            // 元素記号（118）
            string[] keys = new string[] {
                "H","He","Li","Be","B","C","N","O","F","Ne",
                "Na","Mg","Al","Si","P","S","Cl","Ar","K","Ca",
                "Sc","Ti","V","Cr","Mn","Fe","Co","Ni","Cu","Zn",
                "Ga","Ge","As","Se","Br","Kr","Rb","Sr","Y","Zr",
                "Nb","Mo","Tc","Ru","Rh","Pd","Ag","Cd","In","Sn",
                "Sb","Te","I","Xe","Cs","Ba","La","Ce","Pr","Nd",
                "Pm","Sm","Eu","Gd","Tb","Dy","Ho","Er","Tm","Yb",
                "Lu","Hf","Ta","W","Re","Os","Ir","Pt","Au","Hg",
                "Tl","Pb","Bi","Po","At","Rn","Fr","Ra","Ac","Th",
                "Pa","U","Np","Pu","Am","Cm","Bk","Cf","Es","Fm",
                "Md","No","Lr","Rf","Db","Sg","Bh","Hs","Mt","Ds",
                "Rg","Cn","Nh","Fl","Mc","Lv","Ts","Og"
            };

            // 化学式（自然界での存在形を意識）
            string[] formulas = new string[] {
                "H₂","He","Li","Be","B","C","N₂","O₂","F₂","Ne",
                "Na","Mg","Al","Si","P","S₈","Cl₂","Ar","K","Ca",
                "Sc","Ti","V","Cr","Mn","Fe","Co","Ni","Cu","Zn",
                "Ga","Ge","As","Se","Br₂","Kr","Rb","Sr","Y","Zr",
                "Nb","Mo","Tc","Ru","Rh","Pd","Ag","Cd","In","Sn",
                "Sb","Te","I₂","Xe","Cs","Ba","La","Ce","Pr","Nd",
                "Pm","Sm","Eu","Gd","Tb","Dy","Ho","Er","Tm","Yb",
                "Lu","Hf","Ta","W","Re","Os","Ir","Pt","Au","Hg",
                "Tl","Pb","Bi","Po","At","Rn","Fr","Ra","Ac","Th",
                "Pa","U","Np","Pu","Am","Cm","Bk","Cf","Es","Fm",
                "Md","No","Lr","Rf","Db","Sg","Bh","Hs","Mt","Ds",
                "Rg","Cn","Nh","Fl","Mc","Lv","Ts","Og"
            };

            // 現実世界の見た目をできるだけ反映した色
            Color[] colors = new Color[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                switch (k)
                {
                    case "H": colors[i] = Color.clear; break;                // 無色気体
                    case "He": colors[i] = Color.clear; break;               // 無色気体
                    case "N": colors[i] = Color.clear; break;                // 無色気体
                    case "O": colors[i] = Color.clear; break;                // 無色気体
                    case "F": colors[i] = new Color(0.9f, 1f, 0.5f); break;  // 淡黄緑色気体
                    case "Cl": colors[i] = new Color(0.8f, 1f, 0.4f); break; // 黄緑色気体
                    case "Br": colors[i] = new Color(0.55f, 0.1f, 0.1f); break; // 赤褐色液体
                    case "I": colors[i] = new Color(0.3f, 0.1f, 0.4f); break;  // 紫黒色固体
                    case "C": colors[i] = Color.black; break;                // 黒鉛
                    case "S": colors[i] = Color.yellow; break;               // 黄色固体
                    case "P": colors[i] = new Color(1f, 1f, 1f); break;      // 白リン（白色）
                    case "Na": colors[i] = new Color(0.9f, 0.9f, 0.9f); break; // 銀白色金属
                    case "K": colors[i] = new Color(0.9f, 0.9f, 0.9f); break; // 銀白色
                    case "Fe": colors[i] = new Color(0.75f, 0.75f, 0.75f); break; // 銀灰色
                    case "Cu": colors[i] = new Color(0.8f, 0.4f, 0.1f); break;   // 赤銅色
                    case "Ag": colors[i] = new Color(0.9f, 0.9f, 0.9f); break;   // 銀白色
                    case "Au": colors[i] = new Color(1f, 0.84f, 0f); break;      // 黄金色
                    case "Hg": colors[i] = new Color(0.75f, 0.75f, 0.8f); break; // 銀白色液体
                    case "Pb": colors[i] = new Color(0.35f, 0.35f, 0.35f); break; // 鈍い灰色
                    default: colors[i] = new Color(0.8f, 0.8f, 0.85f); break;    // 金属 → 銀白色
                }
            }

            // ChemEnvironmentManager に設定
            manager.elementKeys = keys;
            manager.elementFormulas = formulas;
            manager.elementColors = colors;

            EditorUtility.SetDirty(manager);
            Debug.Log("[PeriodicTableInitializerEditor] 現実世界の色で元素データ (118個) を設定しました。");
        }
    }
}
#endif
