using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CompletionPopup : MonoBehaviour
{
    public static CompletionPopup Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI celebrationText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject tapToCloseArea;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float autoCloseDuration = 2.5f;

    [Header("Character")]
    [SerializeField] private Transform fullCharacterTransform;

    private Coroutine autoCloseCoroutine;

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

        InitializeUI();
    }

    private void InitializeUI()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (tapToCloseArea != null)
        {
            Button tapButton = tapToCloseArea.GetComponent<Button>();
            if (tapButton == null)
            {
                tapButton = tapToCloseArea.AddComponent<Button>();
            }
            tapButton.onClick.AddListener(Close);
        }
    }

    public void Show(string eventTitle)
    {
        if (popupPanel == null) return;

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }

        popupPanel.SetActive(true);
        
        DisplayCelebrationMessage(eventTitle);
        
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.SwitchToFullMode();
            CharacterManager.Instance.PlayAnimation("Happy");
            CharacterManager.Instance.ShowCompletionDialogue();
        }

        StartCoroutine(FadeIn());
        
        autoCloseCoroutine = StartCoroutine(AutoClose());
    }

    public void Close()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }

        StartCoroutine(FadeOutAndClose());
    }

    private void DisplayCelebrationMessage(string eventTitle)
    {
        if (celebrationText != null)
        {
            celebrationText.text = $"'{eventTitle}' 완료!";
        }
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutAndClose()
    {
        if (canvasGroup == null)
        {
            CloseImmediate();
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        CloseImmediate();
    }

    private void CloseImmediate()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.SwitchToMiniMode();
        }
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseDuration);
        Close();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }
}
