using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NarratorManager : MonoBehaviour
{
    public static NarratorManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI narratorText;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip narratorClip;

    [Header("Settings")]
    [SerializeField] private float defaultDelay = 2f;

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
    }

    public void PlayDialogue(List<string> lines)
    {
        StopAllCoroutines();
        StartCoroutine(PlayDialogueRoutine(lines));
    }

    private IEnumerator PlayDialogueRoutine(List<string> lines)
    {
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
}
