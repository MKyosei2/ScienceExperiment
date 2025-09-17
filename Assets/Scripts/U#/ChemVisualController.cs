// ChemVisualController.cs
// UdonSharp制約対応：InspectorでManager割当必須。探索やAddComponentを使わない。

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
    [Header("Manager (必ずInspectorで指定)")]
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
            env.AddAtom(atomId, elementSymbol, isotopeMass, charge);
        }
    }

    // 旧互換API
    public void SetElementId(string symbol)
    {
        elementSymbol = symbol;
        if (env != null) env.SetElementId(atomId, symbol);
    }
    public void ApplyToShaders() { if (env != null) env.ApplyToShaders(); }
    public void SetIsotope(int mass) { isotopeMass = mass; if (env != null) env.SetIsotope(atomId, mass); }
    public void SetCharge(int q) { charge = q; if (env != null) env.SetCharge(atomId, q); }
}
