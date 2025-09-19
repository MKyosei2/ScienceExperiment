// ChemVisualController.cs
// 元素Prefabにアタッチする制御。Elementが設定されたらChemEnvironmentManagerに通知して
// フラスコ生成＋ラベル表示を行う。

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
#if CHEM_RUNTIME
using UdonSharp;
#endif

#if CHEM_RUNTIME
public class ChemVisualController : UdonSharpBehaviour
#else
public class ChemVisualController : MonoBehaviour
#endif
{
    [Header("Manager (Inspectorで必ず割当)")]
    public ChemEnvironmentManager env;

    [SerializeField] private string atomId = "";
    [SerializeField] private string elementSymbol = "C";
    [SerializeField] private int isotopeMass = 0;
    [SerializeField] private int charge = 0;

    void Start()
    {
        if (string.IsNullOrEmpty(atomId)) atomId = gameObject.name;

        if (env != null)
        {
            Debug.Log("[ChemVisualController] Start: AddAtom " + atomId + " (" + elementSymbol + ")");
            env.AddAtom(atomId, elementSymbol, isotopeMass, charge);
        }
        else
        {
            Debug.LogWarning("[ChemVisualController] Start: env not assigned!");
        }
    }

    // 元素をセット（ここでフラスコ＋ラベルも生成）
    public void SetElementId(string symbol)
    {
        elementSymbol = symbol;
        if (env != null)
        {
            Debug.Log("[ChemVisualController] SetElementId called: " + atomId + " -> " + symbol);
            env.SetElementId(atomId, symbol);
            env.SpawnFlaskLook(symbol);
            env.SpawnOrUpdateLabel(symbol);
        }
        else
        {
            Debug.LogWarning("[ChemVisualController] SetElementId: env not assigned!");
        }
    }

    public void ApplyToShaders() { if (env != null) env.ApplyToShaders(); }
    public void SetIsotope(int mass) { isotopeMass = mass; if (env != null) env.SetIsotope(atomId, mass); }
    public void SetCharge(int q) { charge = q; if (env != null) env.SetCharge(atomId, q); }
}
