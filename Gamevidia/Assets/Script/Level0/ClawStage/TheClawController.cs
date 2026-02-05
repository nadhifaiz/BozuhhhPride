using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheClawController : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private StageManager stageManager;

    [Header("Dialogues")]
    [SerializeField] private List<string> openingDialogue;
    [SerializeField] private List<string> narratorShutdownMovement;
    [SerializeField] private List<string> narratorShutdownGrab;
    [SerializeField] private List<string> afterThreeDrops;
    [SerializeField] private List<string> hintCantTouch;
    [SerializeField] private List<string> successQuick;
    [SerializeField] private List<string> successSlow;

    [Header("Prefabs")]
    [SerializeField] private GameObject dollPrefab;
    [SerializeField] private GameObject cranePrefab;
    [SerializeField] private GameObject goalPrefab;

    [Header("Parents")]
    [SerializeField] private Transform worldParent;

    [Header("UI")]
    [SerializeField] private TutorialIconManager tutorialIconManager;

    [Header("Settings")]
    [SerializeField] private float movementTutorialDelay = 0.5f;
    [SerializeField] private float grabSpoilerDelay = 4f;        // Waktu tunggu sebelum spoil grab
    [SerializeField] private int maxDropsBeforeHint = 3;
    [SerializeField] private float hintDelay1 = 5f;
    [SerializeField] private float hintDelay2 = 10f;
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeDuration = 1f;

    private GameObject doll;
    private GameObject crane;
    private GameObject goal;

    private CraneController craneController;
    private DollDraggable dollDraggable;
    private GoalTrigger goalTrigger;

    private enum StageState
    {
        OpeningDialogue,
        ShowMovementTutorial,
        NarratorShutdownMovement,
        WaitingForFirstGrab,      // WAITING + TIMER
        ShowGrabTutorial,         // SPOILER (optional)
        NarratorShutdownGrab,     // SPOILER shutdown (optional)
        ClawGamePhase,
        EnableDragHint,
        WaitingForDrag,
        Success,
        Finished
    }

    private StageState state;
    private int dropCount = 0;
    private float dragWaitTimer = 0f;
    private bool hasShownHint1 = false;
    private bool hasShownHint2 = false;
    private bool quickSuccess = false;
    private Coroutine shakeRoutine;
    private Coroutine grabSpoilerRoutine; // NEW: Timer untuk spoiler grab

    // =============================
    // LIFECYCLE
    // =============================

    void Awake()
    {
        SpawnWorldObjects();
        HideWorldObjects();
    }

    void OnEnable()
    {
        EnsureWorldObjects();
        ResetWorldObjects();
        ShowWorldObjects();
        HookEventListeners();

        state = StageState.OpeningDialogue;
        PlayDialogue(openingDialogue);
    }

    void OnDisable()
    {
        HideWorldObjects();
        CleanupEventListeners();
    }

    void OnDestroy()
    {
        CleanupEventListeners();
    }

    void Update()
    {
        if (state == StageState.WaitingForDrag)
        {
            HandleDragWaitTimer();
        }
    }

    // =============================
    // SPAWN & RESET
    // =============================

    private void SpawnWorldObjects()
    {
        doll = Instantiate(dollPrefab, worldParent);
        crane = Instantiate(cranePrefab, worldParent);
        goal = Instantiate(goalPrefab, worldParent);

        craneController = crane.GetComponent<CraneController>();
        dollDraggable = doll.GetComponent<DollDraggable>();
        goalTrigger = goal.GetComponent<GoalTrigger>();
    }

    private void EnsureWorldObjects()
    {
        if (doll == null || crane == null || goal == null)
        {
            SpawnWorldObjects();
            return;
        }

        if (craneController == null)
            craneController = crane.GetComponent<CraneController>();

        if (dollDraggable == null)
            dollDraggable = doll.GetComponent<DollDraggable>();

        if (goalTrigger == null)
            goalTrigger = goal.GetComponent<GoalTrigger>();
    }

    private void ResetWorldObjects()
    {
        doll.transform.position = dollPrefab.transform.position;
        crane.transform.position = cranePrefab.transform.position;
        goal.transform.position = goalPrefab.transform.position;

        dropCount = 0;
        dragWaitTimer = 0f;
        hasShownHint1 = false;
        hasShownHint2 = false;
        quickSuccess = false;

        if (dollDraggable != null)
            dollDraggable.SetDraggable(false);

        if (craneController != null)
            craneController.SetInputEnabled(false);

        if (tutorialIconManager != null)
            tutorialIconManager.HideAllIcons();

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (grabSpoilerRoutine != null)
        {
            StopCoroutine(grabSpoilerRoutine);
            grabSpoilerRoutine = null;
        }
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
    // DIALOGUE FLOW
    // =============================

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

        Debug.Log($"[MAIN] OnDialogueFinished - State: {state}");

        switch (state)
        {
            case StageState.OpeningDialogue:
                state = StageState.ShowMovementTutorial;
                StartCoroutine(ShowMovementTutorialSequence());
                break;

            case StageState.NarratorShutdownMovement:
                state = StageState.WaitingForFirstGrab;
                if (craneController != null)
                    craneController.SetInputEnabled(true);

                // START TIMER untuk spoiler grab
                grabSpoilerRoutine = StartCoroutine(GrabSpoilerTimer());
                break;

            case StageState.NarratorShutdownGrab:
                // Dialog spoiler selesai, langsung ke game phase
                state = StageState.ClawGamePhase;
                break;

            case StageState.EnableDragHint:
                state = StageState.WaitingForDrag;
                if (dollDraggable != null)
                    dollDraggable.SetDraggable(true);
                dragWaitTimer = 0f;
                break;

            case StageState.Success:
                state = StageState.Finished;
                GoToNextStage();
                break;
        }
    }

    // =============================
    // TUTORIAL SEQUENCES
    // =============================

    private IEnumerator ShowMovementTutorialSequence()
    {
        yield return new WaitForSeconds(movementTutorialDelay);

        if (tutorialIconManager != null)
            tutorialIconManager.ShowMovementIcon();

        yield return new WaitForSeconds(movementTutorialDelay);

        state = StageState.NarratorShutdownMovement;
        if (tutorialIconManager != null)
            tutorialIconManager.HideMovementIcon();

        PlayDialogue(narratorShutdownMovement);
    }

    // NEW: Timer untuk spoiler grab (jika player gak nemu dalam X detik)
    private IEnumerator GrabSpoilerTimer()
    {
        Debug.Log($"[MAIN] ⏳ Grab spoiler timer started ({grabSpoilerDelay}s)");
        yield return new WaitForSeconds(grabSpoilerDelay);

        // Jika masih di state WaitingForFirstGrab (player belum grab)
        if (state == StageState.WaitingForFirstGrab)
        {
            Debug.Log("[MAIN] ⚠️ Player didn't grab, showing spoiler");
            state = StageState.ShowGrabTutorial;

            if (tutorialIconManager != null)
                tutorialIconManager.ShowGrabIcon();

            yield return new WaitForSeconds(movementTutorialDelay);

            state = StageState.NarratorShutdownGrab;
            if (tutorialIconManager != null)
                tutorialIconManager.HideGrabIcon();

            PlayDialogue(narratorShutdownGrab);
        }
        else
        {
            Debug.Log("[MAIN] ✅ Player already grabbed, spoiler cancelled");
        }

        grabSpoilerRoutine = null;
    }

    // =============================
    // GAME EVENTS
    // =============================

    // NEW: Dipanggil saat player pertama kali tekan Space
    private void OnFirstGrabAttempt()
    {
        Debug.Log($"[MAIN] 🎯 OnFirstGrabAttempt - State: {state}");

        if (state == StageState.WaitingForFirstGrab)
        {
            Debug.Log("[MAIN] ✅ Player grabbed before spoiler, cancelling timer");

            // CANCEL spoiler timer
            if (grabSpoilerRoutine != null)
            {
                StopCoroutine(grabSpoilerRoutine);
                grabSpoilerRoutine = null;
            }

            // Langsung ke game phase (skip spoiler)
            state = StageState.ClawGamePhase;
        }
    }

    private void OnDollDropped()
    {
        Debug.Log($"[MAIN] 💧 Doll dropped - State: {state}");

        if (state != StageState.ClawGamePhase) return;

        dropCount++;
        Debug.Log($"[MAIN] Drop count: {dropCount}/{maxDropsBeforeHint}");

        if (dropCount >= maxDropsBeforeHint)
        {
            state = StageState.EnableDragHint;
            PlayDialogue(afterThreeDrops);
        }
    }

    private void OnDollEntered()
    {
        Debug.Log($"[MAIN] 🎯 Doll entered basket - State: {state}");

        if (state == StageState.WaitingForDrag)
        {
            quickSuccess = dragWaitTimer < hintDelay1;

            state = StageState.Success;

            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                shakeRoutine = null;
            }

            if (quickSuccess)
            {
                Debug.Log("[MAIN] ⚡ Quick success!");
                PlayDialogue(successQuick);
            }
            else
            {
                Debug.Log("[MAIN] 🐌 Slow success");
                PlayDialogue(successSlow);
            }
        }
    }

    // =============================
    // DRAG WAIT HINTS
    // =============================

    private void HandleDragWaitTimer()
    {
        dragWaitTimer += Time.deltaTime;

        if (!hasShownHint1 && dragWaitTimer >= hintDelay1)
        {
            hasShownHint1 = true;
            Debug.Log("[MAIN] 💡 Hint 1: can't touch");
            PlayDialogue(hintCantTouch);
        }

        if (!hasShownHint2 && dragWaitTimer >= hintDelay2)
        {
            hasShownHint2 = true;
            Debug.Log("[MAIN] 💡 Hint 2: shake doll");
            shakeRoutine = StartCoroutine(ShakeDoll());
        }
    }

    private IEnumerator ShakeDoll()
    {
        Vector3 originalPos = doll.transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + Random.Range(-shakeIntensity, shakeIntensity);
            float y = originalPos.y + Random.Range(-shakeIntensity, shakeIntensity);
            doll.transform.position = new Vector3(x, y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        doll.transform.position = originalPos;
        shakeRoutine = null;
    }

    // =============================
    // CLEANUP
    // =============================

    private void HookEventListeners()
    {
        if (craneController != null)
        {
            craneController.OnDollDropped -= OnDollDropped;
            craneController.OnDollDropped += OnDollDropped;

            craneController.OnFirstGrabAttempt -= OnFirstGrabAttempt;
            craneController.OnFirstGrabAttempt += OnFirstGrabAttempt;
        }

        if (goalTrigger != null)
        {
            goalTrigger.OnDollEntered -= OnDollEntered;
            goalTrigger.OnDollEntered += OnDollEntered;
        }
    }

    private void CleanupEventListeners()
    {
        if (NarratorManager.Instance != null)
            NarratorManager.Instance.OnDialogueFinished -= OnDialogueFinished;

        if (craneController != null)
        {
            craneController.OnDollDropped -= OnDollDropped;
            craneController.OnFirstGrabAttempt -= OnFirstGrabAttempt;
        }

        if (goalTrigger != null)
        {
            goalTrigger.OnDollEntered -= OnDollEntered;
        }
    }

    private void GoToNextStage()
    {
        CleanupEventListeners();
        stageManager.GoToNextStage();
    }
}


