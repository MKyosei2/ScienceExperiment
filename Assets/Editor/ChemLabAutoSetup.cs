using UnityEngine;
using UnityEditor;
using System.IO;

public class ChemLabAutoSetup : EditorWindow
{
    private const string SetupFlag = "Assets/Editor/OverflowSetupDone.txt";
    private const string PrefabPath = "Assets/OverflowParticle.prefab";

    // ▼▼▼ これが抜けていた！ ▼▼▼
    [MenuItem("ChemLab/Setup Overflow Particle")]
    public static void OpenWindow()
    {
        GetWindow<ChemLabAutoSetup>("ChemLab Overflow Setup");
    }
    // ▲▲▲ 重要：static メソッドを MenuItem の後に置く ▲▲▲

    private void OnGUI()
    {
        GUILayout.Label("ChemLab Overflow Particle 自動セットアップ", EditorStyles.boldLabel);

        if (File.Exists(SetupFlag))
        {
            GUILayout.Label("✔ すでにセットアップ済みです。", EditorStyles.helpBox);
            if (GUILayout.Button("再セットアップを強制実行する"))
            {
                RunSetup(true);
            }
            return;
        }

        if (GUILayout.Button("一度だけ自動セットアップ"))
        {
            RunSetup(false);
        }
    }

    private void RunSetup(bool force)
    {
        if (!force && File.Exists(SetupFlag))
        {
            Debug.LogWarning("Setup はすでに実行済みです。");
            return;
        }

        Debug.Log("=== ChemLab Overflow Setup 開始 ===");

        // -----------------------------------------
        // (1) ChemElementSpawner を検索
        // -----------------------------------------
        ChemElementSpawner spawner = FindObjectOfType<ChemElementSpawner>();
        if (spawner == null)
        {
            Debug.LogError("❌ ChemElementSpawner がシーンに見つかりません。");
            return;
        }

        // -----------------------------------------
        // (2) OverflowParticle.prefab が存在するか確認
        // -----------------------------------------
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null)
        {
            Debug.Log("OverflowParticle.prefab を新規作成します…");

            GameObject temp = new GameObject("OverflowParticle");
            var ps = temp.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 1.2f;
            main.startSize = 0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

            PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
            GameObject.DestroyImmediate(temp);

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Debug.Log("✔ OverflowParticle.prefab を自動生成しました");
        }
        else
        {
            Debug.Log("✔ OverflowParticle.prefab はすでに存在します");
        }

        // -----------------------------------------
        // (3) ChemElementSpawner に割り当て
        // -----------------------------------------
        spawner.overflowParticlePrefab = prefab;
        EditorUtility.SetDirty(spawner);

        Debug.Log("✔ overflowParticlePrefab に自動設定しました");

        // -----------------------------------------
        // (4) 一度だけ実行のフラグを保存
        // -----------------------------------------
        File.WriteAllText(SetupFlag, "Done");
        AssetDatabase.Refresh();

        Debug.Log("=== ChemLab Overflow Setup 完了 ===");
    }
}
