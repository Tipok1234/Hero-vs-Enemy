using UnityEngine;
using Views;

namespace Screens
{
    public class GameScreen : BaseScreen
    {
        [SerializeField] private HealthView mainCharacterHealthView;

        private void Start()
        {
            if (mainCharacterHealthView == null)
                mainCharacterHealthView = GetComponentInChildren<HealthView>(true);

            var playerHealth = FindObjectOfType<PlayerHealth>();
            if (mainCharacterHealthView == null || playerHealth == null)
            {
                Debug.LogWarning("GameScreen could not connect the main character health view.", this);
                return;
            }

            mainCharacterHealthView.Setup(playerHealth);
        }

        public void Setup(PlayerHealth playerHealth)
        {
            if (mainCharacterHealthView == null)
                mainCharacterHealthView = GetComponentInChildren<HealthView>(true);

            if (mainCharacterHealthView != null && playerHealth != null)
                mainCharacterHealthView.Setup(playerHealth);
        }
    }
}
