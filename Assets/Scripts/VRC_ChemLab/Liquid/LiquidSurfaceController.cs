using UdonSharp;
using UnityEngine;

public class LiquidSurfaceController : UdonSharpBehaviour
{
    public MeshRenderer liquidSurface;

    // 液体の色設定
    public void SetColor(Color c)
    {
        if (liquidSurface != null)
        {
            liquidSurface.material.SetColor("_Color", c);
        }
    }

    // 波紋（SetRipple） — ChemReactionAnimator が使用
    public void SetRipple(float strength)
    {
        if (liquidSurface != null)
        {
            liquidSurface.material.SetFloat("_RippleStrength", strength);
        }
    }

    // 波紋（SetWave） — LiquidWaveController が使用
    public void SetWave(float strength)
    {
        if (liquidSurface != null)
        {
            liquidSurface.material.SetFloat("_RippleStrength", strength);
        }
    }
}
