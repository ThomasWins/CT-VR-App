using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DropAndDrop : MonoBehaviour
{
    [Header("Buttons")]
    public Button checkButton;
    public Button resetButton;

    [Header("Feedback (optional)")]
    public TMP_Text scoreLabel;

    public Color correctColor = new Color(0.15f, 0.65f, 0.25f);
    public Color incorrectColor = new Color(0.85f, 0.20f, 0.20f);
    public bool caseInsensitive = true;
    public bool normalizeWhitespace = true;

    private static readonly Regex _expectRx =
        new Regex(@"\((.*?)\)\s*$", RegexOptions.Compiled);

    void Start()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);

        if (checkButton != null)
            checkButton.onClick.AddListener(OnCheckButtonClicked);
    }

    private static string ExpectedFromSlotName(string slotName)
    {
        if (string.IsNullOrEmpty(slotName)) return "";
        var m = _expectRx.Match(slotName);
        return m.Success ? m.Groups[1].Value : "";
    }

    private string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = s;
        if (normalizeWhitespace)
            t = Regex.Replace(t, @"\s+", " ").Trim();
        return caseInsensitive ? t.ToLowerInvariant() : t;
    }

    private void TintLocked(TMP_Text label, bool correct)
    {
        if (!label) return;
        label.color = correct ? correctColor : incorrectColor;
    }

    void OnCheckButtonClicked()
    {
        Debug.Log("[DropAndDrop] Check button clicked.");

        // If any slot is empty, reset and exit early
        foreach (var slot in DropSlot.All)
        {
            if (!slot) continue;
            if (!slot.HasContent())
            {
                Debug.Log("[DropAndDrop] Found empty slot; resetting all.");
                DropSlot.ResetAllSlots();
                return;
            }
        }

        int total = 0, correctCount = 0;

        foreach (var slot in DropSlot.All)
        {
            if (!slot) continue;
            total++;

            var expectedRaw = ExpectedFromSlotName(slot.name);
            var expected = Normalize(expectedRaw);

            var gotRaw = slot.filledLook ? slot.filledLook.text : "";
            var got = Normalize(gotRaw);

            bool isCorrect = string.Equals(got, expected,
                caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            TintLocked(slot.filledLook, isCorrect);
            if (isCorrect) correctCount++;

            Debug.Log($"[DropAndDrop] {slot.name} -> got \"{gotRaw}\", expected \"{expectedRaw}\" :: {(isCorrect ? "CORRECT" : "WRONG")}");
        }

        if (scoreLabel)
            scoreLabel.text = $"{correctCount}/{total}";

        Debug.Log($"[DropAndDrop] Score: {correctCount}/{total}");

        // Restart (reset) after 3 seconds
        Invoke(nameof(OnResetButtonClicked), 3f);
    }

    void OnResetButtonClicked()
    {
        Debug.Log("[DropAndDrop] Resetting all DropSlots...");
        DropSlot.ResetAllSlots();

        if (scoreLabel)
            scoreLabel.text = "";

        foreach (var slot in DropSlot.All)
        {
            if (!slot || !slot.filledLook) continue;
            slot.filledLook.color = Color.white;
        }
    }
}
