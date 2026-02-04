using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class SecondController : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private StageManager stageManager;

    [Header("Dialogues")]
    [SerializeField] private List<string> openingDialog;
    [SerializeField] private List<string> successDialogB;
    [SerializeField] private List<string> failDialogEarly;   // klik saat opening
    [SerializeField] private List<string> dialogueAfterFailEarly;   // setelah dialog fail early
    [SerializeField] private List<string> failDialogAfter;   // klik setelah opening
    [SerializeField] private List<string> dialogD; // Dialog kalau player gamau klik tombol selanjutnya
    [SerializeField] private List<string> dialogE; // Dialog sebelum auto lanjut ke stage berikutnya
    [SerializeField] private List<string> AtClickConfusing; // dialog saat player klik bingung
    [SerializeField] private List<string> AtClickAnnoying; // dialog saat player klik kesal di opening
    [SerializeField] private List<string> AtFakeButtonUp; // dialog saat tombol palsu muncul
    [SerializeField] private List<string> AtClickFakeButton; // dialog saat player klik tombol palsu
    [SerializeField] private List<string> successDialogWithFakeButton; // dialog sukses saat tombol palsu aktif

    [Header("Timing")]
    [SerializeField] private float idleToSuccessTime = 9f;
    [SerializeField] private float waitAfterBToD = 4f;
    [SerializeField] private float waitAfterDToE = 4f;
    [SerializeField] private float followUpDialogDelay = 2f;

    [Header("UI")]
    [SerializeField] private GameObject stagePanel;
    [SerializeField] private GameObject fakeButton;
    [SerializeField] private GameObject actionButton;

    private enum StageState
    {
        Opening,
        WarningPhase,  // NEW: State khusus untuk warning + follow up
        Idle,
        DialogB,
        WaitingAfterB,
        DialogD,       // NEW: State untuk dialog D
        DialogE,       // NEW: State untuk dialog E
        Failed,
        Finished
    }

    private StageState state;
    private Coroutine runningRoutine;
    private int idleRandomClickCount;
    private int openingRandomClickCount;

    // Variable untuk menyimpan follow-up dialog yang akan dimainkan
    private List<string> pendingFollowUpDialog;

    // =========================
    // LIFECYCLE
    // =========================

    void OnEnable()
    {
        Debug.Log("SecondController OnEnable");
        stagePanel.SetActive(true);
        state = StageState.Opening;
        PlayDialogue(openingDialog);
    }

    void OnDisable()
    {
        Debug.Log("SecondController OnDisable");
        stagePanel.SetActive(false);
        ResetAllState();
    }

    void Update()
    {
        if (state == StageState.Opening && Input.GetMouseButtonDown(0))
        {
            HandleRandomClickAtOpening();
        }

        if (state == StageState.Idle && Input.GetMouseButtonDown(0))
        {
            if (!IsClickOnButton())
                HandleRandomClickAtIdle();
        }
    }

    void OnDestroy()
    {
        CleanupEventListeners();
    }

    // =========================
    // INPUT
    // =========================

    public void OnActionButtonPressed()
    {
        if (state == StageState.Idle)
        {
            Fail(failDialogAfter);
            return;
        }

        if (state == StageState.WaitingAfterB)
        {
            GoToNextStage();
        }
    }

    public void OnFakeButtonPressed()
    {
        Fail(AtClickFakeButton);
    }

    // =========================
    // DIALOGUE FLOW
    // =========================

    private void PlayDialogue(List<string> dialog)
    {
        if (dialog == null || dialog.Count == 0) return;
        if (NarratorManager.Instance == null)
        {
            Debug.LogWarning("NarratorManager instance not found.");
            return;
        }

        NarratorManager.Instance.OnDialogueFinished -= OnDialogueFinished;
        NarratorManager.Instance.OnDialogueFinished += OnDialogueFinished;
        NarratorManager.Instance.PlayDialogue(dialog);
    }

    private void OnDialogueFinished()
    {
        if (NarratorManager.Instance != null)
            NarratorManager.Instance.OnDialogueFinished -= OnDialogueFinished;

        switch (state)
        {
            case StageState.Opening:
                Debug.Log("Opening dialogue finished");
                state = StageState.Idle;
                StartIdleTimer();
                break;

            case StageState.WarningPhase:
                Debug.Log("Warning dialogue finished, playing follow-up");
                StartCoroutine(PlayFollowUpAfterDelay());
                break;

            case StageState.DialogB:
                Debug.Log("Success dialogue B finished");
                state = StageState.WaitingAfterB;
                actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue";
                StartForcedProgress();
                break;

            case StageState.DialogD:
                Debug.Log("Dialog D finished, waiting before E");
                StartCoroutine(WaitAndPlayDialogE());
                break;

            case StageState.DialogE:
                Debug.Log("Dialog E finished, auto progressing");
                GoToNextStage();
                break;

            case StageState.Failed:
                Debug.Log("Fail dialogue finished, restarting stage");
                stageManager.RestartStage();
                break;
        }
    }

    // =========================
    // IDLE SUCCESS
    // =========================

    private void StartIdleTimer()
    {
        StopAllRunning();
        runningRoutine = StartCoroutine(IdleSuccessRoutine());
    }

    private IEnumerator IdleSuccessRoutine()
    {
        yield return new WaitForSeconds(idleToSuccessTime);

        state = StageState.DialogB;
        if (fakeButton.activeSelf)
            PlayDialogue(successDialogWithFakeButton);
        else
            PlayDialogue(successDialogB);
    }

    // =========================
    // FORCE PLAYER (D → E → AUTO)
    // =========================

    private void StartForcedProgress()
    {
        StopAllRunning();
        runningRoutine = StartCoroutine(ForcedProgressRoutine());
    }

    private IEnumerator ForcedProgressRoutine()
    {
        yield return new WaitForSeconds(waitAfterBToD);
        Debug.Log("Playing Dialog D");
        state = StageState.DialogD;
        PlayDialogue(dialogD);
        // OnDialogueFinished akan handle transisi ke Dialog E
    }

    private IEnumerator WaitAndPlayDialogE()
    {
        yield return new WaitForSeconds(waitAfterDToE);
        Debug.Log("Playing Dialog E");
        state = StageState.DialogE;
        PlayDialogue(dialogE);
        // OnDialogueFinished akan handle auto progress ke next stage
    }

    // =========================
    // RANDOM CLICK HANDLING
    // =========================

    private bool IsClickOnButton()
    {
        PointerEventData data = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);

        foreach (var r in results)
        {
            if (r.gameObject.GetComponent<Button>() != null)
                return true;
        }

        return false;
    }

    private void HandleRandomClickAtOpening()
    {
        openingRandomClickCount++;

        if (openingRandomClickCount == 1)
        {
            WarningWithFollowUp(failDialogEarly, dialogueAfterFailEarly);
        }
        else if (openingRandomClickCount == 2)
        {
            Fail(AtClickAnnoying);
        }
    }

    private void HandleRandomClickAtIdle()
    {
        idleRandomClickCount++;

        if (idleRandomClickCount == 1)
        {
            StartIdleTimer(); // reset timer
            PlayDialogue(AtClickConfusing);
        }
        else if (idleRandomClickCount == 3)
        {
            StartIdleTimer(); // reset timer
            fakeButton.SetActive(true);
            PlayDialogue(AtFakeButtonUp);
        }
    }

    // =========================
    // FAIL & CLEANUP
    // =========================

    private void Fail(List<string> dialog)
    {
        StopAllRunning();
        state = StageState.Failed;
        PlayDialogue(dialog);
    }

    private void WarningWithFollowUp(List<string> warningDialog, List<string> followUpDialog)
    {
        StopAllRunning();
        state = StageState.WarningPhase;
        pendingFollowUpDialog = followUpDialog;
        PlayDialogue(warningDialog);
        // OnDialogueFinished akan handle follow-up
    }

    private IEnumerator PlayFollowUpAfterDelay()
    {
        yield return new WaitForSeconds(followUpDialogDelay);

        // Kembali ke state Opening setelah follow-up
        state = StageState.Opening;
        PlayDialogue(pendingFollowUpDialog);
        pendingFollowUpDialog = null;
    }

    private void StopAllRunning()
    {
        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            runningRoutine = null;
        }
    }

    private void GoToNextStage()
    {
        state = StageState.Finished;
        StopAllRunning();
        CleanupEventListeners();
        stageManager.GoToNextStage();
    }

    private void CleanupEventListeners()
    {
        if (NarratorManager.Instance != null)
        {
            NarratorManager.Instance.OnDialogueFinished -= OnDialogueFinished;
        }
    }

    private void ResetAllState()
    {
        state = StageState.Opening;
        StopAllRunning();
        CleanupEventListeners();

        idleRandomClickCount = 0;
        openingRandomClickCount = 0;
        pendingFollowUpDialog = null;

        // Reset UI
        if (fakeButton != null)
            fakeButton.SetActive(false);

        if (actionButton != null)
        {
            var buttonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "DON'T PRESS"; // atau text default-nya
        }
    }
}