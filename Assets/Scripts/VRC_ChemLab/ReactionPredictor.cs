using UdonSharp;
using UnityEngine;

public class ReactionPredictor : UdonSharpBehaviour
{
    public ChemElementDatabase db;

    public string Predict(string a, string b)
    {
        int ga = db.GetGroup(a);
        int gb = db.GetGroup(b);

        // 中和反応（アルカリ金属 × ハロゲン）
        if ((ga == 1 && gb == 4) || (ga == 4 && gb == 1))
            return $"{a} + {b} → 塩（中和反応）";

        // 金属 + ハロゲン
        if ((ga == 3 && gb == 4) || (ga == 4 && gb == 3))
            return $"{a} + {b} → 金属ハロゲン化物";

        // 酸化
        if ((a == "C" && b == "O") || (a == "O" && b == "C"))
            return "C + O₂ → CO₂（酸化反応）";

        if ((a == "H" && b == "O") || (a == "O" && b == "H"))
            return "2H + O → H₂O（結合反応）";

        if (ga == gb)
            return $"{a} + {b} → 混合（反応なし）";

        // 不明反応 → 混色
        return $"{a} + {b} → 不明（混色のみ）";
    }
}
