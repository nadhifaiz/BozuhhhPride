using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrdinaryMinesweeper : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private Button switchToPanelA_Button;

    [Header("External References")]
    [SerializeField] private StageManagement stageManagement;

    [Header("Grid Layout")]
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private Vector2 cellSpacing = new Vector2(4f, 4f);
    [SerializeField] private bool autoCellSizeFromPrefab = true;

    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private int easyBombCount = 10;
    [SerializeField] private int hardBombCount = 10;

    [Header("Hard Mode Patterns")]
    [SerializeField] private List<MinePattern> minePatterns = new List<MinePattern>();

    [Header("Dialogue")]
    [SerializeField] private DialogueConfig dialogueConfig = new DialogueConfig();
    [SerializeField] private float fallbackDialogueDelayPerLine = 2f;

    // Game State
    private MinesweeperTile[,] tiles;
    private bool[,] bombs;
    private int[,] adjacentBombCount;
    private StageManagement.Difficulty currentDifficulty;
    private bool isHardMode;
    private bool gameStarted;
    private bool gameOver;
    private int totalBombs;

    // Hard Mode State
    private MinePattern currentPattern;
    private int hardModeDeathCount = 0;

    // Easy Mode State
    private int easyModeFlagCount = 0;

    private void Start()
    {
        // Get difficulty from StageManagement
        if (stageManagement != null)
        {
            currentDifficulty = stageManagement.CurrentDifficulty;
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
        if (tiles == null)
            yield break;

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
        if (gridWidth <= 0 || gridHeight <= 0)
        {
            Debug.LogWarning($"Invalid grid size {gridWidth}x{gridHeight}. Grid will not be created.");
            return;
        }

        if (gridParent == null || tilePrefab == null)
        {
            Debug.LogWarning("Grid parent or tile prefab is not assigned. Grid will not be created.");
            return;
        }

        ConfigureGridLayout();

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

    private void ConfigureGridLayout()
    {
        if (gridParent == null)
            return;

        RectTransform gridRect = gridParent as RectTransform;
        if (gridRect == null)
            return;

        GridLayoutGroup gridLayout = gridParent.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
            gridLayout = gridParent.gameObject.AddComponent<GridLayoutGroup>();

        LayoutGroup[] otherLayouts = gridParent.GetComponents<LayoutGroup>();
        foreach (var layout in otherLayouts)
        {
            if (layout != gridLayout)
                layout.enabled = false;
        }

        ContentSizeFitter contentSizeFitter = gridParent.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
            contentSizeFitter.enabled = false;

        if (autoCellSizeFromPrefab && tilePrefab != null)
        {
            RectTransform prefabRect = tilePrefab.GetComponent<RectTransform>();
            if (prefabRect != null)
                cellSize = prefabRect.sizeDelta;
        }

        gridLayout.cellSize = cellSize;
        gridLayout.spacing = cellSpacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gridWidth;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        float totalWidth = (gridWidth * cellSize.x) + Mathf.Max(0, gridWidth - 1) * cellSpacing.x;
        float totalHeight = (gridHeight * cellSize.y) + Mathf.Max(0, gridHeight - 1) * cellSpacing.y;

        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(totalWidth, totalHeight);
        gridRect.anchoredPosition = Vector2.zero;
    }

    private void GenerateEasyModeBombs()
    {
        int totalCells = gridWidth * gridHeight;
        int targetBombs = Mathf.Min(easyBombCount, totalCells);
        if (easyBombCount > totalCells)
        {
            Debug.LogWarning($"Easy bomb count ({easyBombCount}) exceeds grid cells ({totalCells}). Clamping.");
        }

        int bombsPlaced = 0;
        while (bombsPlaced < targetBombs)
        {
            int x = Random.Range(0, gridWidth);
            int y = Random.Range(0, gridHeight);

            if (!bombs[x, y])
            {
                bombs[x, y] = true;
                bombsPlaced++;
            }
        }

        totalBombs = bombsPlaced;
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
        else
        {
            Debug.LogWarning("Hard mode is active but no mine pattern is assigned.");
        }

        totalBombs = CountBombs();
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
        if (bombs == null || tiles == null) return;

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
        if (bombs == null || tiles == null) return;

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

        yield return StartCoroutine(PlayDialogueAndWait(GetEasyFlagDialogue()));

        // Restart game
        RestartGame();
    }

    private IEnumerator OnEasyModeBombHit(int x, int y)
    {
        gameOver = true;
        tiles[x, y].ShowBomb();

        yield return StartCoroutine(PlayDialogueAndWait(dialogueConfig.easyBombHit));

        RestartGame();
    }

    private IEnumerator OnHardModeBombHit(int x, int y)
    {
        gameOver = true;
        tiles[x, y].ShowBomb();

        yield return StartCoroutine(PlayDialogueAndWait(GetHardBombDialogue()));

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
        int totalSafeTiles = (gridWidth * gridHeight) - totalBombs;

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

    private int CountBombs()
    {
        int count = 0;
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (bombs[x, y])
                    count++;
            }
        }
        return count;
    }

    private void OnGameWon()
    {
        gameOver = true;
        StartCoroutine(PlayWinDialogue());
    }

    private IEnumerator PlayIntroDialogue()
    {
        List<NarratorManager.DialogueLine> dialogue = isHardMode
            ? dialogueConfig.introHard
            : dialogueConfig.introEasy;

        yield return StartCoroutine(PlayDialogueAndWait(dialogue));
    }

    private IEnumerator PlayWinDialogue()
    {
        List<NarratorManager.DialogueLine> dialogue = isHardMode
            ? dialogueConfig.winHard
            : dialogueConfig.winEasy;

        yield return StartCoroutine(PlayDialogueAndWait(dialogue));
    }

    private void RestartGame()
    {
        gameOver = false;
        gameStarted = false;

        // Don't reset pattern or death count in hard mode
        // Don't reset flag count in easy mode (player needs to learn!)

        StopAllCoroutines();
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

    public void SetStageManagement(StageManagement management)
    {
        stageManagement = management;
    }

    public void SetDifficulty(StageManagement.Difficulty difficulty)
    {
        currentDifficulty = difficulty;
        isHardMode = (currentDifficulty == StageManagement.Difficulty.Hard);
    }

    public void SetDialogueConfig(DialogueConfig config)
    {
        dialogueConfig = config;
    }

    private List<NarratorManager.DialogueLine> GetEasyFlagDialogue()
    {
        switch (easyModeFlagCount)
        {
            case 1:
                return dialogueConfig.easyFlag1;
            case 2:
                return dialogueConfig.easyFlag2;
            case 3:
                return dialogueConfig.easyFlag3;
            default:
                return dialogueConfig.easyFlagDefault;
        }
    }

    private List<NarratorManager.DialogueLine> GetHardBombDialogue()
    {
        switch (hardModeDeathCount)
        {
            case 1:
                return dialogueConfig.hardBombHit1;
            case 2:
                return dialogueConfig.hardBombHit2;
            case 3:
                return dialogueConfig.hardBombHit3;
            default:
                return dialogueConfig.hardBombHitDefault;
        }
    }

    private IEnumerator PlayDialogueAndWait(List<NarratorManager.DialogueLine> dialogue)
    {
        if (dialogue == null || dialogue.Count == 0)
            yield break;

        if (NarratorManager.Instance == null)
        {
            yield return new WaitForSeconds(dialogue.Count * fallbackDialogueDelayPerLine);
            yield break;
        }

        bool finished = false;
        void OnFinished()
        {
            finished = true;
        }

        NarratorManager.Instance.OnDialogueFinished += OnFinished;
        NarratorManager.Instance.PlayDialogue(dialogue);

        while (!finished)
            yield return null;

        NarratorManager.Instance.OnDialogueFinished -= OnFinished;
    }

    private void OnSwitchToPanelA()
    {
        if (stageManagement != null)
        {
            stageManagement.ShowDifficultyPanel();
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

[System.Serializable]
public class DialogueConfig
{
    public List<NarratorManager.DialogueLine> introEasy = new List<NarratorManager.DialogueLine>();
    public List<NarratorManager.DialogueLine> introHard = new List<NarratorManager.DialogueLine>();

    public List<NarratorManager.DialogueLine> winEasy = new List<NarratorManager.DialogueLine>();
    public List<NarratorManager.DialogueLine> winHard = new List<NarratorManager.DialogueLine>();

    public List<NarratorManager.DialogueLine> easyBombHit = new List<NarratorManager.DialogueLine>();
    public List<NarratorManager.DialogueLine> hardBombHit1 = new List<NarratorManager.DialogueLine>();
    public List<NarratorManager.DialogueLine> hardBombHit2 = new List<NarratorManager.DialogueLine>();
    public List<NarratorManager.DialogueLine> hardBombHit3 = new List<NarratorManager.DialogueLine>();
    public List<NarratorManager.DialogueLine> hardBombHitDefault = new List<NarratorManager.DialogueLine>();

    public List<NarratorManager.DialogueLine> easyFlag1 = new List<NarratorManager.DialogueLine>();
    public List<NarratorManager.DialogueLine> easyFlag2 = new List<NarratorManager.DialogueLine>();
    public List<NarratorManager.DialogueLine> easyFlag3 = new List<NarratorManager.DialogueLine>();
    public List<NarratorManager.DialogueLine> easyFlagDefault = new List<NarratorManager.DialogueLine>();
}






