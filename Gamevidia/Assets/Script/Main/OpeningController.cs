using System.Collections.Generic;
using UnityEngine;

public class OpeningController : MonoBehaviour
{
    [SerializeField] private MainUIManager uiManager;

    [Header("TextDialogue")]
    [SerializeField] private List<NarratorManager.DialogueLine> openingDialogueLines;

    void Start()
    {
        if (!GameManager.Instance.IsFirstLaunch())
        {
            uiManager.ShowPlayButton();
            return;
        }

        uiManager.HidePlayButton();

        NarratorManager.Instance.OnDialogueFinished += OnOpeningFinished;

        NarratorManager.Instance.PlayDialogue(openingDialogueLines);
    }

    private void OnOpeningFinished()
    {
        NarratorManager.Instance.OnDialogueFinished -= OnOpeningFinished;

        uiManager.ShowPlayButton();
    }
}
