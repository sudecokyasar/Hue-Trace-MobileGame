using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CellView : MonoBehaviour
{
    [Header("G�rsel Bile�enler")]
    [SerializeField] private SpriteRenderer bgRenderer;
    [SerializeField] private SpriteRenderer stoneRenderer;

    [Header("Buz H�cresi G�rselleri")]
    [SerializeField] private Sprite iceSprite;        
    [SerializeField] private Sprite brokenIceSprite;  

    [Header("K�pr� H�cresi G�rseli")]
    [SerializeField] private Sprite bridgeSprite;   

    [Header("H�cre Bilgisi")]
    public Vector2Int GridPos { get; private set; }
    public bool HasStone { get; private set; }
    public Color StoneColor { get; private set; }

    // Lock Mechanic
    public bool IsLocked { get; private set; } = false;
    public Color AllowedColor { get; private set; }

    // Ice Mechanic
    public bool IsIce { get; private set; } = false;
    public bool IsCollapsed { get; private set; } = false;

    // Bridge Mechanic
    public bool IsBridge { get; private set; } = false;

    // Color Mix Mechanic
    public bool IsMixCell { get; private set; } = false;
    public ColorMixRuleData MixRule { get; private set; }
    public bool IsMixActivated { get; private set; } = false;

    private BoxCollider2D boxCollider;
    
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


    private void ApplyBgSprite(Sprite sprite)
    {
        if (bgRenderer == null || sprite == null) return;

        bgRenderer.sprite = sprite;
        bgRenderer.color = Color.white;

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

        if (stoneRenderer != null)
        {
            stoneRenderer.gameObject.SetActive(false);
        }


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
        if (IsCollapsed) return; 

        IsCollapsed = true;

        ApplyBgSprite(brokenIceSprite);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayIceBreakSfx();
    }

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
        HasStone = false; // Ta� ba�lang��ta kapal�
        if (stoneRenderer != null) stoneRenderer.gameObject.SetActive(false);

        if (bgRenderer != null)
        {
            bgRenderer.color = new Color(0.40f, 0.25f, 0.45f, 1f); 
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