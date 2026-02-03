using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Buttons")]
    [SerializeField] private GameObject playButton;

    private GameManager gameManager;

    void Start()
    {
        gameManager = GameManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlayGame()
    {
        gameManager.StartGame();
    }

    public void HidePlayButton()
    {
        playButton.SetActive(false);
    }

    public void ShowPlayButton()
    {
        playButton.SetActive(true);
    }
}