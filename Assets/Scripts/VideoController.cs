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

    public VideoClip clip;
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
        
        vp.source = VideoSource.VideoClip;
        vp.clip = clip;

        vp.Prepare();
    }
 
}
