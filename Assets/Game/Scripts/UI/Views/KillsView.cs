using System.Collections;
using TMPro;
using UnityEngine;

namespace Views
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class KillsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text killsText;
        [SerializeField, Min(1f)] private float punchScale = 1.22f;
        [SerializeField, Min(0.01f)] private float punchDuration = 0.28f;
        [SerializeField] private Color punchColor = new(1f, 0.82f, 0.15f, 1f);

        private int kills;
        private Vector3 initialScale;
        private Color initialColor;
        private Coroutine punchAnimation;

        private void Awake()
        {
            if (killsText == null) killsText = GetComponent<TMP_Text>();
            initialScale = transform.localScale;
            initialColor = killsText != null ? killsText.color : Color.white;
        }

        private void OnEnable()
        {
            kills = 0;
            UpdateText();
            EnemyHealth.EnemyDied += OnEnemyDied;
        }

        private void OnDisable()
        {
            EnemyHealth.EnemyDied -= OnEnemyDied;
            if (punchAnimation != null) StopCoroutine(punchAnimation);
            punchAnimation = null;
            transform.localScale = initialScale;
            if (killsText != null) killsText.color = initialColor;
        }

        private void OnEnemyDied(EnemyHealth enemy)
        {
            kills++;
            UpdateText();
            PlayPunchAnimation();
        }

        private void UpdateText()
        {
            if (killsText != null) killsText.SetText("Kills: {0}", kills);
        }

        private void PlayPunchAnimation()
        {
            if (punchAnimation != null) StopCoroutine(punchAnimation);
            punchAnimation = StartCoroutine(AnimatePunch());
        }

        private IEnumerator AnimatePunch()
        {
            var halfDuration = punchDuration * 0.5f;
            yield return AnimateTo(initialScale * punchScale, punchColor, halfDuration);
            yield return AnimateTo(initialScale, initialColor, halfDuration);
            punchAnimation = null;
        }

        private IEnumerator AnimateTo(Vector3 targetScale, Color targetColor, float duration)
        {
            var startScale = transform.localScale;
            var startColor = killsText != null ? killsText.color : initialColor;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedProgress);
                if (killsText != null)
                    killsText.color = Color.LerpUnclamped(startColor, targetColor, easedProgress);
                yield return null;
            }

            transform.localScale = targetScale;
            if (killsText != null) killsText.color = targetColor;
        }
    }
}
