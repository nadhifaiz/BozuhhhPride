using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrdinaryMinesweeper : MonoBehaviour
{
    public static OrdinaryMinesweeper Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private Button switchToPanelA_Button;

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private int easyBombCount = 10;
    [SerializeField] private int hardBombCount = 10;

    [Header("Hard Mode Patterns")]
    [SerializeField] private List<MinePattern> minePatterns = new List<MinePattern>();

    // Game State
    private MinesweeperTile[,] tiles;
    private bool[,] bombs;
    private int[,] adjacentBombCount;
    private StageManagement.Difficulty currentDifficulty;
    private bool isHardMode;
    private bool gameStarted;
    private bool gameOver;

    // Hard Mode State
    private MinePattern currentPattern;
    private int hardModeDeathCount = 0;

    // Easy Mode State
    private int easyModeFlagCount = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Get difficulty from StageManagement
        if (StageManagement.Instance != null)
        {
            currentDifficulty = StageManagement.Instance.CurrentDifficulty;
            isHardMode = (currentDifficulty == StageManagement.Difficulty.Hard);
        }

        // Setup navigation button
        if (switchToPanelA_Button != null)
        {
            switchToPanelA_Button.onClick.AddListener(OnSwitchToPanelA);
        }

        // Initialize game
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        // Update difficulty text
        if (difficultyText != null)
        {
            difficultyText.text = isHardMode ? "Difficulty: High" : "Difficulty: Low";
        }

        // Create grid
        CreateGrid();

        // Generate bombs based on difficulty
        if (isHardMode)
        {
            GenerateHardModeBombs();
        }
        else
        {
            GenerateEasyModeBombs();
        }

        // Calculate adjacent bombs (only for easy mode)
        if (!isHardMode)
        {
            CalculateAdjacentBombs();
        }

        // Play intro dialogue
        yield return StartCoroutine(PlayIntroDialogue());

        gameStarted = true;
    }

    private void CreateGrid()
    {
        tiles = new MinesweeperTile[gridWidth, gridHeight];
        bombs = new bool[gridWidth, gridHeight];
        adjacentBombCount = new int[gridWidth, gridHeight];

        // Clear existing tiles
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        // Create tiles
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                GameObject tileObj = Instantiate(tilePrefab, gridParent);
                MinesweeperTile tile = tileObj.GetComponent<MinesweeperTile>();

                if (tile != null)
                {
                    tile.Initialize(x, y, this);
                    tiles[x, y] = tile;
                }
            }
        }
    }

    private void GenerateEasyModeBombs()
    {
        int bombsPlaced = 0;
        while (bombsPlaced < easyBombCount)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);

            if (!bombs[x, y])
            {
                bombs[x, y] = true;
                bombsPlaced++;
            }
        }
    }

    private void GenerateHardModeBombs()
    {
        // Select random pattern or use existing if retry
        if (currentPattern == null && minePatterns.Count > 0)
        {
            currentPattern = minePatterns[Random.Range(0, minePatterns.Count)];
        }

        if (currentPattern != null)
        {
            foreach (Vector2Int pos in currentPattern.bombPositions)
            {
                if (pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight)
                {
                    bombs[pos.x, pos.y] = true;
                }
            }
        }
    }

    private void CalculateAdjacentBombs()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (!bombs[x, y])
                {
                    int count = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
                            {
                                if (bombs[nx, ny])
                                    count++;
                            }
                        }
                    }
                    adjacentBombCount[x, y] = count;
                }
            }
        }
    }

    // Called from MinesweeperTile
    public void OnTileClicked(int x, int y)
    {
        if (gameOver || !gameStarted) return;

        if (bombs[x, y])
        {
            // Hit a bomb!
            OnBombClicked(x, y);
        }
        else
        {
            // Safe tile
            RevealTile(x, y);
            CheckWinCondition();
        }
    }

    // Called from MinesweeperTile
    public void OnTileFlagged(int x, int y)
    {
        if (gameOver || !gameStarted) return;

        // Easy mode: Flag = instant lose!
        if (!isHardMode)
        {
            easyModeFlagCount++;
            StartCoroutine(OnEasyModeFlagPlaced());
        }
    }

    private void OnBombClicked(int x, int y)
    {
        if (isHardMode)
        {
            hardModeDeathCount++;
            StartCoroutine(OnHardModeBombHit(x, y));
        }
        else
        {
            // Easy mode - clicking bomb is bad but expected
            StartCoroutine(OnEasyModeBombHit(x, y));
        }
    }

    private IEnumerator OnEasyModeFlagPlaced()
    {
        gameOver = true;

        // Show bomb at flagged location
        List<NarratorManager.DialogueLine> dialogue = new List<NarratorManager.DialogueLine>();

        switch (easyModeFlagCount)
        {
            case 1:
                dialogue.Add(new NarratorManager.DialogueLine { text = "...oh" });
                dialogue.Add(new NarratorManager.DialogueLine { text = "That's unfortunate" });
                break;
            case 2:
                dialogue.Add(new NarratorManager.DialogueLine { text = "You're very confident with that flag" });
                dialogue.Add(new NarratorManager.DialogueLine { text = "I…admire that" });
                break;
            case 3:
                dialogue.Add(new NarratorManager.DialogueLine { text = "Maybe don't mark things you're afraid of" });
                dialogue.Add(new NarratorManager.DialogueLine { text = "...just a thought" });
                break;
            default:
                dialogue.Add(new NarratorManager.DialogueLine { text = "Wow" });
                dialogue.Add(new NarratorManager.DialogueLine { text = "You really trusted that flag" });
                break;
        }

        if (NarratorManager.Instance != null)
            NarratorManager.Instance.PlayDialogue(dialogue);

        yield return new WaitForSeconds(3f);

        // Restart game
        RestartGame();
    }

    private IEnumerator OnEasyModeBombHit(int x, int y)
    {
        gameOver = true;
        tiles[x, y].ShowBomb();

        List<NarratorManager.DialogueLine> dialogue = new List<NarratorManager.DialogueLine>();
        dialogue.Add(new NarratorManager.DialogueLine { text = "Focus dude…" });
        dialogue.Add(new NarratorManager.DialogueLine { text = "Let's try this again" });

        if (NarratorManager.Instance != null)
            NarratorManager.Instance.PlayDialogue(dialogue);

        yield return new WaitForSeconds(3f);

        RestartGame();
    }

    private IEnumerator OnHardModeBombHit(int x, int y)
    {
        gameOver = true;
        tiles[x, y].ShowBomb();

        List<NarratorManager.DialogueLine> dialogue = new List<NarratorManager.DialogueLine>();

        switch (hardModeDeathCount)
        {
            case 1:
                dialogue.Add(new NarratorManager.DialogueLine { text = "Expected" });
                break;
            case 2:
                dialogue.Add(new NarratorManager.DialogueLine { text = "Observe and remember" });
                break;
            case 3:
                dialogue.Add(new NarratorManager.DialogueLine { text = "Did you realize?" });
                break;
            default:
                dialogue.Add(new NarratorManager.DialogueLine { text = "Same board" });
                dialogue.Add(new NarratorManager.DialogueLine { text = "Not a bug" });
                break;
        }

        if (NarratorManager.Instance != null)
            NarratorManager.Instance.PlayDialogue(dialogue);

        yield return new WaitForSeconds(3f);

        RestartGame();
    }

    private void RevealTile(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return;
        if (tiles[x, y].IsRevealed) return;

        tiles[x, y].Reveal(adjacentBombCount[x, y]);

        // Auto-reveal adjacent tiles if no bombs nearby (flood fill)
        if (!isHardMode && adjacentBombCount[x, y] == 0)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    RevealTile(x + dx, y + dy);
                }
            }
        }
    }

    private void CheckWinCondition()
    {
        int revealedCount = 0;
        int totalSafeTiles = (gridWidth * gridHeight) - (isHardMode ? hardBombCount : easyBombCount);

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (tiles[x, y].IsRevealed && !bombs[x, y])
                    revealedCount++;
            }
        }

        if (revealedCount >= totalSafeTiles)
        {
            OnGameWon();
        }
    }

    private void OnGameWon()
    {
        gameOver = true;
        StartCoroutine(PlayWinDialogue());
    }

    private IEnumerator PlayIntroDialogue()
    {
        List<NarratorManager.DialogueLine> dialogue = new List<NarratorManager.DialogueLine>();

        if (isHardMode)
        {
            dialogue.Add(new NarratorManager.DialogueLine { text = "Okay" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "High difficulty" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "No numbers" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "Observe, remember, and good luck" });
        }
        else
        {
            dialogue.Add(new NarratorManager.DialogueLine { text = "Okay, this one should be easy" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "You know what, i won't interfere you here" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "Just.." });
            dialogue.Add(new NarratorManager.DialogueLine { text = "Focus" });
        }

        if (NarratorManager.Instance != null)
            NarratorManager.Instance.PlayDialogue(dialogue);

        yield return new WaitForSeconds(dialogue.Count * 2f);
    }

    private IEnumerator PlayWinDialogue()
    {
        List<NarratorManager.DialogueLine> dialogue = new List<NarratorManager.DialogueLine>();

        if (isHardMode)
        {
            dialogue.Add(new NarratorManager.DialogueLine { text = "...you figured it out" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "You figured the patterns" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "That is just…wow" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "No no i shouldn't be amazed" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "Moving on" });
        }
        else
        {
            dialogue.Add(new NarratorManager.DialogueLine { text = "There it is" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "Easy" });
            dialogue.Add(new NarratorManager.DialogueLine { text = "Moving on?" });
        }

        if (NarratorManager.Instance != null)
            NarratorManager.Instance.PlayDialogue(dialogue);

        yield return new WaitForSeconds(dialogue.Count * 2f);
    }

    private void RestartGame()
    {
        gameOver = false;
        gameStarted = false;

        // Don't reset pattern or death count in hard mode
        // Don't reset flag count in easy mode (player needs to learn!)

        StartCoroutine(InitializeGame());
    }

    public bool IsBomb(int x, int y)
    {
        if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return false;
        return bombs[x, y];
    }

    public bool IsHardMode()
    {
        return isHardMode;
    }

    private void OnSwitchToPanelA()
    {
        if (StageManagement.Instance != null)
        {
            //StageManagement.Instance.SwitchToGamePanel_A();
        }
    }

    private void OnDestroy()
    {
        if (switchToPanelA_Button != null)
        {
            switchToPanelA_Button.onClick.RemoveListener(OnSwitchToPanelA);
        }
    }
}

[System.Serializable]
public class MinePattern
{
    public string patternName;
    public List<Vector2Int> bombPositions = new List<Vector2Int>();
}