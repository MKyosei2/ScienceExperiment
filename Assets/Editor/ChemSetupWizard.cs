// Assets/Editor/ChemSetupWizard.cs
// 一度だけ実行するセットアップ用。Hierarchyに必要な子を自動生成し、配列に割当。

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ChemSetupWizard
{
    [MenuItem("ChemLab/Setup Scene Hierarchy")]
    public static void SetupScene()
    {
        // ChemEnvironmentManager を探す
        ChemEnvironmentManager mgr = Object.FindObjectOfType<ChemEnvironmentManager>();
        if (mgr == null)
        {
            GameObject go = new GameObject("ChemEnvironmentManager");
            mgr = go.AddComponent<ChemEnvironmentManager>();
            Debug.Log("ChemEnvironmentManager を新規作成しました");
        }

        Transform root = mgr.transform;

        // ---------- Atoms ----------
        GameObject atomsRoot = new GameObject("Atoms");
        atomsRoot.transform.SetParent(root);
        int atomCount = 10; // 必要に応じて変更
        mgr.atomRoots = new GameObject[atomCount];
        mgr.atomRims = new LineRenderer[atomCount];
        mgr.atomLabels = new TMPro.TextMeshPro[atomCount];
        mgr.atomAux = new TMPro.TextMeshPro[atomCount];
        for (int i = 0; i < atomCount; i++)
        {
            GameObject slot = new GameObject("AtomSlot_" + i);
            slot.transform.SetParent(atomsRoot.transform);
            mgr.atomRoots[i] = slot;

            GameObject rim = new GameObject("Rim");
            rim.transform.SetParent(slot.transform);
            var lr = rim.AddComponent<LineRenderer>();
            mgr.atomRims[i] = lr;

            GameObject label = new GameObject("Label");
            label.transform.SetParent(slot.transform);
            var tmp = label.AddComponent<TMPro.TextMeshPro>();
            mgr.atomLabels[i] = tmp;

            GameObject aux = new GameObject("Aux");
            aux.transform.SetParent(slot.transform);
            var tmp2 = aux.AddComponent<TMPro.TextMeshPro>();
            mgr.atomAux[i] = tmp2;
        }

        // ---------- Bonds ----------
        GameObject bondsRoot = new GameObject("Bonds");
        bondsRoot.transform.SetParent(root);
        int bondCount = 10; // 必要に応じて変更
        mgr.bondRoots = new GameObject[bondCount];
        mgr.bondLine0 = new LineRenderer[bondCount];
        mgr.bondLine1 = new LineRenderer[bondCount];
        mgr.bondLine2 = new LineRenderer[bondCount];
        for (int i = 0; i < bondCount; i++)
        {
            GameObject slot = new GameObject("BondSlot_" + i);
            slot.transform.SetParent(bondsRoot.transform);
            mgr.bondRoots[i] = slot;

            mgr.bondLine0[i] = new GameObject("Line0").AddComponent<LineRenderer>();
            mgr.bondLine0[i].transform.SetParent(slot.transform);

            mgr.bondLine1[i] = new GameObject("Line1").AddComponent<LineRenderer>();
            mgr.bondLine1[i].transform.SetParent(slot.transform);

            mgr.bondLine2[i] = new GameObject("Line2").AddComponent<LineRenderer>();
            mgr.bondLine2[i].transform.SetParent(slot.transform);
        }

        // ---------- FlaskLooks ----------
        GameObject flaskRoot = new GameObject("FlaskLooks");
        flaskRoot.transform.SetParent(root);
        string[] keys = new string[] { "H", "O", "C" }; // 必要に応じて追加
        mgr.elementKeys = keys;
        mgr.flaskLooks = new GameObject[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            GameObject look = new GameObject("Liquid_" + keys[i]);
            look.transform.SetParent(flaskRoot.transform);
            look.SetActive(false);
            mgr.flaskLooks[i] = look;
        }

        // ---------- WorldFX ----------
        GameObject fxRoot = new GameObject("WorldFX");
        fxRoot.transform.SetParent(root);
        string[] fxNames = new string[] { "FX_Smoke", "FX_Spark", "FX_Flash" };
        mgr.worldFxSlots = new GameObject[fxNames.Length];
        for (int i = 0; i < fxNames.Length; i++)
        {
            GameObject fx = new GameObject(fxNames[i]);
            fx.transform.SetParent(fxRoot.transform);
            fx.SetActive(false);
            mgr.worldFxSlots[i] = fx;
        }

        // ---------- Spawner ----------
        GameObject spawnerRoot = new GameObject("Spawner");
        spawnerRoot.transform.SetParent(mgr.transform.parent); // SystemsやInteractablesに置きたい場合は調整
        var spawner = spawnerRoot.AddComponent<ChemElementSpawner>();
        spawner.envManager = mgr;

        GameObject poolRoot = new GameObject("SpawnPool");
        poolRoot.transform.SetParent(spawnerRoot.transform);
        int poolSize = 5;
        spawner.spawnPool = new GameObject[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            GameObject atomPrefab = new GameObject("AtomPrefab_" + i);
            atomPrefab.transform.SetParent(poolRoot.transform);
            atomPrefab.SetActive(false);
            var ctrl = atomPrefab.AddComponent<ChemVisualController>();
            ctrl.env = mgr;
            spawner.spawnPool[i] = atomPrefab;
        }

        EditorUtility.SetDirty(mgr);
        Debug.Log("ChemEnvironmentManager セットアップ完了！");
    }
}
#endif
