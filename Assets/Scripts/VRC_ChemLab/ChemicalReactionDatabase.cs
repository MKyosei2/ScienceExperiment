using UdonSharp;
using UnityEngine;

public class ChemicalReactionDatabase : UdonSharpBehaviour
{
    // 反応辞書：（元素A,元素B）→ 化合物名
    public string GetCompoundName(string a, string b)
    {
        // 並び順の揺れを防ぐためソート
        if (string.Compare(a, b) > 0)
        {
            string t = a; a = b; b = t;
        }

        
// 同じ元素同士は化合物ではない
if (a == b) return a + "（単体）";

// 典型例（足したい場合は増やしてください）
        if (a == "H" && b == "O") return "H₂O（水）";
        if (a == "Na" && b == "Cl") return "NaCl（塩化ナトリウム / 食塩）";
        if (a == "C" && b == "O") return "CO / CO₂（一酸化炭素 / 二酸化炭素）";
        if (a == "Fe" && b == "O") return "Fe₂O₃（酸化鉄 / さび）";
        if (a == "Cu" && b == "S") return "CuS（硫化銅）";

        return "不明な化合物（データなし）";
    }

    // 化合物説明文
    public string GetDescription(string compound)
    {
        if (compound.Contains("水")) return "水は生命維持に必須の物質で、室温では液体として存在します。";
        if (compound.Contains("食塩")) return "食塩はNa+とCl−のイオン結合で形成され、強い結晶構造を持ちます。";
        if (compound.Contains("酸化鉄")) return "鉄が酸素と結びつくことで生成する赤褐色の物質で、いわゆる「さび」です。";

        return "この化合物に関する説明はデータベースにありません。";
    }
}
