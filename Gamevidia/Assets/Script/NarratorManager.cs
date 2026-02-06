using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NarratorManager : MonoBehaviour
{
    public static NarratorManager Instance { get; private set; }

    [Serializable]
    public class DialogueLine
    {
        [TextArea]
        public string text;
        public AudioClip clip;
        [Tooltip("Optional override duration in seconds. Use -1 to auto (clip length or default delay).")]
        public float overrideDelay = -1f;
    }

    [Header("UI")]
    [SerializeField] private GameObject canvasPrefab;
    private GameObject canvasInstance;
    private TextMeshProUGUI narratorText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip narratorClip;

    [Header("Settings")]
    [SerializeField] private float defaultDelay = 2f;
    [SerializeField] private float defaultPostClipDelay = 0.2f;

    public event Action OnDialogueFinished;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureCanvas();
    }

    public void PlayDialogue(List<string> lines)
    {
        StopAllCoroutines();
        StartCoroutine(PlayDialogueRoutine(lines));
    }

    public void PlayDialogue(List<DialogueLine> lines)
    {
        StopAllCoroutines();
        StartCoroutine(PlayDialogueRoutine(lines));
    }

    private IEnumerator PlayDialogueRoutine(List<string> lines)
    {
        EnsureCanvas();
        foreach (var line in lines)
        {
            narratorText.text = line;

            // 🔊 MAINKAN SUARA NARATOR
            if (audioSource != null && narratorClip != null)
                audioSource.PlayOneShot(narratorClip);

            yield return new WaitForSeconds(defaultDelay);
        }

        narratorText.text = "";
        OnDialogueFinished?.Invoke();
    }

    private IEnumerator PlayDialogueRoutine(List<DialogueLine> lines)
    {
        EnsureCanvas();
        foreach (var line in lines)
        {
            narratorText.text = line != null ? line.text : "";

            float waitTime = defaultDelay;
            if (line != null)
            {
                if (line.overrideDelay >= 0f)
                {
                    waitTime = line.overrideDelay;
                }
                else if (line.clip != null)
                {
                    waitTime = line.clip.length + defaultPostClipDelay;
                }
            }

            if (audioSource != null && line != null && line.clip != null)
            {
                audioSource.PlayOneShot(line.clip);
            }
            else if (audioSource != null && narratorClip != null)
            {
                audioSource.PlayOneShot(narratorClip);
            }

            yield return new WaitForSeconds(waitTime);
        }

        narratorText.text = "";
        OnDialogueFinished?.Invoke();
    }

    private void EnsureCanvas()
    {
        if (canvasInstance == null && canvasPrefab != null)
        {
            canvasInstance = Instantiate(canvasPrefab, transform);
        }

        if (narratorText == null && canvasInstance != null)
            narratorText = canvasInstance.GetComponentInChildren<TextMeshProUGUI>(true);

        if (narratorText == null)
        {
            Debug.LogWarning("NarratorManager: TextMeshProUGUI not found. Assign it or provide a Canvas prefab with a TextMeshProUGUI child.");
        }
    }
}
