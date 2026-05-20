using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checktipreach : MonoBehaviour
{
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(GameConstants.PLAYER_TAG))
        {
            Debug.Log("Enter player after fan");
            GameManager.Instance.ischeck = true;
            GameManager.Instance.checktipcount = 0;
        }

    }
}
