// ChemEnvironmentManager.cs
// ベース環境の保持と、全フラスコ(ChemVisualController)への一括反映を行うマネージャ
// UdonSharp制約対応: List<T>.Contains/Resize 等は使わず、固定長配列＋線形探索で実装

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
#if CHEM_RUNTIME
using UdonSharp;
#endif

#if CHEM_RUNTIME
public class ChemEnvironmentManager : UdonSharpBehaviour
#else
public class ChemEnvironmentManager : MonoBehaviour
#endif
{
    [Header("ベース環境（UIスライダーから操作予定）")]
    public float baseTempC = 20f;      // 既定20℃
    public float baseHumidity = 50f;   // %RH（極端値も可）
    public float basePressureAtm = 1f; // atm（真空～超高圧まで想定）

    [Header("同時管理する最大フラスコ数")]
    public int maxControllers = 256;

    // 内部：固定長配列で管理（U#の未公開API回避）
    private ChemVisualController[] _controllers;
    private int _count = 0;

    private void Awake()
    {
        if (maxControllers <= 0) maxControllers = 64;
        _controllers = new ChemVisualController[maxControllers];
    }

    // ===== 登録 / 解除（ChemVisualController から呼ばれる） =====
    public void Register(ChemVisualController c)
    {
        if (c == null) return;
        int idx = IndexOf(c);
        if (idx >= 0) return; // 既に登録済み

        if (_count < _controllers.Length)
        {
            _controllers[_count] = c;
            _count++;
        }
        // いっぱいの場合は無視（必要なら maxControllers を増やす）
    }

    public void Unregister(ChemVisualController c)
    {
        if (c == null) return;
        int idx = IndexOf(c);
        if (idx < 0) return;

        int last = _count - 1;
        _controllers[idx] = _controllers[last];
        _controllers[last] = null;
        _count = last;
    }

    private int IndexOf(ChemVisualController c)
    {
        for (int i = 0; i < _count; i++)
        {
            if (_controllers[i] == c) return i;
        }
        return -1;
    }

    // ===== UI（スライダー等）から呼ぶ =====
    public void SetTemperature(float celsius) { baseTempC = celsius; ApplyAll(); }
    public void SetHumidity(float percent) { baseHumidity = percent; ApplyAll(); }
    public void SetPressure(float atm) { basePressureAtm = (atm < 0f) ? 0f : atm; ApplyAll(); }

    // ===== 全反映 =====
    public void ApplyAll()
    {
        for (int i = 0; i < _count; i++)
        {
            ChemVisualController c = _controllers[i];
            if (c != null && c.enabled && c.gameObject.activeInHierarchy)
            {
                c.ApplyToShaders();
            }
        }
    }
}
