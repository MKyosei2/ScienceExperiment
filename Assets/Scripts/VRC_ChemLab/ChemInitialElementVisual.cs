using UdonSharp;
using UnityEngine;

public class ChemInitialElementVisual : UdonSharpBehaviour
{
    [Header("Refs")]
    public ChemElementDatabase elementDb;
    public ChemVisualController visual;

    [Header("Initial State")]
    public string symbol = "H";
    public float temperatureC = 25f;
    public bool applyOnStart = true;

    private void Start()
    {
        if (!applyOnStart) return;

        if (visual == null) visual = GetComponent<ChemVisualController>();
        if (visual == null) visual = GetComponentInChildren<ChemVisualController>(true);

        // elementDbはEditor側で入れる想定（未設定なら軽いフォールバック）
        if (elementDb == null)
        {
            // 例: シーン内に "ChemElementDatabase" という名前がある場合
            GameObject dbGo = GameObject.Find("ChemElementDatabase");
            if (dbGo != null) elementDb = dbGo.GetComponent<ChemElementDatabase>();
        }

        if (visual != null && elementDb != null)
        {
            visual.ApplyElementBySymbol(elementDb, symbol, temperatureC);
        }
    }
}
