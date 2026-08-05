using System.Collections.Generic;
using UnityEngine;
using Views;

namespace Screens
{
    public class ViewScreen<TView, TData> : BaseScreen where TView : View<TData> 
    {
        [Header("UI Elements")]
        [SerializeField] private RectTransform content;
        [SerializeField] private TView viewPrefab;

        private readonly List<TView> _views = new();

        protected void Setup(List<TData> dataList)
        {
            if (_views.Count == 0)
            {
                InitViews(dataList);
            }
            else
            {
                RefreshViews();
            }
        }

        protected void ClearList()
        {
            if (_views.Count > 0)
            {
                foreach (var view in _views)
                {
                    Destroy(view.gameObject);
                }

                _views.Clear();
            }
        }
        protected virtual void OnItemBought(TData data)
        {
            RefreshViews();
        }

        private void InitViews(List<TData> dataList)
        {
            foreach (var data in dataList)
            {
                var view = Instantiate(viewPrefab, content);
                view.Setup(data);
                
                _views.Add(view);
            }
        }
        
        private void RefreshViews()
        {
            foreach (var view in _views)
            {
                view.Refresh();
            }
        }
    }
}
