using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class PortraitManager : MonoBehaviour
{
    [Header("UI Reference")]
    public Image portraitImage;

    [Header("Portraits")]
    public Sprite[] portraits;
    public string[] portraitNames;

    private Dictionary<string, Sprite> portraitMap;

    private void Awake()
    {
        portraitMap = new Dictionary<string, Sprite>();
        for (int i = 0; i < portraitNames.Length; i++)
        {
            if (!portraitMap.ContainsKey(portraitNames[i]))
            {
                portraitMap.Add(portraitNames[i], portraits[i]);
            }
        }
    }

    private void Start()
    {
        portraitImage.enabled = false;


        var dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler<string>("show_portrait", ShowPortrait);
            dialogueRunner.AddCommandHandler("hide_portrait", HidePortrait);
        }
        else
        {
            Debug.LogError("No DialogueRunner found in the scene.");
        }
    }

    private void ShowPortrait(string name)
    {
        Debug.Log($"[PortraitManager] Showing portrait: {name}");

        if (portraitMap.TryGetValue(name, out Sprite sprite))
        {
            portraitImage.sprite = sprite;
            portraitImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[PortraitManager] No sprite found for name '{name}'");
        }
    }

    private void HidePortrait()
    {
        portraitImage.enabled = false;
    }
}
