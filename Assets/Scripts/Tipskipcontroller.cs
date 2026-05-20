using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tipskipcontroller : MonoBehaviour
{
    
    public Button nextButton;
    public Button prevButton;
    public Button takequizButton;
    
    public GameObject[] textObjects; // Assign in Inspector
    private int currentIndex = 2;
    public GameObject quizpanel;

    void Start()
    {
        
        UpdateText();
        nextButton.onClick.AddListener(NextText);
        prevButton.onClick.AddListener(PreviousText);
    }

    void NextText()
    {
        if (currentIndex< textObjects.Length - 1)
        {
            currentIndex++;
            UpdateText();
        }
    }

    void PreviousText()
    {
        if (currentIndex> 0)
        {
            currentIndex--;
            UpdateText();
        }
    }

    private void Update()
    {
        //currentIndex = GameManager.Instance.Keycount;
    }

    void UpdateText()
    {
        for (int i = 0; i < textObjects.Length; i++)
        {
            textObjects[i].SetActive(i == currentIndex);
        }
        prevButton.interactable = currentIndex > 0;
        nextButton.interactable = currentIndex < textObjects.Length - 1;
        
        if (currentIndex >= textObjects.Length - 1)
            takequizButton.gameObject.SetActive(true);
    }

    public void Enablequizpanel()
    {
        quizpanel.SetActive(true);
    }
}

