using UnityEngine;
using TMPro;
using DataUtils;
using DG.Tweening;
using System.Globalization;

namespace Views
{
    public class CurrencyView : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private float animationTime;

        private int _currentCoin;

        private void Start()
        {
            GameSaves.Instance.ChangedCurrencyAction += UpdateCoin;
        }

        private void OnDestroy()
        {
            GameSaves.Instance.ChangedCurrencyAction -= UpdateCoin;
        }

        private void OnEnable()
        {
            _currentCoin = GameSaves.Instance.GetCoin();
            UpdateCoin();
        }

        private void UpdateCoin()
        {
            int newCoin = GameSaves.Instance.GetCoin();
            
            AnimateCoinText(_currentCoin, newCoin);
            _currentCoin = newCoin;
        }

        private void AnimateCoinText(int fromValue, int toValue)
        {
            if (coinText)
            {
                DOTween.To(() => fromValue, x => { coinText.text = FormatNumberWithSpaces(x); }, toValue, animationTime)
                    .SetEase(Ease.OutQuad);
            }
        }

        private string FormatNumberWithSpaces(int number)
        {
            return number.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");  
        }
    }
}