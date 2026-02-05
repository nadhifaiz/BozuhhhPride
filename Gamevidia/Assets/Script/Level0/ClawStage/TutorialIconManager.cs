using UnityEngine;
using TMPro;

public class TutorialIconManager : MonoBehaviour
{
    [Header("Icons")]
    [SerializeField] private GameObject movementIcon;  // Text: "← →"
    [SerializeField] private GameObject grabIcon;      // Text: "SPACE"

    void Awake()
    {
        HideAllIcons();
    }

    public void ShowMovementIcon()
    {
        if (movementIcon != null)
            movementIcon.SetActive(true);
    }

    public void HideMovementIcon()
    {
        if (movementIcon != null)
            movementIcon.SetActive(false);
    }

    public void ShowGrabIcon()
    {
        if (grabIcon != null)
            grabIcon.SetActive(true);
    }

    public void HideGrabIcon()
    {
        if (grabIcon != null)
            grabIcon.SetActive(false);
    }

    public void HideAllIcons()
    {
        HideMovementIcon();
        HideGrabIcon();
    }
}