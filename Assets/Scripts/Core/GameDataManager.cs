using System;
using System.Collections.Generic;
using UnityEngine;


public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    public int Coins { get; private set; }
    public int[] MetaUpgrades { get; private set; } = new int[5];
    public List<string> PurchasedSkins { get; private set; } = new List<string> { "default" };
    public string CurrentSkinId { get; private set; } = "default";
    public int NewGamePlusLevel { get; private set; }

    public event Action<int> OnCoinsChanged;
    public event Action<int, int> OnMetaUpgradeChanged;

    private static readonly int[] BASE_PRICES = { 400, 350, 600, 1500, 1200 };
    private static readonly int[] MAX_LEVELS = { 3, 3, 3, 2, 3 };
    private const float PRICE_GROWTH = 1.8f;

    private const string SAVE_KEY = "GameSave_v1";
    private const float SAVE_DEBOUNCE = 3f;
    private bool _dirty;
    private float _saveTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();

        Application.focusChanged += OnFocusChanged;
        Application.quitting += ForceSave;
    }

    void OnDestroy()
    {
        Application.focusChanged -= OnFocusChanged;
        Application.quitting -= ForceSave;
    }

    void Update()
    {
        if (!_dirty) return;
        _saveTimer -= Time.unscaledDeltaTime;
        if (_saveTimer <= 0f) ForceSave();
    }

    private void OnFocusChanged(bool focused)
    {
        if (!focused) ForceSave();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
        MarkDirty();
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount < 0 || Coins < amount) return false;
        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        MarkDirty();
        return true;
    }

    public bool IsSkinPurchased(string skinId) => PurchasedSkins.Contains(skinId);

    public bool PurchaseSkin(string skinId, int price)
    {
        if (IsSkinPurchased(skinId)) return false;
        if (!TrySpendCoins(price)) return false;
        PurchasedSkins.Add(skinId);
        MarkDirty();
        return true;
    }

    public void SetCurrentSkin(string skinId)
    {
        if (!IsSkinPurchased(skinId)) return;
        CurrentSkinId = skinId;
        MarkDirty();
    }

    public int GetMetaPrice(int index, int currentLevel)
    {
        if (index < 0 || index >= MAX_LEVELS.Length) return -1;
        if (currentLevel >= MAX_LEVELS[index]) return -1;
        float price = BASE_PRICES[index];
        for (int i = 0; i < currentLevel; i++) price *= PRICE_GROWTH;
        return Mathf.RoundToInt(price);
    }

    public bool UpgradeMeta(int index)
    {
        if (index < 0 || index >= MetaUpgrades.Length) return false;
        int price = GetMetaPrice(index, MetaUpgrades[index]);
        if (price < 0 || !TrySpendCoins(price)) return false;
        MetaUpgrades[index]++;
        OnMetaUpgradeChanged?.Invoke(index, MetaUpgrades[index]);
        MarkDirty();
        return true;
    }

    private void MarkDirty()
    {
        _dirty = true;
        _saveTimer = SAVE_DEBOUNCE;
    }

    public void ForceSave()
    {
        if (!_dirty) return;
        _dirty = false;

        var data = new SaveData
        {
            coins = Coins,
            meta = MetaUpgrades,
            skins = PurchasedSkins.ToArray(),
            skin = CurrentSkinId,
            ng = NewGamePlusLevel
        };
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();

    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
        try
        {
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SAVE_KEY));
            if (data == null) return;
            Coins = data.coins;
            if (data.meta != null && data.meta.Length == MetaUpgrades.Length)
                Array.Copy(data.meta, MetaUpgrades, MetaUpgrades.Length);
            if (data.skins != null)
                PurchasedSkins = new List<string>(data.skins);
            CurrentSkinId = string.IsNullOrEmpty(data.skin) ? "default" : data.skin;
            NewGamePlusLevel = data.ng;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Save corrupted, resetting: {e.Message}");
        }
    }

    [Serializable]
    private class SaveData
    {
        public int coins;
        public int[] meta;
        public string[] skins;
        public string skin;
        public int ng;
    }
}