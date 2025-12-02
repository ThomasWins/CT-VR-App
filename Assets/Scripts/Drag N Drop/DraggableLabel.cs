using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI; // LayoutRebuilder

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DraggableLabel : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Canvas that contains this UI.")]
    public Canvas canvas;

    [Header("Drag Proxy (3D)")]
    public TMP_FontAsset proxyFont;
    public Color proxyColor = Color.white;
    public float proxyFontSize = 16f;
    public float proxyPixelHeight = 20f;

    [Header("Dropped Label Font (only used if you reparent the UI)")]
    public float finalFontSize = 18f;

    // --- runtime refs/state ---
    RectTransform _rect;
    CanvasGroup _group;
    TMP_Text _tmp;

    // TRUE defaults (spawn-time home). Never overwrite these.
    Transform _defaultParent;
    Vector2 _defaultAnchoredPos;

    // Per-drag session (only used if you ever want a “cancel to previous”).
    Transform _sessionParent;
    Vector2 _sessionAnchoredPos;

    WorldLabelProxy _proxy;
    Plane _dragPlane;
    Vector3 _lastWorldPos;

    bool _snapped;               // set true when a slot consumes or SnapTo succeeds
    DropSlot _hoverSlot;         // slot currently highlighted while dragging

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _group = GetComponent<CanvasGroup>();
        _tmp = GetComponent<TMP_Text>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (_tmp) _tmp.color = Color.white;
    }

    // Cache spawn-time default parent/position
    void Start()
    {
        _defaultParent = transform.parent;
        _defaultAnchoredPos = _rect.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        _snapped = false;
        _hoverSlot = null;

        // record the session start (not used for reset-to-default, but handy if needed)
        _sessionParent = transform.parent;
        _sessionAnchoredPos = _rect.anchoredPosition;

        // make UI passthrough while dragging
        _group.blocksRaycasts = false;
        _group.alpha = 0.3f;

        // spawn world proxy that follows the XR/mouse ray
        _proxy = new GameObject("[WorldLabelProxy]").AddComponent<WorldLabelProxy>();
        var startPos = GetWorldPoint(e, out bool ok);
        if (!ok) startPos = canvas.transform.position + canvas.transform.forward * 0.1f;
        _proxy.transform.position = startPos;
        _lastWorldPos = startPos;

        var cam = EventCamera();
        // Init(string, Camera, TMP_FontAsset, Color, float, float)
        _proxy.Init(
            _tmp ? _tmp.text : name,
            cam,
            proxyFont,
            proxyColor,
            proxyFontSize,
            proxyPixelHeight
        );

        _dragPlane = new Plane(canvas.transform.forward, canvas.transform.position);
    }

    public void OnDrag(PointerEventData e)
    {
        // move proxy
        if (_proxy)
        {
            var pos = GetWorldPoint(e, out bool ok);
            if (!ok) pos = _lastWorldPos;
            _proxy.transform.position = pos;
            _lastWorldPos = pos;
        }

        // highlight slot under pointer/ray
        var slot = GetSlotFromEvent(e);
        if (slot != _hoverSlot)
        {
            if (_hoverSlot) _hoverSlot.HideBorder();
            _hoverSlot = slot;
            if (_hoverSlot) _hoverSlot.ShowHover();
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        // Kill the 3D proxy (if any)
        if (_proxy) { Destroy(_proxy.gameObject); _proxy = null; }

        _group.blocksRaycasts = true;
        _group.alpha = 1f;

        if (_hoverSlot) { _hoverSlot.HideBorder(); _hoverSlot = null; }

        // If a DropSlot already consumed us (via OnDrop → ConsumeIntoSlot), do nothing.
        if (_snapped)
        {
            _snapped = false;
            return;
        }

        // Try to find a target slot under the release point (XR-first)
        DropSlot slot = GetSlotFromEvent(e);

        if (slot != null)
        {
            // Snap into that slot using the classic UI path
            SnapTo(slot.transform, slot.fitMode, slot.padding, slot.filledLook);
            _snapped = true;
            return;
        }

        // No valid slot → reset back to TRUE DEFAULT location (spawn home)
        ResetToDefault();
    }

    /// <summary>Called by a DropSlot when it consumes this label and shows its own FilledLook.</summary>
    public void ConsumeIntoSlot(DropSlot slot)
    {
        if (_proxy) { Destroy(_proxy.gameObject); _proxy = null; }

        _group.blocksRaycasts = false;
        _group.alpha = 0f;

        // park the label at its TRUE default home and hide it
        transform.SetParent(_defaultParent, true);
        _rect.anchoredPosition = _defaultAnchoredPos;

        gameObject.SetActive(false);
        _snapped = true;
    }

    /// <summary>Optional classic reparent path (kept for flexibility).</summary>
    public void SnapTo(Transform newParent, SlotFitMode fitMode, Vector4 padding, TMP_Text tmp = null)
    {
        transform.SetParent(newParent, false);

        var r = (RectTransform)transform;
        var p = (RectTransform)newParent;

        r.localScale = Vector3.one;
        r.localRotation = Quaternion.identity;

        switch (fitMode)
        {
            case SlotFitMode.MatchParent:
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                r.pivot = new Vector2(0.5f, 0.5f);
                r.offsetMin = new Vector2(padding.x, padding.w);
                r.offsetMax = new Vector2(-padding.z, -padding.y);
                r.anchoredPosition = Vector2.zero;
                break;

            case SlotFitMode.MatchParentWidth:
                r.anchorMin = new Vector2(0f, 0.5f); r.anchorMax = new Vector2(1f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.offsetMin = new Vector2(padding.x, 0f);
                r.offsetMax = new Vector2(-padding.z, 0f);
                r.anchoredPosition = Vector2.zero;
                float desiredH = Mathf.Max(40f, p.rect.height - (padding.y + padding.w));
                r.sizeDelta = new Vector2(r.sizeDelta.x, desiredH);
                break;

            default:
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                break;
        }

        TMP_Text label = tmp ? tmp : _tmp;
        if (label)
        {
            label.enableAutoSizing = false;
            label.fontSize = finalFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Truncate;
            label.color = Color.white;
            label.maskable = false;
            label.raycastTarget = false;
            label.transform.SetAsLastSibling();
            var cr = label.canvasRenderer; if (cr) cr.cull = false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(p);
        _snapped = true;
    }

    /// <summary>Used by slots’ eject/reset to restore this label to its TRUE default position.</summary>
    public void RestoreToDefaultAndEnable()
    {
        gameObject.SetActive(true);
        transform.SetParent(_defaultParent, true);
        _rect.anchoredPosition = _defaultAnchoredPos;
        _group.blocksRaycasts = true;
        _group.alpha = 1f;
        if (_tmp) _tmp.color = Color.white;
    }

    /// <summary>Public reset helper (same as above, but doesn’t force visible).</summary>
    public void ResetToDefault()
    {
        transform.SetParent(_defaultParent, true);
        _rect.anchoredPosition = _defaultAnchoredPos;
        _rect.localRotation = Quaternion.identity;
        _rect.localScale = Vector3.one;
        if (_group) { _group.blocksRaycasts = true; _group.alpha = 1f; }
        if (_tmp) _tmp.color = Color.white;
    }

    // ----------------- helpers -----------------
    private DropSlot GetSlotFromEvent(PointerEventData e)
    {
        // Prefer XR/UI raycast result (works with XR Interaction Toolkit)
        var go = e.pointerCurrentRaycast.gameObject ?? e.pointerPressRaycast.gameObject;
        if (go)
        {
            var s = go.GetComponentInParent<DropSlot>();
            if (s && s.isActiveAndEnabled) return s;
        }

        // Fallback: screen-rect hit test for mouse/touch
        if (DropSlot.All != null && DropSlot.All.Count > 0)
        {
            Camera uiCam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : Camera.main;

            Vector2 pos = e.position;
            foreach (var s in DropSlot.All)
            {
                if (!s || !s.isActiveAndEnabled) continue;
                if (s.IsScreenPointOver(pos, uiCam)) return s;
            }
        }
        return null;
    }

    private Vector3 GetWorldPoint(PointerEventData e, out bool ok)
    {
        ok = false;
        var rp = e.pointerCurrentRaycast;
        if (rp.isValid && rp.worldPosition != Vector3.zero)
        { ok = true; return rp.worldPosition; }

        Camera cam = EventCamera();
        if (cam)
        {
            Ray ray = cam.ScreenPointToRay(e.position);
            if (_dragPlane.Raycast(ray, out float enter))
            { ok = true; return ray.GetPoint(enter); }
        }
        return Vector3.zero;
    }

    private Camera EventCamera()
    {
        return canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : Camera.main;
    }
}
