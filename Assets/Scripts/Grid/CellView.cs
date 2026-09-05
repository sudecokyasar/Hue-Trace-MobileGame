using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CellView : MonoBehaviour
{
    [Header("Görsel Bileþenler")]
    [SerializeField] private SpriteRenderer bgRenderer;
    [SerializeField] private SpriteRenderer stoneRenderer;

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

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    public void Initialize(Vector2Int pos, float cellSize)
    {
        GridPos = pos;
        gameObject.name = $"Cell_{pos.x}_{pos.y}";

        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(cellSize, cellSize);

        if (stoneRenderer != null)
        {
            stoneRenderer.gameObject.SetActive(false);
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

        if (bgRenderer != null)
        {
            bgRenderer.color = new Color(0.25f, 0.65f, 0.85f, 1f); // Açýk buz mavisi
        }
    }

    public void CollapseIce()
    {
        if (!IsIce) return;

        IsCollapsed = true;

        if (bgRenderer != null)
        {
            bgRenderer.color = new Color(0.12f, 0.14f, 0.18f, 1f); // Çökmüþ koyu renk
        }
    }

    public void SetBridge()
    {
        IsBridge = true;

        if (bgRenderer != null)
        {
            bgRenderer.color = new Color(0.65f, 0.75f, 0.90f, 1f); // Parlak gümüþ/metalik
        }
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