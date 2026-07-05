using UnityEngine;
using UnityEngine.EventSystems;
using BrokenAnchor.Core;

namespace BrokenAnchor.Pieces
{
    [RequireComponent(typeof(AudioSource))]
    public class PieceInteractionAudio : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private bool onlyLeftMouseButton = true;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (onlyLeftMouseButton && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            PlayOnce();
        }

        public void PlayOnce()
        {
            if (clip == null)
            {
                return;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            audioSource.PlayOneShot(clip, volume * AudioSettingsController.VoiceVolume);
        }
    }
}
