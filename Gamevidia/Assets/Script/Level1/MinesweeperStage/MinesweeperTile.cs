using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MinesweeperTile : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image tileImage;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private GameObject bombIcon;
    [SerializeField] private GameObject flagIcon;

    [Header("Colors")]
    [SerializeField] private Color hiddenColor = Color.gray;
    [SerializeField] private Color revealedColor = Color.white;
    [SerializeField] private Color bombColor = Color.red;

    private int x, y;
    private OrdinaryMinesweeper controller;
    private bool isRevealed = false;
    private bool isFlagged = false;

    public bool IsRevealed => isRevealed;

    public void Initialize(int x, int y, OrdinaryMinesweeper controller)
    {
        this.x = x;
        this.y = y;
        this.controller = controller;

        // Set initial state
        if (tileImage != null)
            tileImage.color = hiddenColor;

        if (numberText != null)
            numberText.text = "";

        if (bombIcon != null)
            bombIcon.SetActive(false);

        if (flagIcon != null)
            flagIcon.SetActive(false);

        isRevealed = false;
        isFlagged = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isRevealed) return;

        // Right click = flag (only in easy mode for the twist!)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ToggleFlag();
        }
        // Left click = reveal
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!isFlagged)
            {
                controller.OnTileClicked(x, y);
            }
        }
    }

    private void ToggleFlag()
    {
        if (!isFlagged)
        {
            isFlagged = true;
            if (flagIcon != null)
                flagIcon.SetActive(true);

            controller.OnTileFlagged(x, y);
        }
    }

    public void Reveal(int adjacentBombs)
    {
        isRevealed = true;

        if (tileImage != null)
            tileImage.color = revealedColor;

        // Show number only in easy mode
        if (!controller.IsHardMode() && adjacentBombs > 0)
        {
            if (numberText != null)
            {
                numberText.text = adjacentBombs.ToString();
                numberText.color = GetNumberColor(adjacentBombs);
            }
        }
    }

    public void ShowBomb()
    {
        isRevealed = true;

        if (tileImage != null)
            tileImage.color = bombColor;

        if (bombIcon != null)
            bombIcon.SetActive(true);
    }

    private Color GetNumberColor(int number)
    {
        switch (number)
        {
            case 1: return Color.blue;
            case 2: return Color.green;
            case 3: return Color.red;
            case 4: return new Color(0, 0, 0.5f); // Dark blue
            case 5: return new Color(0.5f, 0, 0); // Dark red
            case 6: return Color.cyan;
            case 7: return Color.black;
            case 8: return Color.gray;
            default: return Color.black;
        }
    }
}