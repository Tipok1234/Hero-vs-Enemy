using DG.Tweening;
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

        private DifficultyController difficultyController;
        private Vector3 initialScale;
        private Color initialColor;
        private Sequence animationSequence;

        private void Awake()
        {
            if (waveText == null) waveText = GetComponent<TMP_Text>();
            initialScale = Vector3.one;
            initialColor = waveText != null ? waveText.color : Color.white;
        }

        private void OnEnable()
        {
            difficultyController = FindObjectOfType<DifficultyController>();
            if (difficultyController != null)
                difficultyController.WaveChanged += ShowWave;

            SetVisible(false);
            if (difficultyController != null && difficultyController.Current.Wave > 0)
                ShowWave(difficultyController.Current);
        }

        private void OnDisable()
        {
            if (difficultyController != null)
                difficultyController.WaveChanged -= ShowWave;

            animationSequence?.Kill();
            animationSequence = null;
        }

        private void ShowWave(DifficultySnapshot difficulty)
        {
            if (waveText == null) return;

            waveText.SetText("WAVE {0}", difficulty.Wave);
            animationSequence?.Kill();
            SetVisible(true);
            transform.localScale = Vector3.zero;

            animationSequence = DOTween.Sequence()
                .SetUpdate(true)
                .Append(transform.DOScale(
                        initialScale,
                        animationDuration)
                    .SetEase(Ease.OutBack))
                .AppendInterval(visibleDuration)
                .Append(waveText.DOFade(0f, animationDuration)
                    .SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    SetVisible(false);
                    animationSequence = null;
                });
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
