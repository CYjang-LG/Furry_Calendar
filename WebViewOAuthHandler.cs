using System.Collections;
using UnityEngine;

public class WebViewOAuthHandler : MonoBehaviour
{
    private WebViewObject webViewObject;

    public void ShowOAuthWebView(string authUrl, string redirectUri)
    {
#if UNITY_EDITOR
        Debug.Log("에디터에서는 WebView를 지원하지 않습니다.");
        Application.OpenURL(authUrl);
        return;
#endif

        if (webViewObject == null)
        {
            webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
            webViewObject.Init(
                cb: (msg) =>
                {
                    Debug.Log($"WebView 콜백: {msg}");
                },
                err: (msg) =>
                {
                    Debug.LogError($"WebView 에러: {msg}");
                },
                httpErr: (msg) =>
                {
                    Debug.LogError($"WebView HTTP 에러: {msg}");
                },
                ld: (msg) =>
                {
                    Debug.Log($"WebView 로딩: {msg}");
                    
                    // Redirect URI 감지
                    if (msg.StartsWith(redirectUri))
                    {
                        // Authorization Code 추출
                        string code = ExtractCodeFromUrl(msg);
                        if (!string.IsNullOrEmpty(code))
                        {
                            GoogleOAuthManager.Instance.ExchangeCodeForToken(code);
                        }
                        
                        // WebView 닫기
                        webViewObject.SetVisibility(false);
                    }
                },
                enableWKWebView: true);

#if UNITY_ANDROID
            webViewObject.SetMargins(0, 0, 0, 0);
#elif UNITY_IOS
            webViewObject.SetMargins(0, 0, 0, 0);
#endif
            
            webViewObject.SetVisibility(true);
        }

        webViewObject.LoadURL(authUrl);
    }

    private string ExtractCodeFromUrl(string url)
    {
        // URL에서 code 파라미터 추출
        int codeIndex = url.IndexOf("code=");
        if (codeIndex == -1) return "";

        int ampIndex = url.IndexOf("&", codeIndex);
        if (ampIndex == -1)
        {
            return url.Substring(codeIndex + 5);
        }
        else
        {
            return url.Substring(codeIndex + 5, ampIndex - codeIndex - 5);
        }
    }

    private void OnDestroy()
    {
        if (webViewObject != null)
        {
            Destroy(webViewObject.gameObject);
        }
    }
}
