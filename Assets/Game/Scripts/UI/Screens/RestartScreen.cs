using Managers;

namespace Screens
{
    public class RestartScreen : BaseScreen
    {
        public void OnRestartGameBtnClick()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RestartGame();
        }
    }
}
