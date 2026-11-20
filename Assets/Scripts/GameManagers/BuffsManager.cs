using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public GameObject lastsummary;
   

    private Dictionary<GameEnums.EBuffs, PlayerBuffs> _dictBuffs = new();

    protected override void Awake()
    {
        base.Awake();
    }

    public void EnableTipSkipPanel()
    {
        if (HintBlocked()) return; // the player is out: only Retry / Home are allowed
        Time.timeScale = 0;
        tipskippanel.SetActive(true);
        UIManager.Instance.PopDownLoosePanel();
    }

    PlayerStateManager _player;
    CanvasGroup _hintGroup;
    Tipskipcontroller _tipCtrl;
    bool _tipsWasOpen;

    bool PlayerDead()
    {
        if (!_player) _player = FindFirstObjectByType<PlayerStateManager>();
        return _player && _player.IsDead;
    }

    // The hint (tip skip) button can't be used while the player is out / "Mission Failed" is showing.
    bool HintBlocked() => PlayerDead() || UIManager.Instance.IsLoosePanelOpen;

    void UpdateHintButton()
    {
        if (!bulpimg) return;
        if (!_hintGroup) _hintGroup = bulpimg.GetComponent<CanvasGroup>();
        if (!_hintGroup) _hintGroup = bulpimg.gameObject.AddComponent<CanvasGroup>();
        bool blocked = HintBlocked();
        _hintGroup.alpha = blocked ? 0f : 1f;
        _hintGroup.interactable = !blocked;
        _hintGroup.blocksRaycasts = !blocked;
    }

    // Called when the tips popup has been closed (its own close button or the quiz close button).
    public IEnumerator AfterTipSkipClosed(bool reachedAllTips)
    {
        yield return null; // let the button finish its other actions (resume, hide panels)
        if (!tipskippanel || tipskippanel.activeInHierarchy) yield break;
        var retryHome = lastsummary ? lastsummary.transform.parent.Find("LastContinue") : null;
        if ((retryHome && retryHome.gameObject.activeInHierarchy) || (lastsummary && lastsummary.activeInHierarchy)) yield break;

        if (PlayerDead())
            UIManager.Instance.ShowLoosePanelNow();          // never leave an empty level behind
        else if (reachedAllTips && retryHome)
        {
            Time.timeScale = 0;
            retryHome.gameObject.SetActive(true);              // "play again or head back" with Retry / Home
        }
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
    

    public void BackHome_BuffManager()
    {
        GameManager.Instance.BackHome();
        SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 1.0f);
        
    }

    public void Buff_Reloadscene()
    {
        StartCoroutine(CloseAllThenRestart());   // Playagian plays the click sound
    }

    // Retry: close every popup still on screen (summary, quiz, tips...) with their close animation,
    // then restart the level once they have shrunk away.
    static readonly string[] _endPopups = { "LastContinue", "LastSummary", "Questionpanel", "TipsSkip", "TipsPanel", "Lastpage", "WithoutKey", "TIPHint" };

    IEnumerator CloseAllThenRestart()
    {
        var canvas = lastsummary ? lastsummary.transform.parent : null;
        if (canvas)
            foreach (var n in _endPopups)
            {
                var t = canvas.Find(n);
                if (t && t.gameObject.activeSelf) t.gameObject.SetActive(false);
            }
        if (lastsummary) lastsummary.SetActive(false);
        yield return new WaitForSecondsRealtime(0.3f);   // let the close animation play
        GameManager.Instance.Playagian();
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
        UpdateHintButton();

        // Closing the tips popup (its close button, or the quiz close button) must not drop the
        // player into the level when they are out or have read every tip.
        bool tipsOpen = tipskippanel && tipskippanel.activeInHierarchy;
        if (_tipsWasOpen && !tipsOpen)
        {
            if (!_tipCtrl) _tipCtrl = FindFirstObjectByType<Tipskipcontroller>(FindObjectsInactive.Include);
            StartCoroutine(AfterTipSkipClosed(_tipCtrl && _tipCtrl.ReachedAllTips));
        }
        _tipsWasOpen = tipsOpen;
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
        ExitConfirmPopup.Show(QuitGame);
    }

    private void QuitGame()
    {
        //Application.OpenURL("https://beta.skillhub.sq1.security/course/view.php?id=3");
       // Application.OpenURL("http://192.168.8.59/course/view.php?id=3");
        Application.Quit();
    }
}
