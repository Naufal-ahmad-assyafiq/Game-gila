namespace FpsHorrorKit
{
    using System.Collections;
    using UnityEngine;

    /// <summary>Replaceable model and audio, without changing player or camera settings.</summary>
    public sealed class GhostAppearance : MonoBehaviour
    {
        [Header("Replaceable assets")]
        public GameObject ghostModel;
        public AudioSource audioSource;
        public AudioClip scareClip;

        [Header("Appearance")]
        [Min(0.1f)] public float duration = 1.2f;
        [Min(0.01f)] public float approachTime = 0.2f;
        [Min(0.5f)] public float startDistance = 2.8f;
        [Min(0.5f)] public float endDistance = 1.3f;
        public float verticalOffset = -0.85f;
        [Range(0f, 1f)] public float soundVolume = 0.35f;
        public bool IsAppearing { get; private set; }

        private void Awake()
        {
            if (ghostModel != null) { ghostModel.SetActive(false); }
        }

        public bool TryAppear(Camera camera)
        {
            if (IsAppearing) { return false; }
            if (!isActiveAndEnabled || ghostModel == null || camera == null)
            {
                Debug.LogWarning("GhostAppearance needs an active component, model and camera.", this);
                return false;
            }
            IsAppearing = true;
            StartCoroutine(Appear(camera));
            return true;
        }

        private IEnumerator Appear(Camera camera)
        {
            PositionInView(camera, startDistance);
            ghostModel.SetActive(true);
            if (audioSource != null && scareClip != null)
            {
                audioSource.PlayOneShot(scareClip, soundVolume);
            }

            float elapsed = 0f;
            while (elapsed < Mathf.Max(0.1f, duration) && camera != null && ghostModel != null)
            {
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, approachTime));
                PositionInView(camera, Mathf.Lerp(startDistance, endDistance, t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (ghostModel != null) { ghostModel.SetActive(false); }
            IsAppearing = false;
        }

        private void PositionInView(Camera camera, float distance)
        {
            Transform view = camera.transform;
            transform.SetPositionAndRotation(
                view.position + view.forward * Mathf.Max(0.5f, distance) + view.up * verticalOffset,
                view.rotation);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (ghostModel != null) { ghostModel.SetActive(false); }
            if (audioSource != null) { audioSource.Stop(); }
            IsAppearing = false;
        }
    }
}
