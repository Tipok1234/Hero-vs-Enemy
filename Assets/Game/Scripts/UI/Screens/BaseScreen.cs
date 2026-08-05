using UnityEngine;

namespace Screens
{
    public class BaseScreen : MonoBehaviour
    {
        [Header("Components")]
        public Canvas Canvas;

        public virtual void OpenScreen() => gameObject.SetActive(true);
        public virtual void CloseScreen() =>   gameObject.SetActive(false);
        public void SetCamera(Camera camera)
        {
            if (Canvas)
            {
                Canvas.worldCamera = camera;
            }
        }
    }
}
