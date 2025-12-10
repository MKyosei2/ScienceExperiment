using UdonSharp;
using UnityEngine;
using System.Text;

public class AIRequestSender : UdonSharpBehaviour
{
    public ChemElementSpawner spawner;
    public ChemElementDatabase database;
    public ChemEnvironmentManager environment;

    // ===============================
    // 実験開始時に呼ばれるエントリーポイント
    // ===============================
    public void RunAIAnalysis()
    {
        string element = spawner.GetLastElement();
        string equip = spawner.GetLastEquipment();

        if (element == "None" || equip == "None")
        {
            spawner.AppendAILog("[AI] 必要な情報（元素・器具）が不足しています。");
            return;
        }

        string reaction = PredictReaction(element, equip);
        string natural = BuildNaturalLanguage(element, equip, reaction);

        spawner.AppendAILog(natural);
    }

    // ===============================
    // 反応予測（現実の化学ルールベース）
    // ===============================
    private string PredictReaction(string element, string equip)
    {
        float T = environment.Temperature;
        float H = environment.Humidity;
        float P = environment.Pressure;

        string group = database.GetGroup(element);
        float melt = database.GetMeltingPoint(element);
        float boil = database.GetBoilingPoint(element);

        // ---- 気体状態なら反応しにくい ----
        if (T > boil)
        {
            return $"{element} は気体であり反応性が大幅に低下しています。ほとんど反応しませんでした。";
        }

        // ---- 例：アルカリ金属 × 水 → 激発反応 ----
        if (group == "Alkali" && equip == "Water")
        {
            return "アルカリ金属は水と激しく反応し、水酸化物 + 水素を生成。反応式: 2M + 2H2O → 2MOH + H2↑";
        }

        // ---- 酸化（燃焼器 / 高温）----
        if (equip == "Burner" && T > 150)
        {
            return $"{element} は酸素と結合し金属酸化物を生成。例: 4Fe + 3O2 → 2Fe2O3";
        }

        // ---- 湿度が高い → 錆び（酸化）----
        if (group == "Metal" && H > 60)
        {
            return $"{element} は湿度の影響で表面に酸化被膜（錆）が生成される。";
        }

        // ---- 酸 × 塩基 → 中和 ----
        if (group == "Acid" && equip == "Base")
        {
            return "中和反応が進行し、塩と水を生成。一般式: HA + BOH → BA + H2O";
        }

        return "特筆すべき化学反応は観察されませんでした。";
    }

    // ===============================
    // AI が自然文で説明する（文章生成）
    // ===============================
    private string BuildNaturalLanguage(string element, string equip, string reaction)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("【AI 解析結果】");
        sb.AppendLine($"選択元素 : {element}");
        sb.AppendLine($"使用器具 : {equip}");
        sb.AppendLine();
        sb.AppendLine($"▼ 実験条件");
        sb.AppendLine($"温度 : {environment.Temperature} ℃");
        sb.AppendLine($"湿度 : {environment.Humidity} %");
        sb.AppendLine($"圧力 : {environment.Pressure} kPa");
        sb.AppendLine();
        sb.AppendLine("▼ 反応の考察");
        sb.AppendLine(reaction);

        return sb.ToString();
    }
}
