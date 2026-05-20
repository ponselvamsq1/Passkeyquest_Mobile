using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.Rendering.Universal;

public class Quiz : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string[] options;
        public int correctAnswerIndex;
        public string feedback;
    }

    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
  //  public TextMeshProUGUI resultText;

    public List<Question> questions;
    private List<Question> shuffledQuestions;
    private int currentQuestionIndex,answerIndex,attempt=0;
    public Button  submitButton;
    public TextMeshProUGUI feedbackText,feedbackFinalText;
    public GameObject correctpane, wrongpanel,finalpanel,quizfinsihpanel;

    void Start()
    {
        DisplayNextQuestion();
    }

    void DisplayNextQuestion()
    {
        shuffledQuestions = questions.OrderBy(q => Random.value).ToList();
        currentQuestionIndex = 0;
        ShowQuestion();
    }

    void ShowQuestion()
    {
        if (currentQuestionIndex >= shuffledQuestions.Count)
        {
            //resultText.text = "Quiz Finished!";
            quizfinsihpanel.SetActive(true);
            return;
        }

        Question currentQuestion = shuffledQuestions[currentQuestionIndex];
        questionText.text = currentQuestionIndex+1 + ".  " +currentQuestion.questionText;
        feedbackText.text = currentQuestion.feedback;
        feedbackFinalText.text=currentQuestion.feedback;
        
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < currentQuestion.options.Length)
            {
                optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = currentQuestion.options[i];
                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                //optionButtons[i].onClick.AddListener(() => AnswerSelected(index));
                optionButtons[i].onClick.AddListener(()=> OnOptionSelected( index));
                optionButtons[i].gameObject.SetActive(true);
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }
    
    // correct answer checking

    // void AnswerSelected(int index)
    // {
    //     Question currentQuestion = shuffledQuestions[currentQuestionIndex];
    //     if (index == currentQuestion.correctAnswerIndex)
    //     {
    //         resultText.text = "Correct!";
    //     }
    //     else
    //     {
    //         resultText.text = "Wrong! Correct answer: " + currentQuestion.options[currentQuestion.correctAnswerIndex];
    //     }
    //     
    //     
    // }
    void OnOptionSelected(int index)
     {
         answerIndex = index;
         ResetoptionClr();
         optionButtons[index].GetComponent<Image>().color = Color.gray;
         submitButton.interactable = true;

     }
    void ResetoptionClr()
     {
        
         for (int i = 0; i < optionButtons.Length; i++)
         {
             optionButtons[i].GetComponent<Image>().color = new Color32(13,116,89,255);
         }
     }


    public void Submit()
    {
        attempt++;
        if (attempt == 1)
        {
            Question currentQuestion = shuffledQuestions[currentQuestionIndex];
            if (answerIndex == currentQuestion.correctAnswerIndex)
            {
                // resultText.text = "Correct!";
                correctpane.SetActive(true);
           
            }
            else
            {
                wrongpanel.SetActive(true);
                submitButton.interactable = false;
                // resultText.text = "Wrong! Correct answer: " + currentQuestion.options[currentQuestion.correctAnswerIndex];
            }
        }
        else if (attempt == 2)
        {
            Question currentQuestion = shuffledQuestions[currentQuestionIndex];
            if (answerIndex == currentQuestion.correctAnswerIndex)
            {
                // resultText.text = "Correct!";
                correctpane.SetActive(true);
           
            }
            else
            {
                
                ShowcorrectAnswer();
                submitButton.interactable = false;
               
            }
        }
      
        
    }

    void ShowcorrectAnswer()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i == questions[currentQuestionIndex].correctAnswerIndex)
            {
                Debug.Log("final panel enabled1");
                Debug.Log("correct answer is " + answerIndex);
                finalpanel.SetActive(true);
                Invoke("Enablefinalpanel",1);
              optionButtons[i].GetComponent<Image>().color = Color.green; // Correct answer is green
              
              
            }
          optionButtons[i].interactable = false; 
          
        }
        
    }

   void Enablefinalpanel()
    {
        Debug.Log("final panel enabled2");
        
        finalpanel.SetActive(true);
    }
    
    
    
    public void NextQuestion()
     {
         for (int i = 0; i < optionButtons.Length; i++)
         {
             optionButtons[i].GetComponent<Image>().color = new Color32(79,99,202,255);
         }
         correctpane.SetActive(false);
         feedbackText.text = "";
         attempt = 0;
         ResetoptionClr();
        
         for (int i = 0; i < optionButtons.Length; i++)
         {
             optionButtons[i].interactable = true;
         }

         
         currentQuestionIndex++;
         ShowQuestion();
         
     }
    
    public void Retry()
    {
        wrongpanel.SetActive(false);
        ResetoptionClr();
    }
}
