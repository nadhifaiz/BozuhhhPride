using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StageManagement : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject gamePanel_A;
    [SerializeField] private GameObject gamePanel_B;

    [Header("Difficulty Buttons")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button hardButton;

    [Header("Navigation Buttons")]
    [SerializeField] private Button leftButton;  // Ke Game B
    [SerializeField] private Button rightButton; // Ke Game A

    // State Management
    public enum Difficulty { Easy, Hard }
    private Difficulty currentDifficulty;

    // Singleton pattern (opsional, tapi berguna untuk akses global)
    public static StageManagement Instance { get; private set; }

    // Property untuk akses difficulty dari script lain
    public Difficulty CurrentDifficulty => currentDifficulty;

    private void Awake()
    {
        // Setup singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Setup button listeners
        easyButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Easy));
        hardButton.onClick.AddListener(() => OnDifficultySelected(Difficulty.Hard));

        leftButton.onClick.AddListener(ShowGamePanel_A);
        rightButton.onClick.AddListener(ShowGamePanel_B);

        // Initialize - tampilkan panel difficulty dulu
        InitializePanels();
    }

    private void InitializePanels()
    {
        difficultyPanel.SetActive(true);
        leftButton.gameObject.SetActive(false);
        rightButton.gameObject.SetActive(false);
        gamePanel_A.SetActive(false);
        gamePanel_B.SetActive(false);
    }

    private void OnDifficultySelected(Difficulty difficulty)
    {
        // Simpan state kesulitan
        currentDifficulty = difficulty;

        Debug.Log($"Difficulty selected: {difficulty}");

        //Simulasi sembunyikan panel difficulty dan tampilkan navigasi
        easyButton.GameObject().SetActive(false);
        hardButton.GameObject().SetActive(false);

        leftButton.gameObject.SetActive(true);
        rightButton.gameObject.SetActive(true);

        // Opsional: Notify game controllers tentang difficulty yang dipilih
        NotifyGameControllers();
    }

    public void ShowGamePanel_A()
    {
        gamePanel_A.SetActive(true);
        gamePanel_B.SetActive(false);

        Debug.Log("Switched to Game Panel A");
    }

    public void ShowGamePanel_B()
    {
        gamePanel_A.SetActive(false);
        gamePanel_B.SetActive(true);

        Debug.Log("Switched to Game Panel B");
    }

    public void ShowDifficultyPanel()
    {
        difficultyPanel.SetActive(true);
        gamePanel_A.SetActive(false);
        gamePanel_B.SetActive(false);

        leftButton.gameObject.SetActive(true);
        rightButton.gameObject.SetActive(true);

        easyButton.gameObject.SetActive(false);
        hardButton.GameObject().SetActive(false);

        Debug.Log("Returned to Difficulty Selection Panel");
    }

    private void NotifyGameControllers()
    {
        // Kirim notifikasi ke game controllers (jika diperlukan)
        // Contoh:
        // GameController_A.Instance?.SetDifficulty(currentDifficulty);
        // GameController_B.Instance?.SetDifficulty(currentDifficulty);
    }

    // Method helper untuk mendapatkan difficulty dari script lain
    public bool IsHardMode()
    {
        return currentDifficulty == Difficulty.Hard;
    }

    private void OnDestroy()
    {
        // Cleanup listeners
        easyButton.onClick.RemoveAllListeners();
        hardButton.onClick.RemoveAllListeners();
        leftButton.onClick.RemoveAllListeners();
        rightButton.onClick.RemoveAllListeners();
    }
}