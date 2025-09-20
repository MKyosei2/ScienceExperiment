// ChemVisualController.cs
// 役割：元素ボタン/オブジェクトから呼ばれ、ChemEnvironmentManager に元素セットを伝える。
// Udon準拠：FindObjectOfType は使用しない（Inspector で env を割り当てる）。

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
    [Header("Env")]
    [Tooltip("シーン内の ChemEnvironmentManager を Inspector で割り当ててください")]
    public ChemEnvironmentManager env;

    [Header("State")]
    [SerializeField] private string atomId = "A1";
    [SerializeField] private string elementSymbol = "H";
    [SerializeField] private int isotopeMass = 0;
    [SerializeField] private int charge = 0;

    void Start()
    {
        if (string.IsNullOrEmpty(atomId)) atomId = gameObject.name;
    }

    // UI（元素ボタン）から呼ぶ
    public void SetElementId(string symbol)
    {
        elementSymbol = symbol;
        if (env == null)
        {
            Debug.LogWarning("[ChemVisualController] env not assigned (Inspector で設定してください)");
            return;
        }

        env.SetElementId(atomId, symbol); // フラスコ／液体／二枚目を更新
    }

    public void ApplyToShaders()
    {
        if (env != null) env.ApplyToShaders();
    }

    public void SetIsotope(int mass)
    {
        isotopeMass = mass;
        if (env != null) env.SetIsotope(atomId, mass);
    }

    public void SetCharge(int q)
    {
        charge = q;
        if (env != null) env.SetCharge(atomId, q);
    }
}
