using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public string videofilename;
    private VideoPlayer vp;
    public GameObject loadingpanel;

   
    protected  void Awake()
    {
       
        DontDestroyOnLoad(gameObject);
        
        Invoke("DisableLoading",0.5f);
        vp = GetComponent<VideoPlayer>();
        playvideo();
    }

    void DisableLoading()
    {
        loadingpanel.SetActive(false);
    }

    void playvideo()
    {
       
        if (vp)
        {
            string videopath = System.IO.Path.Combine(Application.streamingAssetsPath, videofilename);
            Debug.Log(videopath);
            vp.url = videopath;
            vp.Play();
            
        }
        else
        {
            Debug.Log("VPlayer Not Found");
        }
    }
 
}
