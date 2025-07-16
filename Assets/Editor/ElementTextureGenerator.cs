using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

public class ElementTextureGenerator : EditorWindow
{
    private string[] elementSymbols = new string[]
    {
        "H", "He", "Li", "Be", "B", "C", "N", "O", "F", "Ne",
        "Na", "Mg", "Al", "Si", "P", "S", "Cl", "Ar",
        "K", "Ca", "Sc", "Ti", "V", "Cr", "Mn", "Fe", "Co", "Ni", "Cu", "Zn",
        "Ga", "Ge", "As", "Se", "Br", "Kr",
        "Rb", "Sr", "Y", "Zr", "Nb", "Mo", "Tc", "Ru", "Rh", "Pd", "Ag", "Cd",
        "In", "Sn", "Sb", "Te", "I", "Xe",
        "Cs", "Ba", "La", "Ce", "Pr", "Nd", "Pm", "Sm", "Eu", "Gd",
        "Tb", "Dy", "Ho", "Er", "Tm", "Yb", "Lu",
        "Hf", "Ta", "W", "Re", "Os", "Ir", "Pt", "Au", "Hg",
        "Tl", "Pb", "Bi", "Po", "At", "Rn",
        "Fr", "Ra", "Ac", "Th", "Pa", "U", "Np", "Pu", "Am", "Cm",
        "Bk", "Cf", "Es", "Fm", "Md", "No", "Lr",
        "Rf", "Db", "Sg", "Bh", "Hs", "Mt", "Ds", "Rg", "Cn", "Nh","Fl","Mc", "Lv", "Ts", "Og"
    };

    private Font font;
    private int fontSize = 128;
    private Color textColor = Color.black;
    private Color backgroundColor = Color.white;
    private string savePath = "Assets/Texture";

    [MenuItem("Tools/元素記号テクスチャ生成")]
    public static void ShowWindow()
    {
        GetWindow<ElementTextureGenerator>("元素記号画像生成");
    }

    void OnGUI()
    {
        GUILayout.Label("フォント設定", EditorStyles.boldLabel);
        font = (Font)EditorGUILayout.ObjectField("フォント", font, typeof(Font), false);
        fontSize = EditorGUILayout.IntField("フォントサイズ", fontSize);
        textColor = EditorGUILayout.ColorField("文字色", textColor);
        backgroundColor = EditorGUILayout.ColorField("背景色", backgroundColor);

        GUILayout.Space(10);
        GUILayout.Label("保存先設定", EditorStyles.boldLabel);
        savePath = EditorGUILayout.TextField("保存フォルダ", savePath);

        GUILayout.Space(20);
        if (GUILayout.Button("生成開始"))
        {
            GenerateAll();
        }
    }

    void GenerateAll()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        foreach (string symbol in elementSymbols)
        {
            CreateTexture(symbol);
        }

        AssetDatabase.Refresh();
        Debug.Log("✅ 元素記号画像の生成が完了しました！");
    }

    void CreateTexture(string text)
    {
        int width = 256;
        int height = 256;

        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture.active = rt;

        // カメラ
        Camera cam = new GameObject("TempCam").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        cam.orthographic = true;
        cam.orthographicSize = height / 2;
        cam.targetTexture = rt;

        // Canvas
        var canvasGO = new GameObject("TempCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.pixelPerfect = true;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

        // Text
        var textGO = new GameObject("TempText");
        textGO.transform.SetParent(canvasGO.transform);
        var textComp = textGO.AddComponent<Text>();
        textComp.text = text;
        textComp.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComp.fontSize = fontSize;
        textComp.color = textColor;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.rectTransform.sizeDelta = new Vector2(width, height);
        textComp.rectTransform.localPosition = Vector3.zero;

        // 強制カメラ描画
        cam.transform.position = new Vector3(0, 0, -10);
        canvasGO.transform.position = Vector3.zero;
        cam.Render();

        // Textureに読み込み
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // PNG保存
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(savePath, $"{text}.png"), bytes);

        // Cleanup
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(cam.gameObject);
        Object.DestroyImmediate(canvasGO);
        Object.DestroyImmediate(tex);
        rt.Release();
    }
}
