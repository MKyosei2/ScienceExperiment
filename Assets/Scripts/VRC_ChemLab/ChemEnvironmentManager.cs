using UdonSharp;
using UnityEngine;

public class ChemEnvironmentManager : UdonSharpBehaviour
{
    public float Temperature = 25f;
    public float Humidity = 40f;
    public float Pressure = 101f;

    public void Modify(string command)
    {
        switch (command)
        {
            case "TempPlus": Temperature += 1f; break;
            case "TempMinus": Temperature -= 1f; break;

            case "HumPlus": Humidity += 1f; break;
            case "HumMinus": Humidity -= 1f; break;

            case "PresPlus": Pressure += 1f; break;
            case "PresMinus": Pressure -= 1f; break;
        }
    }
}
