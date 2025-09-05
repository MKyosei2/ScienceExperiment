using UdonSharp;
using UnityEngine;

// 共通カテゴリ列挙（UdonSharpは入れ子型NGのためトップレベル定義）
public enum ESelectorCategory
{
    Element = 0,
    Tool = 1,
    Condition = 2
}

/*
 * UdonSharp は「この .cs ファイル内の UdonSharpBehaviour のクラス名＝ファイル名」を要求します。
 * 本ファイル名 SelectorCategory.cs に合わせて、空のアンカー用クラス SelectorCategory を同居させます。
 * シーンにアタッチする必要はありません（Program AssetのScript先に指定してもOK）。
 */
public class SelectorCategory : UdonSharpBehaviour
{
    // アンカー用途。中身は空でOK。
}
