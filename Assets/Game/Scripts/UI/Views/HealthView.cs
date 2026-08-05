using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Views
{
    [RequireComponent(typeof(Slider))]
    public sealed class HealthView : View<IHealth>
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text healthText;

        private IHealth health;

        private void Awake()
        {
            if (healthSlider == null) 
                healthSlider = GetComponent<Slider>();
            
            
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
        }

        private void Start()
        {
            if (health != null) return;

            foreach (var behaviour in GetComponentsInParent<MonoBehaviour>(true))
            {
                if (!(behaviour is IHealth healthSource)) continue;
                Setup(healthSource);
                break;
            }
        }

        public override void Setup(IHealth data)
        {
            if (health != null)
                health.HealthChanged -= OnHealthChanged;

            health = data;
            if (health != null)
                health.HealthChanged += OnHealthChanged;

            Refresh();
        }

        public override void Refresh()
        {
            if (health == null)
            {
                healthSlider.value = 0f;
                if (healthText != null) 
                    healthText.text = "0/0";
                return;
            }

            OnHealthChanged(health.CurrentHealth, health.MaxHealth);
        }

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            healthSlider.value = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }

        private void OnDestroy()
        {
            if (health != null)
                health.HealthChanged -= OnHealthChanged;
        }
    }
}
