using System.Collections;
using Managers;
using TMPro;
using UnityEngine;

namespace Views
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TimerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text timerText;
        [SerializeField, Min(1f)] private float tickScale = 1.08f;
        [SerializeField, Min(0.01f)] private float tickAnimationDuration = 0.18f;

        private float elapsedTime;
        private int displayedSecond = -1;
        private Vector3 initialScale;
        private Coroutine tickAnimation;

        private void Awake()
        {
            if (timerText == null) timerText = GetComponent<TMP_Text>();
            initialScale = transform.localScale;
        }

        private void OnEnable()
        {
            ResetTimer();
        }

        private void Update()
        {
            if (GameManager.Instance == null ||
                GameManager.Instance.State != GameManager.GameState.Playing) return;

            elapsedTime += Time.deltaTime;
            var currentSecond = Mathf.FloorToInt(elapsedTime);
            if (currentSecond == displayedSecond) return;

            displayedSecond = currentSecond;
            UpdateText(currentSecond);
            PlayTickAnimation();
        }

        private void ResetTimer()
        {
            elapsedTime = 0f;
            displayedSecond = 0;
            UpdateText(0);
        }

        private void UpdateText(int totalSeconds)
        {
            if (timerText == null) return;

            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void PlayTickAnimation()
        {
            if (tickAnimation != null) StopCoroutine(tickAnimation);
            tickAnimation = StartCoroutine(AnimateTick());
        }

        private IEnumerator AnimateTick()
        {
            var halfDuration = tickAnimationDuration * 0.5f;
            yield return ScaleTo(initialScale * tickScale, halfDuration);
            yield return ScaleTo(initialScale, halfDuration);
            tickAnimation = null;
        }

        private IEnumerator ScaleTo(Vector3 targetScale, float duration)
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

        private void OnDisable()
        {
            if (tickAnimation != null) StopCoroutine(tickAnimation);
            tickAnimation = null;
            transform.localScale = initialScale;
        }
    }
}
