using UnityEngine;
using UnityEngine.UI;

namespace PooledScrollList.Controller
{
    public class PooledScrollRectController : PooledScrollRectBase
    {
        private LayoutElement _spaceElement;

        protected override void Awake()
        {
            base.Awake();

            var layoutGroup = ScrollRect.content.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                LayoutSpacing = layoutGroup.spacing;
                Padding = layoutGroup.padding;
                ElementSize += LayoutSpacing;
            }
            else
            {
                Debug.LogWarning("Failed to get HorizontalOrVerticalLayoutGroup assigned to ScrollRect's content. PooledScrollRectController won't work as expected.");
            }

            _spaceElement = CreateSpaceElement(ScrollRect, 0f);
            _spaceElement.ignoreLayout = true;
            _spaceElement.transform.SetParent(ScrollRect.content.transform, false);
            _spaceElement.transform.SetSiblingIndex(0);
        }

        protected override void UpdateContent()
        {
            AdjustContentSize(ElementSize * TotalElementsCount);

            var scrollAreaSize = ExternalViewPort != null ? GetScrollAreaSize(ExternalViewPort) : GetScrollAreaSize(ScrollRect.viewport);
            var elementsVisibleInScrollArea = Mathf.CeilToInt(scrollAreaSize / ElementSize);
            var elementsCulledAbove = Mathf.Clamp(Mathf.FloorToInt(GetScrollRectNormalizedPosition() * (TotalElementsCount - elementsVisibleInScrollArea)), 0,
                Mathf.Clamp(TotalElementsCount - (elementsVisibleInScrollArea + 1), 0, int.MaxValue));

            var requiredElementsInList = Mathf.Min(elementsVisibleInScrollArea + 1, TotalElementsCount);

            if (ActiveElements.Count != requiredElementsInList || LastElementsCulledAbove == -1 || Mathf.Abs(elementsCulledAbove - LastElementsCulledAbove) > 1)
            {
                InitializeElements(requiredElementsInList, elementsCulledAbove);
            }
            else if (LastElementsCulledAbove != elementsCulledAbove)
            {
                ReorientElement(elementsCulledAbove > LastElementsCulledAbove ? ReorientMethod.TopToBottom : ReorientMethod.BottomToTop, elementsCulledAbove);
            }

            AdjustSpaceElement(elementsCulledAbove * ElementSize);

            LastElementsCulledAbove = elementsCulledAbove;
        }

        protected override void AdjustSpaceElement(float size)
        {
            if (size <= 0)
            {
                _spaceElement.ignoreLayout = true;
                size = 0f;
            }
            else
            {
                _spaceElement.ignoreLayout = false;
                size -= LayoutSpacing;
            }

            if (ScrollRect.vertical)
            {
                _spaceElement.minHeight = size;
                _spaceElement.preferredHeight = size;
            }
            else
            {
                _spaceElement.minWidth = size;
                _spaceElement.preferredWidth = size;
            }

            _spaceElement.transform.SetSiblingIndex(0);
        }

        protected override void ReorientElement(ReorientMethod reorientMethod, int elementsCulledAbove)
        {
            if (ActiveElements.Count <= 1)
            {
                return;
            }

            if (reorientMethod == ReorientMethod.TopToBottom)
            {
                var top = ActiveElements[0];
                ActiveElements.RemoveAt(0);
                ActiveElements.Add(top);

                top.transform.SetSiblingIndex(ActiveElements[ActiveElements.Count - 2].transform.GetSiblingIndex() + 1);
                var dataIndex = elementsCulledAbove + ActiveElements.Count - 1;
                if (dataIndex >= 0 && dataIndex < Data.Count)
                {
                    top.Data = Data[dataIndex];
                }
            }
            else
            {
                var bottom = ActiveElements[ActiveElements.Count - 1];
                ActiveElements.RemoveAt(ActiveElements.Count - 1);
                ActiveElements.Insert(0, bottom);

                bottom.transform.SetSiblingIndex(ActiveElements[1].transform.GetSiblingIndex());
                var dataIndex = elementsCulledAbove;
                if (dataIndex >= 0 && dataIndex < Data.Count)
                {
                    bottom.Data = Data[dataIndex];
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_spaceElement != null)
            {
                Destroy(_spaceElement.gameObject);
            }
        }
    }
}