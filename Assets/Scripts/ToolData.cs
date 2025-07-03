using UnityEngine;

[CreateAssetMenu(fileName = "ToolData", menuName = "ChemLab/Tool")]
public class ToolData : ScriptableObject
{
    // ────── 基本情報 ──────
    [Header("基本情報")]
    public string toolName;             // 表示名
    public string toolID;               // ユニークID
    [TextArea(2, 5)]
    public string description;          // 概要説明
    public Sprite icon;                 // UIアイコン

    // ────── 分類・用途 ──────
    public enum ToolFunction
    {
        Container,
        Heater,
        Stirrer,
        Filter,
        Sensor,
        Injector,
        ReactionSurface,
        Other
    }

    [Header("分類・機能")]
    public ToolFunction functionType = ToolFunction.Other;

    public bool isUserOperated = true;  // 手動器具か
    public bool requiresPower = false;  // 電力を必要とするか
    public bool supportsAutomation = false; // 自動ボット操作に対応

    // ────── 使用制限・安全性 ──────
    [Header("使用制限と安全性")]
    public bool isReusable = true;      // 再使用可能
    public int maxUses = 0;             // 0 = 無制限
    public bool isHazardous = false;    // 危険を伴う器具か
    [TextArea(1, 3)]
    public string safetyWarning;        // 警告メッセージ

    // ────── 容量・定量性 ──────
    [Header("定量情報")]
    public float maxVolumeML = 0f;      // 最大容量 (ml)
    public bool isGraduated = false;    // 目盛付き容器か

    // ────── 実験反応制限 ──────
    [Header("対応・制限")]
    public string[] supportedReactions;       // この器具が使える反応名/タグ
    public string[] compatibleToolIDs;        // 相性の良い器具ID
    public string behaviorScriptName;         // 動作スクリプト識別名

    // ────── 視覚（最低限） ──────
    [Header("視覚情報")]
    public Color toolColor = Color.white;     // 表示カラー
    public GameObject toolPrefab;             // 実験空間に出す3Dモデル

    // ────── UI・教育支援 ──────
    [Header("ヒントと補足")]
    [TextArea(1, 3)]
    public string usageHint;                // 使い方の補足
    public string usageCategory;              // UIグループ（例：基本器具／電源系）

    // ────── ユーティリティ関数 ──────
    public string Summary()
    {
        return $"{toolName} ({toolID})\n{description}";
    }

    public string GetToolName()
    {
        return toolName;
    }

    public bool CanBeUsedFor(string reactionKey)
    {
        if (supportedReactions == null || supportedReactions.Length == 0) return true;
        foreach (var tag in supportedReactions)
        {
            if (reactionKey.Contains(tag)) return true;
        }
        return false;
    }
}