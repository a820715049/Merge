/*
 * @Author: qun.chao
 * @Date: 2023-10-25 13:05:07
 */
using UnityEngine.EventSystems;

namespace FAT
{
    public class MBDimensionChangeChecker : UIBehaviour
    {
        public System.Action onDimensionsChange;
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            onDimensionsChange?.Invoke();
        }
    }
}