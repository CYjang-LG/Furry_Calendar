using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [Header("Character Sprites")]
    [SerializeField] private Image miniCharacterImage;
    [SerializeField] private Image fullCharacterImage;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialogueBubble;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float dialogueDisplayDuration = 3f;

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;

    [Header("Dialogue Data")]
    [SerializeField] private TextAsset dialogueDataJson;

    private DialogueData dialogueData;
    private Coroutine currentDialogueCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadDialogueData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SwitchToMiniMode();
        HideDialogue();
        ShowRandomGreeting();
    }

    private void LoadDialogueData()
    {
        if (dialogueDataJson != null)
        {
            try
            {
                dialogueData = JsonUtility.FromJson<DialogueData>(dialogueDataJson.text);
            }
            catch
            {
                Debug.LogWarning("대사 JSON 로드 실패, 기본 대사 사용");
                dialogueData = CreateDefaultDialogueData();
            }
        }
        else
        {
            dialogueData = CreateDefaultDialogueData();
        }
    }

    private DialogueData CreateDefaultDialogueData()
    {
        return new DialogueData
        {
            greetings = new List<string>
            {
                "안녕하세요!",
                "좋은 하루예요!",
                "오늘도 화이팅!"
            },
            completionMessages = new List<string>
            {
                "잘했어요!",
                "멋져요!",
                "정말 훌륭해요!",
                "최고예요!",
                "대단해요!"
            },
            morningGreetings = new List<string> { "좋은 아침이에요!" },
            afternoonGreetings = new List<string> { "점심 맛있게 드셨나요?" },
            eveningGreetings = new List<string> { "오늘 하루 수고하셨어요!" },
            nightGreetings = new List<string> { "푹 쉬세요!" }
        };
    }

    public void SwitchToMiniMode()
    {
        if (miniCharacterImage != null)
        {
            miniCharacterImage.gameObject.SetActive(true);
        }

        if (fullCharacterImage != null)
        {
            fullCharacterImage.gameObject.SetActive(false);
        }
    }

    public void SwitchToFullMode()
    {
        if (miniCharacterImage != null)
        {
            miniCharacterImage.gameObject.SetActive(false);
        }

        if (fullCharacterImage != null)
        {
            fullCharacterImage.gameObject.SetActive(true);
        }
    }

    public void PlayAnimation(string animationName)
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(animationName);
        }
    }

    public void ShowRandomGreeting()
    {
        int hour = System.DateTime.Now.Hour;
        List<string> greetings;

        if (hour >= 5 && hour < 12)
        {
            greetings = dialogueData.morningGreetings;
        }
        else if (hour >= 12 && hour < 17)
        {
            greetings = dialogueData.afternoonGreetings;
        }
        else if (hour >= 17 && hour < 21)
        {
            greetings = dialogueData.eveningGreetings;
        }
        else
        {
            greetings = dialogueData.nightGreetings;
        }

        if (greetings.Count > 0)
        {
            string greeting = greetings[Random.Range(0, greetings.Count)];
            ShowDialogue(greeting);
        }
    }

    public void ShowCompletionDialogue()
    {
        if (dialogueData.completionMessages.Count > 0)
        {
            string message = dialogueData.completionMessages[Random.Range(0, dialogueData.completionMessages.Count)];
            ShowDialogue(message);
        }
    }

    public void ShowDialogue(string message)
    {
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
        }

        currentDialogueCoroutine = StartCoroutine(ShowDialogueCoroutine(message));
    }

    private IEnumerator ShowDialogueCoroutine(string message)
    {
        if (dialogueBubble != null)
        {
            dialogueBubble.SetActive(true);
        }

        if (dialogueText != null)
        {
            dialogueText.text = message;
        }

        yield return new WaitForSeconds(dialogueDisplayDuration);

        HideDialogue();
    }

    private void HideDialogue()
    {
        if (dialogueBubble != null)
        {
            dialogueBubble.SetActive(false);
        }
    }

    [System.Serializable]
    private class DialogueData
    {
        public List<string> greetings;
        public List<string> completionMessages;
        public List<string> morningGreetings;
        public List<string> afternoonGreetings;
        public List<string> eveningGreetings;
        public List<string> nightGreetings;
    }
}
