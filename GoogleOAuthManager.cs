using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Google OAuth 2.0 인증을 처리하는 매니저
/// Access Token과 Refresh Token을 관리하고 자동 갱신
/// </summary>
public class GoogleOAuthManager : MonoBehaviour
{
    public static GoogleOAuthManager Instance { get; private set; }

    [Header("OAuth Settings")]
    [SerializeField] private string clientId = "YOUR_CLIENT_ID";
    [SerializeField] private string clientSecret = "YOUR_CLIENT_SECRET";
    [SerializeField] private string redirectUri = "http://localhost:8080";
    
    private const string SCOPE = "https://www.googleapis.com/auth/calendar.readonly";
    private const string AUTH_URL = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TOKEN_URL = "https://oauth2.googleapis.com/token";
    
    private const string ACCESS_TOKEN_KEY = "GoogleAccessToken";
    private const string REFRESH_TOKEN_KEY = "GoogleRefreshToken";
    private const string TOKEN_EXPIRY_KEY = "TokenExpiryTime";

    public string AccessToken { get; private set; }
    private string refreshToken;
    private DateTime tokenExpiryTime;

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) && DateTime.Now < tokenExpiryTime;

    public event Action OnAuthenticationSuccess;
    public event Action<string> OnAuthenticationFailed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTokens();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartAuthentication()
    {
        string authUrl = $"{AUTH_URL}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(SCOPE)}&access_type=offline&prompt=consent";
        Application.OpenURL(authUrl);
        Debug.Log("브라우저에서 인증을 진행하세요.");
    }

    public void ExchangeCodeForToken(string authCode)
    {
        StartCoroutine(ExchangeCodeCoroutine(authCode));
    }

    private IEnumerator ExchangeCodeCoroutine(string authCode)
    {
        WWWForm form = new WWWForm();
        form.AddField("code", authCode);
        form.AddField("client_id", clientId);
        form.AddField("client_secret", clientSecret);
        form.AddField("redirect_uri", redirectUri);
        form.AddField("grant_type", "authorization_code");

        using (UnityWebRequest request = UnityWebRequest.Post(TOKEN_URL, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                TokenResponse response = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                
                AccessToken = response.access_token;
                refreshToken = response.refresh_token;
                tokenExpiryTime = DateTime.Now.AddSeconds(response.expires_in - 300);

                SaveTokens();
                OnAuthenticationSuccess?.Invoke();
                Debug.Log("인증 성공!");
            }
            else
            {
                string error = $"토큰 교환 실패: {request.error}";
                Debug.LogError(error);
                OnAuthenticationFailed?.Invoke(error);
            }
        }
    }

    public IEnumerator RefreshAccessToken()
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            Debug.LogError("Refresh Token이 없습니다.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("client_id", clientId);
        form.AddField("client_secret", clientSecret);
        form.AddField("refresh_token", refreshToken);
        form.AddField("grant_type", "refresh_token");

        using (UnityWebRequest request = UnityWebRequest.Post(TOKEN_URL, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                TokenResponse response = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                AccessToken = response.access_token;
                tokenExpiryTime = DateTime.Now.AddSeconds(response.expires_in - 300);
                SaveTokens();
                Debug.Log("토큰 갱신 성공!");
            }
            else
            {
                Debug.LogError($"토큰 갱신 실패: {request.error}");
                Logout();
            }
        }
    }

    private void SaveTokens()
    {
        PlayerPrefs.SetString(ACCESS_TOKEN_KEY, AccessToken);
        PlayerPrefs.SetString(REFRESH_TOKEN_KEY, refreshToken);
        PlayerPrefs.SetString(TOKEN_EXPIRY_KEY, tokenExpiryTime.ToString("o"));
        PlayerPrefs.Save();
    }

    private void LoadTokens()
    {
        AccessToken = PlayerPrefs.GetString(ACCESS_TOKEN_KEY, "");
        refreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY, "");
        
        string expiryString = PlayerPrefs.GetString(TOKEN_EXPIRY_KEY, "");
        if (!string.IsNullOrEmpty(expiryString))
        {
            DateTime.TryParse(expiryString, out tokenExpiryTime);
        }

        if (!string.IsNullOrEmpty(refreshToken) && DateTime.Now >= tokenExpiryTime)
        {
            StartCoroutine(RefreshAccessToken());
        }
    }

    public void Logout()
    {
        AccessToken = "";
        refreshToken = "";
        tokenExpiryTime = DateTime.MinValue;

        PlayerPrefs.DeleteKey(ACCESS_TOKEN_KEY);
        PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
        PlayerPrefs.DeleteKey(TOKEN_EXPIRY_KEY);
        PlayerPrefs.Save();

        Debug.Log("로그아웃 완료");
    }

    [Serializable]
    private class TokenResponse
    {
        public string access_token;
        public string refresh_token;
        public int expires_in;
        public string token_type;
    }
}
