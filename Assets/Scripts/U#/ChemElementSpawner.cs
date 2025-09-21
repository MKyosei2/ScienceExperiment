// ===============================
// ChemElementSpawner.cs
// VRCSDK3 + UdonSharp 前提
// ===============================

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ChemElementSpawner : UdonSharpBehaviour
{
    [Header("▼ 生成するフラスコ（CONICAL_FLASK）")]
    public GameObject conicalFlaskPrefab;

    [Header("▼ 生成位置/親")]
    public Transform spawnPoint;
    public Transform parentRoot;

    [Header("▼ 見た目適用（別スクリプト）")]
    public ChemVisualController visualController;

    [Header("▼ オプション")]
    public bool replaceIfExists = true;

    private GameObject _currentFlask;

    public void SpawnElement(int elementId)
    {
        if (_currentFlask != null)
        {
            if (!replaceIfExists) return;

            if (!Networking.IsOwner(_currentFlask))
            {
                var lp = Networking.LocalPlayer;
                if (lp != null) Networking.SetOwner(lp, _currentFlask);
            }
            Networking.Destroy(_currentFlask);
            _currentFlask = null;
        }

        Transform baseTf = (spawnPoint != null) ? spawnPoint : this.transform;
        Vector3 pos = baseTf.position;
        Quaternion rot = baseTf.rotation;
        Transform parent = (parentRoot != null) ? parentRoot : null;

        if (conicalFlaskPrefab == null)
        {
            Debug.LogError("[ChemElementSpawner] conicalFlaskPrefab が未設定です。");
            return;
        }

        _currentFlask = VRCInstantiate(conicalFlaskPrefab);
        if (_currentFlask == null)
        {
            Debug.LogError("[ChemElementSpawner] フラスコ生成に失敗しました。");
            return;
        }

        _currentFlask.transform.SetPositionAndRotation(pos, rot);
        if (parent != null) _currentFlask.transform.SetParent(parent, true);

        if (visualController != null)
        {
            visualController.ApplyElementVisual(_currentFlask, elementId);
        }
    }

    public void ResetExperiment()
    {
        if (_currentFlask != null)
        {
            if (visualController != null) visualController.ClearLiquid(_currentFlask);

            if (!Networking.IsOwner(_currentFlask))
            {
                var lp = Networking.LocalPlayer;
                if (lp != null) Networking.SetOwner(lp, _currentFlask);
            }
            Networking.Destroy(_currentFlask);
            _currentFlask = null;
        }
    }
}
