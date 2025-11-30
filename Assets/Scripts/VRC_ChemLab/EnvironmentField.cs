// EnvironmentField.cs
// Triggerコライダー内に入った ChemVisualController へ環境寄与（温度/湿度/大気圧）＋水圧（任意）を与える

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID
#define CHEM_RUNTIME
#endif

using UnityEngine;
#if CHEM_RUNTIME
using UdonSharp;
#endif

// ※ U# はネスト型が未サポートのためトップレベルに定義
public enum EnvBlend { Add = 0, Multiply = 1, Set = 2 }

#if CHEM_RUNTIME
public class EnvironmentField : UdonSharpBehaviour
#else
public class EnvironmentField : MonoBehaviour
#endif
{
    [Header("温度 [°C]")]
    public EnvBlend tempMode = EnvBlend.Add;
    public float tempValue = 0f;      // 極端値可（例: +100000）

    [Header("湿度 [%RH相当]（極端値可）")]
    public EnvBlend humidityMode = EnvBlend.Set;
    public float humidityValue = 50f; // 0～100外も可（演出用）

    [Header("大気圧 [atm]")]
    public EnvBlend pressureMode = EnvBlend.Add;
    public float pressureAtmValue = 0f; // 真空～超高圧まで可

    [Header("水ボリューム（任意）")]
    public bool isWaterVolume = false;
    [Tooltip("水面のワールドY（BoxCollider上面等）")]
    public Transform waterSurfaceY;
    [Tooltip("流体密度[kg/m3]：水=1000, 水銀=13534 等")]
    public float fluidDensity = 1000f;
    [Tooltip("重力[m/s2]")]
    public float gravity = 9.80665f;

    [Header("メモ")]
    public string note;

    // 深さ[m]→atm への換算（floatのみ使用）
    public float HydrostaticAtm(float depthMeters)
    {
        if (depthMeters <= 0f) return 0f;
        const float ATM_PASCAL = 101325f;
        float pa = fluidDensity * gravity * depthMeters; // ρ g h [Pa]
        return pa / ATM_PASCAL;                          // [atm]
    }

    // 指定位置(world)が水面よりどれだけ下か（m）
    public float ComputeDepthAtWorldPos(Vector3 worldPos)
    {
        if (!isWaterVolume || waterSurfaceY == null) return 0f;
        float depth = waterSurfaceY.position.y - worldPos.y;
        return depth > 0f ? depth : 0f;
    }
}
