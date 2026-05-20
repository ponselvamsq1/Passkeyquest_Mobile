using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TypeEffectTextInit : MonoBehaviour
{
    public TMP_Text textMeshPro;
    public float typingSpeed = 0.2f;
    public GameObject continebutton;

    private string fullText= "Yes, you got it right! As you progress in the game, you'll build a strong foundation in password security, making learning fun and engaging. ";
    

  
    IEnumerator TypeText()
    {
        for (int i = 0; i < fullText.Length; i++)
        {
            textMeshPro.text += fullText[i]; 
            yield return new WaitForSeconds(typingSpeed); 

            
            if (i == fullText.Length - 1)
            {
                Debug.Log("Reached the last character: " + fullText[i]);  
                continebutton.SetActive(true);
                
                PlayerPrefs.SetInt("ins", 1);
                
            }
        }
    }
    public void Reset_typewritring()
    {
        // Stop any ongoing typing coroutine
        textMeshPro.text = ""; // Clear text
        continebutton.SetActive(false); // Hide the button
        // StartCoroutine(TypeText()); // Restart typing effect
    }
    public void RestartTypewriting()
    {
        StartCoroutine(TypeText()); 
    }
}
