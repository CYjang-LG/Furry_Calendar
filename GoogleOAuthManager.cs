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
        string authUrl = $"{AUTH_URL}?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope={Uri.EscapeDataString(SCOPE)}&access_t
