using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Total Levels")]
    [SerializeField] private int totalLevels = 2;

    private int level = 0;
    public int stage { get; private set; }

    public static GameManager Instance { get; private set; }

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

    public void StartGame()
    {
        Debug.Log("Game Started!");
        // Add game start logic here
        if (IsFirstLaunch())
        {
            Debug.Log("Initialized to Level 1, Stage 1");
            SceneManager.LoadScene("Level-0");
        }

        else
        {
            Debug.Log($"Continuing from Level {level}, Stage {stage}");
            SceneManager.LoadScene($"Level-{level}");
        }
    }

    public void GoToNextLevel()
    {
        if (level < totalLevels)
        {
            level++;
            stage = 0; // reset stage for new level
            SceneManager.LoadScene($"Level-{level}");
        }
        else
        {
            Debug.Log("Already at the highest level.");
            SceneManager.LoadScene("EndingScene");
        }
    }

    public bool IsFirstLaunch()
    {
        if (level == 0 && stage == 0)
        {
            Debug.Log("First Launch: Set to Level 1, Stage 1");
            return true;
        }
        return false;
    }

    public void SetStage(int value)
    {
        stage = value;
    }

    public void SetLevel(int value)
    {
        level = value;
    }

}
