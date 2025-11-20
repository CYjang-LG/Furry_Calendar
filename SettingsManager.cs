using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Toggle notificationToggle;
    [SerializeField] private TMP_Dropdown notificationTimeDropdown;
    [SerializeField] private Toggle autoSyncToggle;
    [SerializeField] private TMP_Dropdown syncIntervalDropdown;
    [SerializeField] private TextMeshProUGUI accountInfoText;
    [SerializeField] private Button logoutButton;
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private Button backButton;

    [Header("Settings")]
    private const string NOTIFICATION_ENABLED_KEY = "NotificationEnabled";
    private const string NOTIFICATION_TIME_KEY = "NotificationTime";
    private const string AUTO_SYNC_KEY = "AutoSyncEnabled";
    private const string SYNC_INTERVAL_KEY = "SyncInterval";

    public bool NotificationEnabled { get; private set; }
    public int NotificationMinutesBefore { get; private set; }
    public bool AutoSyncEnabled { get; private set; }
    public int SyncIntervalMinutes { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();
    }

    private void Start()
    {
        InitializeUI();
        UpdateAccountInfo();
    }

    private void InitializeUI()
    {
        if (notificationToggle != null)
        {
            notificationToggle.isOn = NotificationEnabled;
            notificationToggle.onValueChanged.AddListener(OnNotificationToggleChanged);
        }

        if (notificationTimeDropdown != null)
        {
            notificationTimeDropdown.value = GetNotificationTimeDropdownIndex(NotificationMinutesBefore);
            notificationTimeDropdown.onValueChanged.AddListener(OnNotificationTimeChanged);
        }

        if (autoSyncToggle != null)
        {
            autoSyncToggle.isOn = AutoSyncEnabled;
            autoSyncToggle.onValueChanged.AddListener(OnAutoSyncToggleChanged);
        }

        if (syncIntervalDropdown != null)
        {
            syncIntervalDropdown.value = GetSyncIntervalDropdownIndex(SyncIntervalMinutes);
            syncIntervalDropdown.onValueChanged.AddListener(OnSyncIntervalChanged);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (versionText != null)
        {
            versionText.text = $"버전 {Application.version}";
        }
    }

    private void LoadSettings()
    {
        NotificationEnabled = PlayerPrefs.GetInt(NOTIFICATION_ENABLED_KEY, 1) == 1;
        NotificationMinutesBefore = PlayerPrefs.GetInt(NOTIFICATION_TIME_KEY, 15);
        AutoSyncEnabled = PlayerPrefs.GetInt(AUTO_SYNC_KEY, 1) == 1;
        SyncIntervalMinutes = PlayerPrefs.GetInt(SYNC_INTERVAL_KEY, 30);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(NOTIFICATION_ENABLED_KEY, NotificationEnabled ? 1 : 0);
        PlayerPrefs.SetInt(NOTIFICATION_TIME_KEY, NotificationMinutesBefore);
        PlayerPrefs.SetInt(AUTO_SYNC_KEY, AutoSyncEnabled ? 1 : 0);
        PlayerPrefs.SetInt(SYNC_INTERVAL_KEY, SyncIntervalMinutes);
        PlayerPrefs.Save();
    }

    private void OnNotificationToggleChanged(bool isOn)
    {
        NotificationEnabled = isOn;
        SaveSettings();

        if (NotificationManager.Instance != null)
        {
            if (isOn)
            {
                NotificationManager.Instance.EnableNotifications();
            }
            else
            {
                NotificationManager.Instance.DisableNotifications();
            }
        }
    }

    private void OnNotificationTimeChanged(int index)
    {
        NotificationMinutesBefore = GetNotificationMinutesFromIndex(index);
        SaveSettings();

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.UpdateNotificationTime(NotificationMinutesBefore);
        }
    }

    private void OnAutoSyncToggleChanged(bool isOn)
    {
        AutoSyncEnabled = isOn;
        SaveSettings();

        if (SyncManager.Instance != null)
        {
            if (isOn)
            {
                SyncManager.Instance.StartAutoSync(SyncIntervalMinutes);
            }
            else
            {
                SyncManager.Instance.StopAutoSync();
            }
        }
    }

    private void OnSyncIntervalChanged(int index)
    {
        SyncIntervalMinutes = GetSyncMinutesFromIndex(index);
        SaveSettings();

        if (SyncManager.Instance != null && AutoSyncEnabled)
        {
            SyncManager.Instance.StartAutoSync(SyncIntervalMinutes);
        }
    }

    private void UpdateAccountInfo()
    {
        if (accountInfoText != null)
        {
            if (GoogleOAuthManager.Instance != null && GoogleOAuthManager.Instance.IsAuthenticated)
            {
                accountInfoText.text = "Google 계정 연동됨";
            }
            else
            {
                accountInfoText.text = "로그인 필요";
            }
        }
    }

    private void OnLogoutClicked()
    {
        if (GoogleOAuthManager.Instance != null)
        {
            GoogleOAuthManager.Instance.Logout();
            UpdateAccountInfo();
            
            Debug.Log("로그아웃 완료");
        }
    }

    private void OnBackClicked()
    {
        gameObject.SetActive(false);
    }

    private int GetNotificationTimeDropdownIndex(int minutes)
    {
        switch (minutes)
        {
            case 15: return 0;
            case 30: return 1;
            case 60: return 2;
            default: return 0;
        }
    }

    private int GetNotificationMinutesFromIndex(int index)
    {
        switch (index)
        {
            case 0: return 15;
            case 1: return 30;
            case 2: return 60;
            default: return 15;
        }
    }

    private int GetSyncIntervalDropdownIndex(int minutes)
    {
        switch (minutes)
        {
            case 10: return 0;
            case 30: return 1;
            case 60: return 2;
            default: return 1;
        }
    }

    private int GetSyncMinutesFromIndex(int index)
    {
        switch (index)
        {
            case 0: return 10;
            case 1: return 30;
            case 2: return 60;
            default: return 30;
        }
    }

    private void OnDestroy()
    {
        if (notificationToggle != null)
        {
            notificationToggle.onValueChanged.RemoveListener(OnNotificationToggleChanged);
        }

        if (notificationTimeDropdown != null)
        {
            notificationTimeDropdown.onValueChanged.RemoveListener(OnNotificationTimeChanged);
        }

        if (autoSyncToggle != null)
        {
            autoSyncToggle.onValueChanged.RemoveListener(OnAutoSyncToggleChanged);
        }

        if (syncIntervalDropdown != null)
        {
            syncIntervalDropdown.onValueChanged.RemoveListener(OnSyncIntervalChanged);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(OnLogoutClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }
}
