using UdonSharp;
using UnityEngine;

public class ReactionPredictor : UdonSharpBehaviour
{
    public ChemElementDatabase db;

    public string Predict(string a, string b)
    {
        string g1 = db.GetGroup(a);
        string g2 = db.GetGroup(b);

        if (g1 == "" || g2 == "")
            return "";

        // 同じグループ → 波
        if (g1 == g2)
            return "波";

        // 金属 × 酸素
        if ((g1 == "metal" && g2 == "oxygen") ||
            (g1 == "oxygen" && g2 == "metal"))
            return "酸化";

        // 金属 × 塩素
        if ((g1 == "metal" && g2 == "chlorine") ||
            (g1 == "chlorine" && g2 == "metal"))
            return "塩";

        return "";
    }
}
