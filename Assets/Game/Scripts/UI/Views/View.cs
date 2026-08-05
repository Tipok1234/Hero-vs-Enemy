using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Views
{
    public abstract class View<TData> : MonoBehaviour
    {
        public abstract void Setup(TData data);
        public abstract void Refresh();
        
        protected void ShowView() => gameObject.SetActive(true);
        protected void HideView() => gameObject.SetActive(false);
    }
}
