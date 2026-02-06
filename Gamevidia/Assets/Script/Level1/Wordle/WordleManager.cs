using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WordleManager : MonoBehaviour
{
    [Header("UI Reference")]
    public Transform board;
    public TextMeshProUGUI infoText;

    [Header("Word Settings")]
    public string easyWord = "APPLE";
    public string hardWord = "GRAPE";

    const int MAX_ROWS = 6;
    const int COLS = 5;

    TextMeshProUGUI[,] letters = new TextMeshProUGUI[MAX_ROWS, COLS];
    Image[,] cells = new Image[MAX_ROWS, COLS];

    int currentRow = 0;
    int currentCol = 0;

    int allowedRows = 6;   // berubah sesuai difficulty
    string secretWord;

    void Start()
    {
        ApplyDifficulty();

        secretWord = secretWord.ToUpper();
        SetupBoard();

        infoText.text = "Type 5 letters then press ENTER";
    }

    void Update()
    {
        if (currentRow >= allowedRows) return;

        foreach (char c in Input.inputString)
        {
            if (char.IsLetter(c))
                AddLetter(char.ToUpper(c));
            else if (c == '\b')
                RemoveLetter();
            else if (c == '\n' || c == '\r')
                SubmitWord();
        }
    }

    void ApplyDifficulty()
    {
        if (StageManagement.Instance == null)
        {
            Debug.LogWarning("StageManagement not found. Using default Easy.");
            SetEasy();
            return;
        }

        if (StageManagement.Instance.IsHardMode())
            SetHard();
        else
            SetEasy();
    }

    void SetEasy()
    {
        secretWord = easyWord;
        allowedRows = 6;
    }

    void SetHard()
    {
        secretWord = hardWord;
        allowedRows = 4; // Hard mode lebih sedikit kesempatan
    }

    void SetupBoard()
    {
        for (int r = 0; r < MAX_ROWS; r++)
        {
            Transform row = board.GetChild(r);

            for (int c = 0; c < COLS; c++)
            {
                Transform cell = row.GetChild(c);

                cells[r, c] = cell.GetComponent<Image>();
                letters[r, c] = cell.GetComponentInChildren<TextMeshProUGUI>();

                letters[r, c].text = "";
                cells[r, c].color = new Color(0.2f, 0.2f, 0.2f);

                // Disable row yang tidak boleh dipakai (Hard mode)
                if (r >= allowedRows)
                    row.gameObject.SetActive(false);
            }
        }
    }

    void AddLetter(char letter)
    {
        if (currentCol >= COLS) return;

        letters[currentRow, currentCol].text = letter.ToString();
        currentCol++;
    }

    void RemoveLetter()
    {
        if (currentCol <= 0) return;

        currentCol--;
        letters[currentRow, currentCol].text = "";
    }

    void SubmitWord()
    {
        if (currentCol < COLS)
        {
            infoText.text = "Must be 5 letters!";
            return;
        }

        string guess = "";
        for (int c = 0; c < COLS; c++)
            guess += letters[currentRow, c].text;

        CheckWord(guess);

        if (guess == secretWord)
        {
            infoText.text = "🎉 Correct!";
            enabled = false;
            return;
        }

        currentRow++;
        currentCol = 0;

        if (currentRow >= allowedRows)
            infoText.text = "Game Over 😢 Word: " + secretWord;
    }

    void CheckWord(string guess)
    {
        for (int c = 0; c < COLS; c++)
        {
            if (guess[c] == secretWord[c])
                cells[currentRow, c].color = Color.green;
            else if (secretWord.Contains(guess[c].ToString()))
                cells[currentRow, c].color = Color.yellow;
            else
                cells[currentRow, c].color = Color.gray;
        }
    }
}