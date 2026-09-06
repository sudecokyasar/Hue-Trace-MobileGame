using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GlobalButtonSFX : MonoBehaviour
{
    private static readonly HashSet<Button> registeredButtons = new HashSet<Button>();

    private void Awake()
    {
        registeredButtons.Clear();
    }

    private void Start()
    {
        RegisterAllButtonsInScene();
    }

    private void RegisterAllButtonsInScene()
    {
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

        foreach (Button btn in allButtons)
        {

            if (btn.gameObject.scene.name == null) continue;
            if (!btn.gameObject.scene.isLoaded) continue;

            RegisterButton(btn);
        }
    }

    public static void RegisterButton(Button btn)
    {
        if (btn == null) return;
        if (registeredButtons.Contains(btn)) return;


        btn.onClick.RemoveListener(PlayClickSfx);
        btn.onClick.AddListener(PlayClickSfx);
        registeredButtons.Add(btn);
    }

    private static void PlayClickSfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClickSfx();
    }
}