using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>Hover and click sounds for a UI button.</summary>
    [RequireComponent(typeof(Selectable))]
    public class ButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private AudioClip hoverSfx;
        [SerializeField] private AudioClip clickSfx;

        private Selectable selectable;

        private void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable.IsInteractable()) SfxPlayer.Play(hoverSfx, 0.4f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (selectable.IsInteractable()) SfxPlayer.Play(clickSfx, 0.8f);
        }
    }
}
