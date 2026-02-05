using System;
using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public event Action OnDollEntered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Doll"))
        {
            Debug.Log("Doll entered basket!");
            OnDollEntered?.Invoke();
        }
    }
}