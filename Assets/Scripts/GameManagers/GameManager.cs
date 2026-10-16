using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;



public class GameManager : BaseSingleton<GameManager>
{
    [SerializeField] float _delayTrans;
    [SerializeField] float _delayPlayThemeMusic;
    bool _isReplay; //Nếu replay thì chờ loadscene r hẵng restart HP
    bool _fullUnlock;
    bool _deleteScene1Data;
    public int Keycount = 0,checktipcount = 0;
    public bool isplay,iscontinue,ischeck;
    
    public Texttypewritring texttypewritring;
    public TypeEffectTextInit typewritring;

    public GameObject instructionspanel;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
    
    public void ButtonclickSound()
    {
        SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 1.0f);
    }

    public void SwitchNextScene()
    {
        if (!isplay)
        {
            UIManager.Instance.IncreaseTransitionCanvasOrder();
            StartCoroutine(SwitchScene(SceneManager.GetActiveScene().buildIndex + 1));
            SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 1.0f);
            StartCoroutine(PlayNextSceneSong(SceneManager.GetActiveScene().buildIndex + 1, true));
            ResetGameData();
            
            isplay = true;
        }
        
        Invoke("Disableinstructionspnale",4f);
        
    }
    void Disableinstructionspnale()
    {
        instructionspanel.SetActive(false);
    }
    
    private IEnumerator PlayNextSceneSong(int index, bool needWait)
    {
        if (needWait)
            yield return new WaitForSeconds(_delayPlayThemeMusic);

        switch (index)
        {
            case 0:
                SoundsManager.Instance.PlayMusic(GameEnums.ESoundName.StartMenuTheme);
                break;
            case 1:
                SoundsManager.Instance.PlayMusic(GameEnums.ESoundName.Level1Theme);
                break;
            case 2:
                SoundsManager.Instance.PlayMusic(GameEnums.ESoundName.Level2Theme);
               
                break;
        }
    }

    private void ResetGameData()
    {
        PlayerHealthManager.Instance.DecreaseHP();
        PlayerHealthManager.Instance.RestartHP();
        PlayerPrefs.DeleteAll();
        _deleteScene1Data = false;
        Keycount = 0;
        checktipcount = 0;
        texttypewritring.Reset_typewritring();
        typewritring.Reset_typewritring();
    }

    public void Hideinstructionspanel()
    {
        texttypewritring.Disableinstructionspanel();
    }
    

    public void SwitchToScene(int sceneIndex)
    {
        StartCoroutine(SwitchScene(sceneIndex));
    }
    public IEnumerator SwitchScene(int sceneIndex)
    {
        UIManager.Instance.IncreaseTransitionCanvasOrder();
        UIManager.Instance.PopDownAllPanels();
        UIManager.Instance.TriggerAnimation(GameConstants.SCENE_TRANS_END);
            
        yield return new WaitForSeconds(_delayTrans);
        
        if (SceneManager.GetActiveScene().buildIndex == 1 && sceneIndex == 2)
            _deleteScene1Data = true;
        SceneManager.LoadSceneAsync(sceneIndex);
        UIManager.Instance.TriggerAnimation(GameConstants.SCENE_TRANS_START);
        SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.SceneEntrySfx, 1.0f);
        if (_isReplay)
        {
            _isReplay = false;
            PlayerHealthManager.Instance.RestartHP();
            if (SoundsManager.Instance.IsPlayingBossTheme)
                SoundsManager.Instance.PlayMusic(GameEnums.ESoundName.Level2Theme);
            
        }
        else if (sceneIndex == GameConstants.GAME_LEVEL_2)
        {
            if (PlayerPrefs.HasKey(GameEnums.ESpecialStates.PlayerSkillUnlockedLV2.ToString()))
                _fullUnlock = true;
            else
                _fullUnlock = false;
            
            if (_deleteScene1Data)
            {
                _deleteScene1Data = false; //xoá xong thì trả về false
                PlayerPrefs.DeleteAll();
                StartCoroutine(PlayNextSceneSong(sceneIndex, false));
               
            }
            PlayerPrefs.SetString((_fullUnlock) ? GameEnums.ESpecialStates.PlayerSkillUnlockedLV2.ToString() : GameEnums.ESpecialStates.PlayerSkillUnlockedLV1.ToString(), "Unlocked");
            PlayerPrefs.Save();

            PlayerHealthManager.Instance.IncreaseHP();
        }
        
    }

    public void ReloadScene()
    {
        checktipcount = 0;
        Time.timeScale = 1.0f;
        SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 1.0f);
        UIManager.Instance.PopDownAllPanels();
        _isReplay = true;
        SwitchToScene(SceneManager.GetActiveScene().buildIndex);
       
    }

    public void Playagian()
    {
       
        Time.timeScale = 1.0f;
        checktipcount = 0;
        Keycount = 0;
        ischeck = false;
        iscontinue = false;
        PlayerPrefs.DeleteAll();
        SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 1.0f);
        UIManager.Instance.PopDownAllPanels();
        _isReplay = true;
        SwitchToScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackHome()
    {
        SoundsManager.Instance.PlaySfx(GameEnums.ESoundName.ButtonSelectedSfx, 1.0f);
        UIManager.Instance.PopDownAllPanels();
        UIManager.Instance.StartMenuCanvas.SetActive(true);
        SceneManager.LoadSceneAsync(GameConstants.GAME_MENU);
        StartCoroutine(PlayNextSceneSong(GameConstants.GAME_MENU, false));
        isplay = false;
        
    }

    public void Restart()
    {
        UIManager.Instance.PopDownAllPanels();
        EventsManager.Instance.NotifyObservers(GameEnums.EEvents.ObjectOnRestart, null);
        ResetGameData();
        StartCoroutine(PlayNextSceneSong(GameConstants.GAME_LEVEL_1, true));
        SwitchToScene(GameConstants.GAME_LEVEL_1);
        
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.DeleteAll();
        PlayerHealthManager.Instance.DecreaseHP();
       
    }

    public void ContinueTypeeffect1()
    {
        typewritring.RestartTypewriting();
    }

    public void ContinueTypeeffect2()
    {
        texttypewritring.RestartTypewriting();
    }

}
