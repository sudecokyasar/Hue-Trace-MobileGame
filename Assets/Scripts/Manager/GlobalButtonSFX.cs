using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sahnedeki (kapalý paneller/inaktif objeler dahil) tüm UI Button'lara
/// otomatik olarak týklama ses efekti baðlar.
///
/// KULLANIM: Sahneye boþ bir GameObject oluþtur (örn. "GlobalButtonSFX"),
/// bu scripti ekle.
///
/// NOT: registeredButtons static bir HashSet. Unity "Enter Play Mode
/// Options" ayarlarýnda "Reload Domain" kapalýysa, Play'e her basýþta
/// static alanlar SIFIRLANMAZ ama sahnedeki gerçek onClick listener'larý
/// Play'den çýkýnca temizlenir. Bu ikisi çakýþýnca "ilk seferde çalýþýp
/// sonra çalýþmama" sorununa yol açar. Bu yüzden Awake'te HashSet elle
/// temizleniyor - böylece domain reload olsa da olmasa da her Play
/// baþlangýcýnda butonlar sýfýrdan ve doðru þekilde kaydedilir.
/// </summary>
public class GlobalButtonSFX : MonoBehaviour
{
    private static readonly HashSet<Button> registeredButtons = new HashSet<Button>();

    private void Awake()
    {
        // KRÝTÝK: Domain reload olmasa bile her Play baþlangýcýnda
        // temiz bir state ile baþla. Aksi halde önceki oturumdan kalan
        // "zaten kayýtlý" referanslar, gerçek listener'lar Play çýkýþýnda
        // temizlendiði için hiçbir butonun ses çalmamasýna sebep olur.
        registeredButtons.Clear();
    }

    private void Start()
    {
        RegisterAllButtonsInScene();
    }

    private void RegisterAllButtonsInScene()
    {
        // true -> inaktif (kapalý panellerdeki) objeleri de dahil et
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

        foreach (Button btn in allButtons)
        {
            // Sahne dýþý asset'leri (prefab'larýn proje penceresindeki
            // halini) atla, sadece sahnedeki gerçek objeleri al.
            if (btn.gameObject.scene.name == null) continue;
            if (!btn.gameObject.scene.isLoaded) continue;

            RegisterButton(btn);
        }
    }

    public static void RegisterButton(Button btn)
    {
        if (btn == null) return;
        if (registeredButtons.Contains(btn)) return;

        // Emniyet: ayný listener'ýn birden fazla eklenmesini önlemek için
        // önce kaldýrýp sonra ekliyoruz (RemoveListener referans bazlý
        // çalýþtýðý için zaten yoksa hiçbir þey yapmaz, hata vermez).
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