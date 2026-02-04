using System.Collections.Generic;
using UnityEngine;

public class FirstController : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private StageManager stageManager;

    [Header("Dialogue")]
    [SerializeField] private List<string> openingDialogueLines;

    [Header("Prefabs")]
    [SerializeField] private GameObject dollPrefab;
    [SerializeField] private GameObject cranePrefab;
    [SerializeField] private GameObject goalPrefab;

    [Header("Parents")]
    [SerializeField] private Transform worldParent;

    [Header("UI")]
    [SerializeField] private GameObject stagePanel;

    private GameObject doll;
    private GameObject crane;
    private GameObject goal;

    private bool hasPlayedDialogue;

    // =============================
    // LIFECYCLE
    // =============================

    void Awake()
    {
        SpawnWorldObjects();   // 🔹 spawn SEKALI
        HideWorldObjects();
    }

    void OnEnable()
    {
        stagePanel.SetActive(true);

        ResetWorldObjects();  // 🔹 reset state
        ShowWorldObjects();

        TryPlayOpeningDialogue();
    }

    void OnDisable()
    {
        stagePanel.SetActive(false);
        HideWorldObjects();
    }

    void OnDestroy()
    {
        if (NarratorManager.Instance != null)
            NarratorManager.Instance.OnDialogueFinished -= OnDialogueFinished;
    }

    // =============================
    // SPAWN & RESET
    // =============================

    private void SpawnWorldObjects()
    {
        doll = Instantiate(dollPrefab, worldParent);
        crane = Instantiate(cranePrefab, worldParent);
        goal = Instantiate(goalPrefab, worldParent);
    }

    private void ResetWorldObjects()
    {
        doll.transform.localPosition = dollPrefab.transform.localPosition;
        crane.transform.localPosition = cranePrefab.transform.localPosition;
        goal.transform.localPosition = goalPrefab.transform.localPosition;

        // reset tambahan (physics, state) nanti di sini
    }

    private void HideWorldObjects()
    {
        doll.SetActive(false);
        crane.SetActive(false);
        goal.SetActive(false);
    }

    private void ShowWorldObjects()
    {
        doll.SetActive(true);
        crane.SetActive(true);
        goal.SetActive(true);
    }

    // =============================
    // DIALOGUE
    // =============================

    private void TryPlayOpeningDialogue()
    {
        if (hasPlayedDialogue) return;
        if (openingDialogueLines == null || openingDialogueLines.Count == 0) return;
        if (NarratorManager.Instance == null) return;

        hasPlayedDialogue = true;
        NarratorManager.Instance.OnDialogueFinished += OnDialogueFinished;
        NarratorManager.Instance.PlayDialogue(openingDialogueLines);
    }

    private void OnDialogueFinished()
    {
        if (NarratorManager.Instance != null)
            NarratorManager.Instance.OnDialogueFinished -= OnDialogueFinished;

        // di sini nanti:
        // enable input
        // atau mulai objective
    }
}
