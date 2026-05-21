using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuffsManager : BaseSingleton<BuffsManager>
{
    [SerializeField] PlayerSpeedBuff _speedBuff;
    [SerializeField] PlayerJumpBuff _jumpBuff;
    [SerializeField] PlayerInvisibleBuff _invisibleBuff;
    [SerializeField] PlayerShieldBuff _shieldBuff;
    [SerializeField] PlayerAbsorbBuff _absorbBuff;

    public GameObject uiTipsPanel,key1,key2,key3,key4,key5,winpanel,winpanel2,opendoor,tipclose1,tipclose2, wincongrats,tipskippanel,tiphintpanel;
    public Button bulpimg;
    public GameObject pausepage;
   

    private Dictionary<GameEnums.EBuffs, PlayerBuffs> _dictBuffs = new();

    protected override void Awake()
    {
        base.Awake();
    }

   
    

    public void EnableTipSkipPanel()
    {
        Time.timeScale = 0;
        tipskippanel.SetActive(true);
        UIManager.Instance.PopDownLoosePanel();
    }
    public void Lasttip()
    {
        Time.timeScale = 0;
        tipclose1.SetActive(false);
        tipclose2.SetActive(true);
        
    }

    public void Enablewincongrats()
    {
        wincongrats.SetActive(true);
        
    }

    public void disablewincongrats()
    {
        wincongrats.SetActive(false);
        Time.timeScale = 1;
    }
    
   

   
    private void Start()
    {
        
        if (PlayerPrefs.GetInt("keyID1", 0) == 1) 
        {
            key1.SetActive(false);
        }
        if (PlayerPrefs.GetInt("keyID2", 0) == 1) 
        {
            key2.SetActive(false);
        }
        if (PlayerPrefs.GetInt("keyID3", 0) == 1) 
        {
            key3.SetActive(false);
        }
        if (PlayerPrefs.GetInt("keyID4", 0) == 1) 
        {
            key4.SetActive(false);
        }
        if (PlayerPrefs.GetInt("keyID5", 0) == 1) 
        {
            key5.SetActive(false);
        }
        
        if (PlayerPrefs.GetInt("door", 0) == 1) 
        {
            opendoor.SetActive(false);
           
        }

       
        if(PlayerPrefs.GetInt("hint", 0)==1)
        {
            bulpimg.gameObject.SetActive(true);
        }
       
        
        InitBuffDictionary();
        foreach (var buff in _dictBuffs.Values)
            buff.Start();
    }

    public void GameWin()
    {
        
       
        winpanel.SetActive(true);
        
    }

    public void GameLoss()
    {
        Time.timeScale = 0;
        winpanel2.SetActive(true);
       
    }

    public void NextlevelTwo()
    {
        GameManager.Instance.SwitchToScene(2);
    }

    public void BackHome_BuffManager()
    {
        GameManager.Instance.BackHome();
        SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 1.0f);
        
    }

    public void Buff_Reloadscene()
    {
        GameManager.Instance.ReloadScene();
        SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 1.0f);
    }

    public void Continuegamemusic()
    {
        SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 1.0f);
    }

    private void Update()
    {
        //GameManager.Instance.checktipcount >=2 && GameManager.Instance.ischeck &&
        
        if ( GameManager.Instance.checktipcount >=2 && GameManager.Instance.ischeck && GameManager.Instance.Keycount >=2  && PlayerPrefs.GetInt("hint", 0)==0)
        { 
            Debug.Log("enable hint");
            bulpimg.gameObject.SetActive(true);
            PlayerPrefs.SetInt("hint",1);
            tiphintpanel.SetActive(true);
            Time.timeScale = 0;
            GameManager.Instance.ischeck = false;
        }
        if (GameManager.Instance.Keycount == 5)
        {
            opendoor.SetActive(true);
            PlayerPrefs.SetInt("door", 1);
        }
        
        foreach(var buff  in _dictBuffs.Values)
            buff.Update();
    }

    private void InitBuffDictionary()
    {
        _dictBuffs.Add(GameEnums.EBuffs.Speed, _speedBuff);
        _dictBuffs.Add(GameEnums.EBuffs.Jump, _jumpBuff);
        _dictBuffs.Add(GameEnums.EBuffs.Invisible, _invisibleBuff);
        //Xem lại tại sao dùng _shieldBuff kh đc 
        //Do kéo từ trong prefab ra nên nó ref cái thằng shield trong prefab :v
        _dictBuffs.Add(GameEnums.EBuffs.Shield, _shieldBuff);
        _dictBuffs.Add(GameEnums.EBuffs.Absorb, _absorbBuff);
    }

    public PlayerBuffs GetTypeOfBuff(GameEnums.EBuffs buffType)
    {
        return _dictBuffs[buffType];
    }
    
    
    public void ResumeGame()
    {
        Time.timeScale = 1;
        uiTipsPanel.SetActive(false);
    }

    public void ResumeGameAll()
    {
        Time.timeScale = 1;
    }

    public void ContinueGameAftertypeeffect()
    {
        GameManager.Instance.iscontinue = true;
    }

    public void OnClick_Pause()
    {
        Time.timeScale = 0;
        pausepage.SetActive(true);
    }

    public void OnClick_Resume()
    {
        Time.timeScale = 1;
        pausepage.SetActive(false);
    }

    public void OnClick_Exit()
    {
        //Application.OpenURL("https://beta.skillhub.sq1.security/course/view.php?id=3");
       // Application.OpenURL("http://192.168.8.59/course/view.php?id=3");
        Application.Quit();
    }
}
