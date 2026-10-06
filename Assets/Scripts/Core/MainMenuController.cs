using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("ѕанели (экраны)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject upgradesPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject loadingScreen;

    [Header(" нопки главного меню")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button upgradesButton;
    [SerializeField] private Button settingsButton;

    [Header(" нопки магазина")]
    [SerializeField] private Button shopBackButton;

    [Header(" нопки прокачки")]
    [SerializeField] private Button upgradesBackButton;

    [Header(" нопки настроек")]
    [SerializeField] private Button settingsBackButton;

    [Header("—цены")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private bool _isLoading;

    void Start()
    {
        playButton.onClick.AddListener(OnPlayPressed);
        shopButton.onClick.AddListener(() => ShowPanel(shopPanel));
        upgradesButton.onClick.AddListener(() => ShowPanel(upgradesPanel));
        settingsButton.onClick.AddListener(() => ShowPanel(settingsPanel));

        shopBackButton.onClick.AddListener(() => ShowPanel(mainMenuPanel));
        upgradesBackButton.onClick.AddListener(() => ShowPanel(mainMenuPanel));
        settingsBackButton.onClick.AddListener(() => ShowPanel(mainMenuPanel));

        ShowPanel(mainMenuPanel);
        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    private void ShowPanel(GameObject panel)
    {
        mainMenuPanel.SetActive(false);
        shopPanel.SetActive(false);
        upgradesPanel.SetActive(false);
        settingsPanel.SetActive(false);

        panel.SetActive(true);
    }

    private void OnPlayPressed()
    {
        if (_isLoading) return;
        StartCoroutine(LoadGameplayAsync());
    }

    private IEnumerator LoadGameplayAsync()
    {
        _isLoading = true;
        GameDataManager.Instance.ForceSave();

        if (loadingScreen != null) loadingScreen.SetActive(true);

        var op = SceneManager.LoadSceneAsync(gameplaySceneName);
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
    }
}