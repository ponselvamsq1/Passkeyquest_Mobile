using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Texttypewritring : MonoBehaviour
{
    public TMP_Text textMeshPro;
    public float typingSpeed = 0.2f;
    public GameObject continebutton,instructionspanel;

    private string fullText ="Hi, welcome to Passkey Quest! I'm Red Hat Boy, trying to escape from the obstacles. Help me collect five colored keys to unlock the door! ";

  
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
                
                
            }
        }
    }
    public void Reset_typewritring()
    {
       
        textMeshPro.text = "";
        continebutton.SetActive(false); 
      
    }

    public void RestartTypewriting()
    {
        StartCoroutine(TypeText()); 
    }

    public void Disableinstructionspanel()
    {
        instructionspanel.SetActive(false);
        
    }

}