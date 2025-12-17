using UdonSharp;
using UnityEngine;

public class ReactionPredictor : UdonSharpBehaviour
{
    [Header("DB (optional, for better formulas)")]
    public ChemElementDatabase elementDb;

    // いまのプロジェクトの「器具ID」命名に合わせて調整してください
    [Header("Tool Keywords")]
    public string burnerKeyword = "Gasburner";
    public string flaskKeyword = "FLASK";
    public string beakerKeyword = "Beaker";

    [Header("Anions")]
    public string oxidationAnion = "O";
    public string chlorideAnion = "Cl";

    // 予測：入力（元素 or 式）＋器具 → 生成物式＋反応タグ＋説明
    public void Predict(string inputFormulaOrElement, string toolId,
        out string productFormula, out string reactionTag, out string explain)
    {
        string input = inputFormulaOrElement == null ? "" : inputFormulaOrElement.Trim();
        string tool = toolId == null ? "" : toolId.Trim();

        // デフォルト：変化なし
        productFormula = input;
        reactionTag = "none";
        explain = "現在の条件では、見た目変化を弱めに扱います。";

        if (string.IsNullOrEmpty(input))
        {
            productFormula = "";
            reactionTag = "none";
            explain = "入力が空です。";
            return;
        }

        bool looksCompound = LooksLikeCompound(input);

        // 加熱系（酸化方向）
        if (tool.Contains(burnerKeyword))
        {
            reactionTag = "oxidation";
            explain = "加熱を伴う変化として扱います（酸化/発熱/発光など）。";

            if (!looksCompound)
                productFormula = BuildBinaryIonicFormula(input, oxidationAnion);
            else
                productFormula = input;

            return;
        }

        // フラスコ系（塩化方向）
        if (tool.Contains(flaskKeyword))
        {
            reactionTag = "chloride";
            explain = "混合・反応として扱います（塩化物生成の方向）。";

            if (!looksCompound)
                productFormula = BuildBinaryIonicFormula(input, chlorideAnion);
            else
                productFormula = input;

            return;
        }

        // ビーカー系（溶解/混合）
        if (tool.Contains(beakerKeyword))
        {
            reactionTag = "dissolve";
            explain = "溶解・混合による変化として扱います。";
            productFormula = input;
            return;
        }
    }

    // ===== 強化：価数から式生成（Al(+3) + O(-2) => Al2O3）=====
    public string BuildBinaryIonicFormula(string cation, string anion)
    {
        // DBが無い or 価数が無いなら最低限の連結にフォールバック
        if (elementDb == null) return cation + anion;

        int c = elementDb.GetCommonCharge(cation);
        int a = elementDb.GetCommonCharge(anion);
        if (c == 0 || a == 0) return cation + anion;

        int pc = Mathf.Abs(c);
        int pa = Mathf.Abs(a);
        int lcm = LCM(pc, pa);
        int nc = lcm / pc;
        int na = lcm / pa;

        return cation + (nc > 1 ? nc.ToString() : "") + anion + (na > 1 ? na.ToString() : "");
    }

    private int GCD(int x, int y)
    {
        while (y != 0) { int t = x % y; x = y; y = t; }
        return x;
    }

    private int LCM(int x, int y)
    {
        if (x == 0 || y == 0) return 0;
        return (x / GCD(x, y)) * y;
    }

    private bool LooksLikeCompound(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        int upperCount = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= 'A' && c <= 'Z') upperCount++;
        }
        return upperCount >= 2;
    }
}
