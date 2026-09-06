using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CellView : MonoBehaviour
{
    [Header("Görsel Bileþenler")]
    [SerializeField] private SpriteRenderer bgRenderer;
    [SerializeField] private SpriteRenderer stoneRenderer;

    [Header("Buz Hücresi Görselleri")]
    [SerializeField] private Sprite iceSprite;        // Saðlam buz (SetIce)
    [SerializeField] private Sprite brokenIceSprite;  // Kýrýk buz (CollapseIce)

    [Header("Köprü Hücresi Görseli")]
    [SerializeField] private Sprite bridgeSprite;     // Köprü (SetBridge) - tek görsel, deðiþmiyor

    [Header("Hücre Bilgisi")]
    public Vector2Int GridPos { get; private set; }
    public bool HasStone { get; private set; }
    public Color StoneColor { get; private set; }

    // Kilit Mekaniði
    public bool IsLocked { get; private set; } = false;
    public Color AllowedColor { get; private set; }

    // Buz Mekaniði
    public bool IsIce { get; private set; } = false;
    public bool IsCollapsed { get; private set; } = false;

    // Köprü Mekaniði
    public bool IsBridge { get; private set; } = false;

    // Renk Karýþtýrma Mekaniði
    public bool IsMixCell { get; private set; } = false;
    public ColorMixRuleData MixRule { get; private set; }
    public bool IsMixActivated { get; private set; } = false;

    private BoxCollider2D boxCollider;

    // Varsayýlan arkaplan sprite'ý ile özel sprite'lar (ice/bridge) farklý
    // piksel boyutunda/PPU'da olabilir. Bunu telafi etmek için varsayýlan
    // sprite'ýn "bounds" boyutunu ve transform ölçeðini saklayýp, her yeni
    // sprite atandýðýnda orana göre otomatik ölçekliyoruz.
    private Sprite defaultBgSprite;
    private Vector3 defaultBgLocalScale = Vector3.one;
    private bool bgDefaultsCaptured = false;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();

        if (bgRenderer != null && !bgDefaultsCaptured)
        {
            defaultBgSprite = bgRenderer.sprite;
            defaultBgLocalScale = bgRenderer.transform.localScale;
            bgDefaultsCaptured = true;
        }
    }

    // Farklý boyuttaki bir sprite'ý bgRenderer'a atarken, varsayýlan
    // arkaplanla ayný görünür boyutta çýkmasý için transform ölçeðini
    // otomatik düzeltir.
    private void ApplyBgSprite(Sprite sprite)
    {
        if (bgRenderer == null || sprite == null) return;

        bgRenderer.sprite = sprite;
        bgRenderer.color = Color.white; // sprite kendi rengiyle görünsün, tint bozmasýn

        if (defaultBgSprite != null)
        {
            Vector2 defaultSize = defaultBgSprite.bounds.size;
            Vector2 newSize = sprite.bounds.size;

            if (defaultSize.x > 0f && defaultSize.y > 0f && newSize.x > 0f && newSize.y > 0f)
            {
                float scaleX = defaultSize.x / newSize.x;
                float scaleY = defaultSize.y / newSize.y;

                bgRenderer.transform.localScale = new Vector3(
                    defaultBgLocalScale.x * scaleX,
                    defaultBgLocalScale.y * scaleY,
                    defaultBgLocalScale.z);
            }
        }
    }

    public void Initialize(Vector2Int pos, float cellSize)
    {
        GridPos = pos;
        gameObject.name = $"Cell_{pos.x}_{pos.y}";

        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        //boxCollider.size = new Vector2(cellSize, cellSize);

        if (stoneRenderer != null)
        {
            stoneRenderer.gameObject.SetActive(false);
        }

        // Hücre yeniden kullanýlýrsa (yeni level), önceki ice/bridge
        // sprite'ýndan kalma özel ölçek/sprite sýfýrlanýr.
        if (bgRenderer != null && bgDefaultsCaptured)
        {
            bgRenderer.sprite = defaultBgSprite;
            bgRenderer.transform.localScale = defaultBgLocalScale;
        }

        IsLocked = false;
        IsIce = false;
        IsCollapsed = false;
        IsBridge = false;
        IsMixCell = false;
        IsMixActivated = false;
    }

    public void SetStone(Color color)
    {
        HasStone = true;
        StoneColor = color;

        if (stoneRenderer != null)
        {
            stoneRenderer.gameObject.SetActive(true);
            stoneRenderer.color = color;
        }
    }

    public void SetLocked(Color allowedColor)
    {
        IsLocked = true;
        AllowedColor = allowedColor;

        if (bgRenderer != null)
        {
            Color lockBg = allowedColor * 0.45f;
            lockBg.a = 1f;
            bgRenderer.color = lockBg;
        }
    }

    public void SetIce()
    {
        IsIce = true;
        IsCollapsed = false;

        ApplyBgSprite(iceSprite);
    }

    public void CollapseIce()
    {
        if (!IsIce) return;
        if (IsCollapsed) return; // zaten kýrýk, tekrar ses çalmasýn

        IsCollapsed = true;

        ApplyBgSprite(brokenIceSprite);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayIceBreakSfx();
    }

    // Restart/Try Again gibi durumlarda, level yeniden üretilmeden
    // (ayný hücre nesneleri korunarak) kýrýlmýþ buzu tekrar saðlam hale
    // döndürmek için kullanýlýr. IsIce false ise hiçbir þey yapmaz.
    public void ResetIceState()
    {
        if (!IsIce) return;
        if (!IsCollapsed) return;

        IsCollapsed = false;
        ApplyBgSprite(iceSprite);
    }

    public void SetBridge()
    {
        IsBridge = true;

        ApplyBgSprite(bridgeSprite);
    }

    public void SetMixCell(ColorMixRuleData rule)
    {
        IsMixCell = true;
        MixRule = rule;
        IsMixActivated = false;
        HasStone = false; // Taþ baþlangýçta kapalý
        if (stoneRenderer != null) stoneRenderer.gameObject.SetActive(false);

        if (bgRenderer != null)
        {
            bgRenderer.color = new Color(0.40f, 0.25f, 0.45f, 1f); // Mor karýþým karesi
        }
    }

    public void ActivateMixResult()
    {
        if (!IsMixCell || IsMixActivated) return;

        IsMixActivated = true;
        SetStone(MixRule.resultColor);
    }

    public void DeactivateMixResult()
    {
        if (!IsMixCell || !IsMixActivated) return;

        IsMixActivated = false;
        HasStone = false;
        if (stoneRenderer != null)
        {
            stoneRenderer.gameObject.SetActive(false);
        }
    }

    public void SetBackgroundColor(Color color)
    {
        if (bgRenderer != null)
        {
            bgRenderer.color = color;
        }
    }
}