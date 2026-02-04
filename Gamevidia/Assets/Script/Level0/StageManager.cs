using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject Stage1;
    [SerializeField] private GameObject Stage2;
    // [SerializeField] private GameObject Stage3;

    [Header("Total Stages")]
    [SerializeField] private int totalStages = 2;

    private int stage = 0;

    void Start()
    {
        stage = GameManager.Instance.stage;
        if (stage == 0)
        {
            stage = 1;
            GameManager.Instance.SetStage(stage);
        }

        ShowStagePanel();
    }

    private void HideAllStagePanels()
    {
        Stage1.SetActive(false);
        Stage2.SetActive(false);
        // Stage3.SetActive(false);
    }

    public void ShowStagePanel()
    {
        HideAllStagePanels();

        switch (stage)
        {
            case 1:
                Stage1.SetActive(true);
                break;
            case 2:
                Stage2.SetActive(true);
                break;
            // case 3:
            //     Stage3.SetActive(true);
            //     break;
            default:
                Debug.LogWarning("Invalid stage number: " + stage);
                break;
        }
    }

    public void RestartStage()
    {
        ShowStagePanel();
    }

    public void GoToNextStage()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager instance not found.");
            return;
        }

        if (stage >= totalStages)
        {
            Debug.Log("Already at the last stage. Cannot go to next stage.");
            gameManager.GoToNextLevel();
            return;
        }

        gameManager.SetStage(gameManager.stage + 1);
        stage = gameManager.stage;
        ShowStagePanel();
    }
}