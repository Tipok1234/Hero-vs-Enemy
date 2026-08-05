using System.Collections;
using Managers;
using TMPro;
using UnityEngine;

namespace Views
{
    // Displays and animates wave changes reported by DifficultyController.
    [RequireComponent(typeof(TMP_Text))]
    public sealed class WaveView : MonoBehaviour
    {
        [SerializeField] private TMP_Text waveText;
        [SerializeField, Min(0f)] private float visibleDuration = 1.1f;
        [SerializeField, Min(0.01f)] private float animationDuration = 0.35f;
        [SerializeField, Min(1f)] private float overshootScale = 1.15f;

        private DifficultyController difficultyController;
        private Vector3 initialScale;
        private Color initialColor;
        private Coroutine animationRoutine;

        private void Awake()
        {
            if (waveText == null) waveText = GetComponent<TMP_Text>();
            initialScale = transform.localScale;
            initialColor = waveText != null ? waveText.color : Color.white;
        }

        private void OnEnable()
        {
            difficultyController = FindObjectOfType<DifficultyController>();
            if (difficultyController != null)
                difficultyController.WaveChanged += ShowWave;

            SetVisible(false);
        }

        private void OnDisable()
        {
            if (difficultyController != null)
                difficultyController.WaveChanged -= ShowWave;

            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        private void ShowWave(DifficultySnapshot difficulty)
        {
            if (waveText == null) return;

            waveText.SetText("WAVE {0}", difficulty.Wave);
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateWave());
        }

        private IEnumerator AnimateWave()
        {
            SetVisible(true);
            transform.localScale = initialScale * 0.7f;

            yield return AnimateScale(initialScale * overshootScale, animationDuration * 0.65f);
            yield return AnimateScale(initialScale, animationDuration * 0.35f);

            if (visibleDuration > 0f)
                yield return new WaitForSecondsRealtime(visibleDuration);

            var elapsed = 0f;
            var fadeDuration = animationDuration;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var color = initialColor;
                color.a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                waveText.color = color;
                yield return null;
            }

            SetVisible(false);
            animationRoutine = null;
        }

        private IEnumerator AnimateScale(Vector3 targetScale, float duration)
        {
            var startScale = transform.localScale;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedProgress);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        private void SetVisible(bool visible)
        {
            if (waveText == null) return;
            var color = initialColor;
            color.a = visible ? initialColor.a : 0f;
            waveText.color = color;
            if (!visible) transform.localScale = initialScale;
        }
    }
}
