using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum SlotFitMode { KeepSize, MatchParent, MatchParentWidth, CenterNoStretch }

[RequireComponent(typeof(RectTransform))]
public class DropSlot : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static readonly List<DropSlot> All = new List<DropSlot>();

    [Header("Behavior")]
    public bool singleOccupancy = true;
    public SlotFitMode fitMode = SlotFitMode.MatchParentWidth;
    public Vector4 padding = new Vector4(8, 8, 8, 8);

    [Header("Slot Display (built-in text)")]
    public TMP_Text filledLook; // child named "FilledLook" preferred

    [Header("Border")]
    public float borderThickness = 3f;
    public float borderInset = 2f;
    public Color hoverColor = new Color(0.2f, 0.7f, 1f, 1f);
    public Color selectedColor = new Color(1f, 0.6f, 0.2f, 1f);

    [Header("3D Drag of Filled Slot")]
    public TMP_FontAsset slotDragFont;
    public float slotDragFontSize = 24f;
    public float slotDragPixelHeight = 24f;
    public Color slotDragColor = Color.white;

    // runtime
    RectTransform _rect;
    RectTransform _borderRoot;
    Image _top, _bottom, _left, _right;
    bool _selectedOn;

    DraggableLabel _heldLabel;
    static DropSlot _selectedForMove;

    bool _draggingFromSlot;
    WorldLabelProxy _slotProxy;
    Plane _dragPlane;
    Vector3 _lastWorldPos;
    DropSlot _hoverWhileSlotDrag;
    string _dragTextCache;
    DraggableLabel _dragHeldLabel;
    Canvas _cachedCanvas;

    void Awake()
    {
        _rect = (RectTransform)transform;
        _cachedCanvas = GetComponentInParent<Canvas>();
        AutoWireFilledLook();
        EnsureBorder();
        HideBorder();
    }
    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); if (_selectedForMove == this) _selectedForMove = null; }

    void AutoWireFilledLook()
    {
        if (filledLook) return;
        var child = transform.Find("FilledLook");
        if (child) filledLook = child.GetComponent<TMP_Text>();
        if (!filledLook) filledLook = GetComponentInChildren<TMP_Text>(true);
        if (!filledLook) { Debug.LogWarning($"[DropSlot] No FilledLook on {name}"); return; }
        filledLook.gameObject.SetActive(false);
        filledLook.color = Color.white;
        filledLook.enableAutoSizing = false;
        filledLook.alignment = TextAlignmentOptions.Center;
        filledLook.enableWordWrapping = false;
        filledLook.overflowMode = TextOverflowModes.Truncate;
        var cr = filledLook.canvasRenderer; if (cr) cr.cull = false;
    }

    void EnsureBorder()
    {
        if (_borderRoot) return;
        _borderRoot = new GameObject("HoverBorder", typeof(RectTransform)).GetComponent<RectTransform>();
        _borderRoot.SetParent(transform, false);
        _borderRoot.anchorMin = Vector2.zero;
        _borderRoot.anchorMax = Vector2.one;
        _borderRoot.offsetMin = Vector2.zero;
        _borderRoot.offsetMax = Vector2.zero;
        _borderRoot.pivot = new Vector2(0.5f, 0.5f);
        _borderRoot.SetAsLastSibling();

        _top = MakeEdge("Top");
        _bottom = MakeEdge("Bottom");
        _left = MakeEdge("Left");
        _right = MakeEdge("Right");

        LayoutBorder(hoverColor);
        _borderRoot.gameObject.SetActive(false);
    }
    Image MakeEdge(string n)
    {
        var rt = new GameObject(n, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rt.SetParent(_borderRoot, false);
        var img = rt.GetComponent<Image>(); img.raycastTarget = false;
        return img;
    }
    void LayoutBorder(Color c)
    {
        float t = Mathf.Max(1f, borderThickness);
        float inset = Mathf.Max(0f, borderInset);
        _top.color = _bottom.color = _left.color = _right.color = c;

        var rt = _top.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0, t);
        rt.anchoredPosition = new Vector2(0, -inset);

        rt = _bottom.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f); rt.sizeDelta = new Vector2(0, t);
        rt.anchoredPosition = new Vector2(0, inset);

        rt = _left.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f); rt.sizeDelta = new Vector2(t, 0);
        rt.anchoredPosition = new Vector2(inset, 0);

        rt = _right.rectTransform;
        rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f); rt.sizeDelta = new Vector2(t, 0);
        rt.anchoredPosition = new Vector2(-inset, 0);
    }
    public void ShowHover() { if (_selectedOn) return; _borderRoot.gameObject.SetActive(true); LayoutBorder(hoverColor); }
    public void ShowSelected() { _selectedOn = true; _borderRoot.gameObject.SetActive(true); LayoutBorder(selectedColor); }
    public void HideBorder() { _selectedOn = false; if (_borderRoot) _borderRoot.gameObject.SetActive(false); }

    public void OnPointerEnter(PointerEventData _) { ShowHover(); }
    public void OnPointerExit(PointerEventData _) { if (!_selectedOn) HideBorder(); }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
        {
            if (HasContent()) TryEjectToDefault();
            return;
        }

        if (e.button == PointerEventData.InputButton.Left)
        {
            if (_selectedForMove == null)
            {
                if (HasContent()) { _selectedForMove = this; ShowSelected(); }
            }
            else
            {
                if (_selectedForMove == this)
                {
                    _selectedForMove.HideBorder();
                    _selectedForMove = null; // cancel
                }
                else
                {
                    _selectedForMove.TransferTo(this);
                    _selectedForMove.HideBorder();
                    HideBorder();
                    _selectedForMove = null;
                }
            }
        }
    }

    // ---- slot → 3D drag
    public void OnBeginDrag(PointerEventData e)
    {
        if (!HasContent()) return;

        _draggingFromSlot = true;
        _hoverWhileSlotDrag = null;

        _dragTextCache = filledLook ? filledLook.text : "";
        _dragHeldLabel = _heldLabel;

        if (filledLook) filledLook.gameObject.SetActive(false);

        _slotProxy = new GameObject("[WorldLabelProxy_FromSlot]").AddComponent<WorldLabelProxy>();
        var startPos = GetWorldPoint(e, out bool ok);
        if (!ok)
        {
            var root = _cachedCanvas ? _cachedCanvas.transform : transform;
            startPos = root.position + root.forward * 0.1f;
        }
        _slotProxy.transform.position = startPos;
        _lastWorldPos = startPos;

        var cam = EventCamera();
        _slotProxy.Init(_dragTextCache, cam, slotDragFont, slotDragColor, slotDragFontSize, slotDragPixelHeight);

        _dragPlane = new Plane((_cachedCanvas ? _cachedCanvas.transform.forward : Vector3.forward),
                               (_cachedCanvas ? _cachedCanvas.transform.position : Vector3.zero));
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_draggingFromSlot || _slotProxy == null) return;

        var pos = GetWorldPoint(e, out bool ok);
        if (!ok) pos = _lastWorldPos;
        _slotProxy.transform.position = pos;
        _lastWorldPos = pos;

        var slot = GetSlotFromEvent(e);
        if (slot != _hoverWhileSlotDrag)
        {
            if (_hoverWhileSlotDrag) _hoverWhileSlotDrag.HideBorder();
            _hoverWhileSlotDrag = slot;
            if (_hoverWhileSlotDrag) _hoverWhileSlotDrag.ShowHover();
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!_draggingFromSlot) return;

        if (_slotProxy) { Object.Destroy(_slotProxy.gameObject); _slotProxy = null; }
        if (_hoverWhileSlotDrag) { _hoverWhileSlotDrag.HideBorder(); _hoverWhileSlotDrag = null; }

        // XR-friendly: get target from raycast first
        var target = GetSlotFromEvent(e);

        if (target == null || target == this)
        {
            RestoreLook(_dragTextCache);
        }
        else
        {
            TransferTo(target, _dragHeldLabel, _dragTextCache);
        }

        _dragTextCache = null;
        _dragHeldLabel = null;
        _draggingFromSlot = false;
    }

    // ---- label → slot
    public void OnDrop(PointerEventData e)
    {
        var go = e.pointerDrag;
        if (!go) return;

        var label = go.GetComponent<DraggableLabel>();
        var tmp = go.GetComponent<TMP_Text>();
        if (!label || !tmp) return;

        if (singleOccupancy && HasContent()) TryEjectToDefault();

        AutoWireFilledLook();
        if (filledLook)
        {
            filledLook.text = tmp.text;
            filledLook.color = Color.white;
            filledLook.gameObject.SetActive(true);
            filledLook.transform.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        label.ConsumeIntoSlot(this);
        _heldLabel = label;

        HideBorder();
    }

    // ---- transfers
    public void TransferTo(DropSlot target)
    {
        TransferTo(target, _heldLabel, filledLook ? filledLook.text : "");
    }

    void TransferTo(DropSlot target, DraggableLabel movingLabel, string movingText)
    {
        if (!target || target == this)
        {
            RestoreLook(movingText);
            return;
        }

        if (target.singleOccupancy && target.HasContent())
            target.TryEjectToDefault();

        target.AutoWireFilledLook();
        if (target.filledLook)
        {
            target.filledLook.text = movingText;
            target.filledLook.color = Color.white;
            target.filledLook.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)target.transform);
        }

        target._heldLabel = movingLabel;

        if (filledLook) { filledLook.text = ""; filledLook.gameObject.SetActive(false); }
        _heldLabel = null;
    }

    void RestoreLook(string text)
    {
        if (filledLook)
        {
            filledLook.text = text;
            filledLook.color = Color.white;
            filledLook.gameObject.SetActive(true);
        }
    }

    // ---- utils
    public bool HasContent()
        => filledLook && filledLook.gameObject.activeSelf && !string.IsNullOrEmpty(filledLook.text);

    public bool IsScreenPointOver(Vector2 screenPos, Camera eventCam)
        => RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPos, eventCam);

    public void Clear()
    {
        if (filledLook) { filledLook.text = ""; filledLook.gameObject.SetActive(false); }
        _heldLabel = null;
        HideBorder();
    }

    public bool TryEjectToDefault()
    {
        if (!HasContent() || _heldLabel == null) return false;

        var label = _heldLabel; _heldLabel = null;
        if (filledLook) { filledLook.text = ""; filledLook.gameObject.SetActive(false); }
        label.RestoreToDefaultAndEnable();
        HideBorder();
        return true;
    }

    public void ResetSlot()
    {
        TryEjectToDefault();
        Clear();
    }

    // XR-first target resolution
    DropSlot GetSlotFromEvent(PointerEventData e)
    {
        // prefer XR/UI raycast object
        var go = e.pointerCurrentRaycast.gameObject ?? e.pointerPressRaycast.gameObject;
        if (go)
        {
            var s = go.GetComponentInParent<DropSlot>();
            if (s && s.isActiveAndEnabled) return s;
        }

        // fallback: rectangle hit test for mouse
        if (All != null && All.Count > 0)
        {
            Camera uiCam = EventCamera();
            Vector2 screenPos = e.position;
            foreach (var s in All)
            {
                if (!s || !s.isActiveAndEnabled) continue;
                if (s.IsScreenPointOver(screenPos, uiCam)) return s;
            }
        }
        return null;
    }

    Vector3 GetWorldPoint(PointerEventData e, out bool ok)
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

    Camera EventCamera()
    {
        if (_cachedCanvas && _cachedCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return _cachedCanvas.worldCamera;
        return Camera.main;
    }

    // ---------- global reset ----------
    public static void ResetAllSlots()
    {
        foreach (var slot in All)
        {
            if (!slot) continue;
            slot.TryEjectToDefault(); // returns its held label to default
            slot.Clear();              // clears text and hides visuals
        }
        Debug.Log("[DropSlot] All slots reset.");
    }

}
