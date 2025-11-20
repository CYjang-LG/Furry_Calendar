using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Google OAuth 2.0 PKCE 방식 인증 (모바일 앱용)
/// Client Secret 없이 안전하게 인증
/// </summary>
public class GoogleOAuthManager : MonoBehaviour
{
    public static GoogleOAuthManager Instance { get; private set; }

    [Header("OAuth Settings")]
    [SerializeField] private string clientId = ""; // Inspector에서 입력
    [SerializeField] private string redirectUri = "com.yourcompany.calendarapp:/oauth2redirect";
    
    private const string SCOPE = "https://www.googleapis.com/auth/calendar.readonly";
    private const string AUTH_URL = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TOKEN_URL = "https://oauth2.googleapis.com/token";
    
    private const string ACCESS_TOKEN_KEY = "GoogleAccessToken";
    private const string REFRESH_TOKEN_KEY = "GoogleRefreshToken";
    private const string TOKEN_EXPIRY_KEY = "TokenExpiryTime";

    public string AccessToken { get; private set; }
    private string refreshToken;
    private DateTime tokenExpiryTime;
    
    // PKCE 관련
    private string codeVerifier;
    private string codeChallenge;

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

    /// <summary>
    /// PKCE Code Verifier 생성
    /// </summary>
    private void GeneratePKCECodes()
    {
        // Code Verifier: 43-128자 랜덤 문자열
        const string unreservedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var random = new System.Random();
        var sb = new StringBuilder();
        
        for (int i = 0; i < 128; i++)
        {
            sb.Append(unreservedChars[random.Next(unreservedChars.Length)]);
        }
        
        codeVerifier = sb.ToString();

        // Code Challenge: Base64URL(SHA256(codeVerifier))
        using (var sha256 = SHA256.Create())
        {
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            codeChallenge = Base64UrlEncode(challengeBytes);
        }
    }

    /// <summary>
    /// Base64 URL 인코딩
    /// </summary>
    private string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        base64 = base64.Replace("+", "-");
        base64 = base64.Replace("/", "_");
        base64 = base64.Replace("=", "");
        return base64;
    }

    public void StartAuthentication()
    {
        GeneratePKCECodes();

        string authUrl = $"{AUTH_URL}?" +
            $"client_id={Uri.EscapeDataString(clientId)}&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"response_type=code&" +
            $"scope={Uri.EscapeDataString(SCOPE)}&" +
            $"code_challenge={codeChallenge}&" +
            $"code_challenge_method=S256&" +
            $"access_type=offline&" +
            $"prompt=consent";
        
        Application.OpenURL(authUrl);
        Debug.Log("브라우저에서 인증을 진행하세요.");
        Debug.Log($"Redirect URI: {redirectUri}");
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
        form.AddField("redirect_uri", redirectUri);
        form.AddField("grant_type", "authorization_code");
        form.AddField("code_verifier", codeVerifier); // PKCE

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
        // 암호화하여 저장 (간단한 예제)
        PlayerPrefs.SetString(ACCESS_TOKEN_KEY, EncryptString(AccessToken));
        PlayerPrefs.SetString(REFRESH_TOKEN_KEY, EncryptString(refreshToken));
        PlayerPrefs.SetString(TOKEN_EXPIRY_KEY, tokenExpiryTime.ToString("o"));
        PlayerPrefs.Save();
    }

    private void LoadTokens()
    {
        string encryptedAccessToken = PlayerPrefs.GetString(ACCESS_TOKEN_KEY, "");
        string encryptedRefreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY, "");
        
        AccessToken = !string.IsNullOrEmpty(encryptedAccessToken) ? DecryptString(encryptedAccessToken) : "";
        refreshToken = !string.IsNullOrEmpty(encryptedRefreshToken) ? DecryptString(encryptedRefreshToken) : "";
        
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

    /// <summary>
    /// 간단한 암호화 (실제 프로덕션에서는 더 강력한 암호화 사용 권장)
    /// </summary>
    private string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        
        byte[] data = Encoding.UTF8.GetBytes(plainText);
        string base64 = Convert.ToBase64String(data);
        return base64;
    }

    private string DecryptString(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return "";
        
        try
        {
            byte[] data = Convert.FromBase64String(encryptedText);
            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return "";
        }
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
