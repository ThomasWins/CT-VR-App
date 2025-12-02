using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Controls the visual states of a boolean toggle switch UI
    /// Knob positions are based dynamically on the Hit Target edges.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class BooleanToggleVisualsController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
#pragma warning disable 649
        [SerializeField, Tooltip("The boolean toggle knob.")]
        RectTransform m_Knob;

        [SerializeField, Tooltip("The hit target that defines the toggle range.")]
        RectTransform m_HitTarget;

        [SerializeField, Tooltip("How much to translate the knob on Z when hovering.")]
        float m_ZTranslation = 5f;
#pragma warning restore 649

        Toggle m_Toggle;
        float m_InitialZ;

        void Awake()
        {
            m_Toggle = GetComponent<Toggle>();

            // Listen to toggle value changes
            m_Toggle.onValueChanged.AddListener(ToggleValueChanged);

            if (m_Knob != null)
                m_InitialZ = m_Knob.localPosition.z;
        }

        void OnEnable()
        {
            ToggleValueChanged(m_Toggle.isOn);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            PerformEntranceActions();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            PerformExitActions();
        }

        void ToggleValueChanged(bool value)
        {
            if (m_Knob == null || m_HitTarget == null) return;

            float halfHitWidth = m_HitTarget.rect.width / 2f;
            float halfKnobWidth = m_Knob.rect.width / 2f;

            float targetX = value
                ? halfHitWidth - halfKnobWidth  // ON position (right edge)
                : -halfHitWidth + halfKnobWidth; // OFF position (left edge)

            m_Knob.localPosition = new Vector3(targetX, m_Knob.localPosition.y, m_Knob.localPosition.z);
        }

        void PerformEntranceActions()
        {
            if (m_Knob != null)
            {
                var pos = m_Knob.localPosition;
                pos.z = m_InitialZ - m_ZTranslation;
                m_Knob.localPosition = pos;
            }
        }

        void PerformExitActions()
        {
            if (m_Knob != null)
            {
                var pos = m_Knob.localPosition;
                pos.z = m_InitialZ;
                m_Knob.localPosition = pos;
                m_Knob.localScale = Vector3.one;
            }
        }
    }
}
