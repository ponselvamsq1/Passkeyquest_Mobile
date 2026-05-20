// using System;
// using System.Collections;
// using System.Collections.Generic;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class Quizmanager : MonoBehaviour
// {
//     [System.Serializable]
//     public class Question
//     {
//         public string questionText;
//         public string[] options;
//         public int correctAnswerIndex;
//         public string feedback;
//     }
//
//     public Text questionText;
//     public Button[] optionButtons;
//     public TextMeshProUGUI feedbackText, questionNoText, levelcompletiontxt,feedbacktxt2,feedbacktxt3;
//     public List<Question> questions;
//     public GameObject loadingScreen;
//
//     private int quizscore = 0, correctIndex;
//     public int currentQuestionIndex = 0, temptotalquiestion ;
//     public Button submitButton, nxtButton;
//     public GameObject continuepanel,gameendpanel;
//
//     private int attempt = 0;
//     public GameObject retrypanel,correctpanel,correctpanel2,loadingscreen,bg1,skipbutton;
//
//
//     
//
//     void Start()
//     {
//         DisplayNextQuestion();
//     }
//
//     public void DisplayNextQuestion()
//     {
//         loadingScreen.SetActive(false);
//         //feedbackText.text = "Quiz Score " + quizscore;
//         if (currentQuestionIndex < temptotalquiestion)
//         {
//             Question currentQuestion = questions[currentQuestionIndex];
//             int x = currentQuestionIndex + 1;
//             questionNoText.text = "QUESTION " + x;
//             questionText.text = currentQuestion.questionText;
//             feedbackText.text = currentQuestion.feedback;
//             feedbacktxt2.text = currentQuestion.feedback;
//             feedbacktxt3.text = currentQuestion.feedback;
//
//             for (int i = 0; i < optionButtons.Length; i++)
//             {
//                 if (i < currentQuestion.options.Length)
//                 {
//                     optionButtons[i].interactable = true;
//                     optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = currentQuestion.options[i];
//                     int index = i;
//                     optionButtons[i].onClick.RemoveAllListeners();
//                     
//                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index) ); 
//                     optionButtons[i].gameObject.SetActive(true);
//                 }
//                 else
//                 {
//                     optionButtons[i].gameObject.SetActive(false);
//                 }
//             }
//         }
//         else
//         {
//             if (Gamemanager.instance.currentlevel <= 2)
//             {
//                 continuepanel.SetActive(true);
//                 if (Gamemanager.instance.currentlevel == 1)
//                 {
//                     Gamemanager.instance.audioVoice.clip = Gamemanager.instance.level1_audio;
//                     Gamemanager.instance.textanim1.StartHeadingLone();
//                     Gamemanager.instance.audioVoice.Play();
//                 }
//                 else if (Gamemanager.instance.currentlevel == 2)
//                 {
//                     bg1.SetActive(true);
//                     Gamemanager.instance.audioVoice.clip = Gamemanager.instance.level2_audio;
//                     Gamemanager.instance.textanim2.StartHeadingLtwo();
//                     
//                     Gamemanager.instance.audioVoice.Play();
//                 }
//             }
//             else
//             {
//                 gameendpanel.SetActive(true);
//                 Gamemanager.instance.textanimE.StartHeadingLEnd();
//                 Gamemanager.instance.audioVoice.clip = Gamemanager.instance.endstory_audio;
//                 Gamemanager.instance.audioVoice.Play();
//             }
//             questionText.text = "Click Next To Continue Game";
//             levelcompletiontxt.text="Level "+ Gamemanager.instance.currentlevel + " Completed";
//             currentQuestionIndex++;
//         }
//     }
//     
//     void OnOptionSelected(int index)
//     {
//         correctIndex = index;
//         //Debug.Log("Correct answer Index"+ correctIndex);
//         // Reset all button colors first
//         ResetoptionClr();
//     
//         // Change the selected button to gray
//         optionButtons[index].GetComponent<Image>().color = Color.gray;
//     
//     }
//
//     public void Submit()
//     {
//        
//         bool isCorrect = (correctIndex == questions[currentQuestionIndex].correctAnswerIndex);
//         if (correctIndex == questions[currentQuestionIndex].correctAnswerIndex)
//         {
//             correctpanel.SetActive(true);
//             for (int i = 0; i < optionButtons.Length; i++)
//             {
//                 if (i == questions[currentQuestionIndex].correctAnswerIndex)
//                 {
//                     nxtButton.gameObject.SetActive(true);
//                     submitButton.gameObject.SetActive(false);
//                     optionButtons[i].GetComponent<Image>().color = Color.green; // Correct answer is green
//                 }
//                 optionButtons[i].interactable = false;
//             }
//             
//             currentQuestionIndex++;
//             attempt = 0;
//         }
//         else
//         {
//             Gamemanager.instance.Overallattempt++;
//             attempt++;
//             if (attempt > 1)
//             {
//                 ResetoptionClr();
//                 nxtButton.interactable = false;
//                 Debug.Log("all but clr same");
//                 //loadingscreen.SetActive(true);
//                     for (int i = 0; i < optionButtons.Length; i++)
//                     {
//                         if (i == questions[currentQuestionIndex].correctAnswerIndex)
//                         {
//                             nxtButton.gameObject.SetActive(true);
//                             submitButton.gameObject.SetActive(false);
//                             optionButtons[i].GetComponent<Image>().color = Color.green; // Correct answer is green
//                             
//                             Invoke("Enablecorrectpanel",3);
//                         }
//                         optionButtons[i].interactable = false;
//                     }
//                     currentQuestionIndex++;
//                     attempt = 0;
//             }
//             else
//             {
//                 for (int i = 0; i < optionButtons.Length; i++)
//                 {
//                    
//                     if (i == correctIndex)
//                     {
//                         retrypanel.SetActive(true);
//                         optionButtons[i].GetComponent<Image>().color = Color.red;
//
//                     }
//                 }
//             }
//            
//         }
//     }
//
//     void Enablecorrectpanel()
//     {
//         //loadingscreen.SetActive(false);
//         correctpanel2.SetActive(true);
//         nxtButton.interactable = true;
//     }
//
//     public void ResetoptionClr()
//     {
//        
//         for (int i = 0; i < optionButtons.Length; i++)
//         {
//             optionButtons[i].GetComponent<Image>().color = new Color32(79,99,202,255);
//         }
//     }
//
//
//     public void NextQuestion()
//     {
//         for (int i = 0; i < optionButtons.Length; i++)
//         {
//             optionButtons[i].GetComponent<Image>().color = new Color32(79,99,202,255);
//         }
//         nxtButton.gameObject.SetActive(false);
//         submitButton.gameObject.SetActive(true);  
//         feedbackText.text = "";
//         
//         DisplayNextQuestion();
//         // Invoke("DisplayNextQuestion", 0.1f);
//     }
// }
