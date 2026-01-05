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

        
        if (portraitImage != null)
        {
            Canvas rootCanvas = portraitImage.GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                if (rootCanvas.isRootCanvas)
                {
                    rootCanvas.sortingOrder = 2000;
                    Debug.Log($"[PortraitManager] Set Canvas SortingOrder to {rootCanvas.sortingOrder}");
                }
                else
                {
                    
                    rootCanvas = rootCanvas.rootCanvas;
                    if (rootCanvas != null)
                    {
                        rootCanvas.sortingOrder = 2000;
                        Debug.Log($"[PortraitManager] Set Root Canvas SortingOrder to {rootCanvas.sortingOrder}");
                    }
                }

                
                if (rootCanvas != null)
                {
                    CanvasScaler scaler = rootCanvas.GetComponent<CanvasScaler>();
                    if (scaler == null)
                    {
                        scaler = rootCanvas.gameObject.AddComponent<CanvasScaler>();
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1920, 1080);
                        scaler.matchWidthOrHeight = 0.5f;
                        Debug.Log("[PortraitManager] Added and configured CanvasScaler (ScaleWithScreenSize)");
                    }
                    else if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize || scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPhysicalSize)
                    {
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1920, 1080);
                        scaler.matchWidthOrHeight = 0.5f;
                        Debug.Log("[PortraitManager] Reconfigured CanvasScaler to ScaleWithScreenSize");
                    }
                }

                
                
                RectTransform panelRect = portraitImage.transform.parent as RectTransform;
                if (panelRect != null && !rootCanvas.Equals(panelRect.GetComponent<Canvas>())) 
                {
                    
                    Debug.Log("[PortraitManager] Resetting Dialogue Panel Layout...");
                    
                    
                    panelRect.anchorMin = new Vector2(0.05f, 0.05f);
                    panelRect.anchorMax = new Vector2(0.95f, 0.95f);
                    panelRect.pivot = new Vector2(0.5f, 0.5f);
                    panelRect.offsetMin = Vector2.zero; 
                    panelRect.offsetMax = Vector2.zero;
                    
                    
                    LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
                }
            }
        }

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
            
            sprite = Resources.Load<Sprite>(name);
            if (sprite == null) sprite = Resources.Load<Sprite>("Portraits/" + name); 
            
            if (sprite != null)
            {
                
                portraitMap[name] = sprite; 
                portraitImage.sprite = sprite;
                portraitImage.enabled = true;
                Debug.Log($"[PortraitManager] Loaded '{name}' from Resources.");
            }
            else
            {
                Debug.LogWarning($"[PortraitManager] No sprite found for name '{name}' in Dictionary OR Resources.");
            }
        }
    }

    private void HidePortrait()
    {
        portraitImage.enabled = false;
    }
}
