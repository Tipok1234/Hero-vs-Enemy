using System.Collections.Generic;
using UnityEngine;
using Screens;

namespace Managers
{
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private List<BaseScreen> screensPrefab = new List<BaseScreen>();
        [SerializeField] private List<BaseScreen> screens = new List<BaseScreen>();

        [SerializeField] private Camera camera;
        protected override void Awake()
        {
            base.Awake();
            SetupScreen();
        }

        public void ShowGame(PlayerHealth player)
        {
            CloseScreen<RestartScreen>();
            OpenScreen<GameScreen>();
            var gameScreen = GetScreen<GameScreen>();
            if (gameScreen != null) gameScreen.Setup(player);
        }

        public void ShowRestart()
        {
            CloseScreen<GameScreen>();
            OpenScreen<RestartScreen>();
        }

        public void OpenScreen<T>() where T : BaseScreen
        {
            T screen = GetScreen<T>();

            if (screen != null)
            {
                screen.OpenScreen();
            }
            else
            {
                Debug.LogError($"Screen of type {typeof(T)} is not registered.");
            }
        }

        public void CloseScreen<T>() where T : BaseScreen
        {
            T screen = GetScreen<T>();

            if (screen != null)
            {
                screen.CloseScreen();
            }
            else
            {
                Debug.LogError($"Screen of type {typeof(T)} is not registered.");
            }
        }

        public T GetScreen<T>() where T : BaseScreen
        {
            foreach (var screen in screens)
            {
                if (screen is T)
                {
                    return (T)screen;
                }
            }

            return null;
        }
        
        private void SetupScreen()
        {
            for (int i = 0; i < screensPrefab.Count; i++)
            {
                var addScreen = Instantiate(screensPrefab[i], gameObject.transform);
                addScreen.SetCamera(camera);
                addScreen.CloseScreen();
                screens.Add(addScreen);
            }
        }
    }
}
