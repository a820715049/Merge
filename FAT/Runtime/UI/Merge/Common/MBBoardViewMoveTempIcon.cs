using UnityEngine;
using FAT.Merge;
namespace FAT
{
    public class MBBoardViewMoveTempIcon : MonoBehaviour
    {
        public Item item;
        public MBItemView view;
        private Vector3 initialWorldPosition = new Vector3(-9999, -9999, 0);
        private bool hasTriggeredCollect = false;
        private float collectTriggerDistance;
        private Vector3 triggerDirection;

        public void SetImage(Item item)
        {
            this.item = item;

            // 获取原始ItemView的图标信息，但不移动它
            view = BoardViewManager.Instance.boardView.boardHolder.FindItemView(item.id);
            if (view != null)
            {
                // 复制图标到当前临时图标
                var tempImage = GetComponent<UIImageRes>();
                if (tempImage != null)
                {
                    var itemConfig = Env.Instance.GetItemConfig(item.tid);
                    if (itemConfig != null)
                    {
                        tempImage.SetImage(itemConfig.Icon);
                    }
                }
            }
            transform.localPosition = BoardUtility.GetPosByCoord(item.coord.x, item.coord.y);
        }

        public void SetCollectTriggerDistance(float distance, Vector3 direction)
        {
            collectTriggerDistance = distance;
            triggerDirection = direction.normalized;
        }

        private void Update()
        {
            if (view == null || item == null || hasTriggeredCollect) return;

            if (initialWorldPosition == new Vector3(-9999, -9999, 0))
            {
                initialWorldPosition = transform.position;
                return;
            }

            // 计算当前世界坐标相对于初始位置的偏移向量
            Vector3 currentWorldPosition = transform.position;
            Vector3 offsetVector = currentWorldPosition - initialWorldPosition;

            // 将世界坐标的偏移向量转换为本地坐标的偏移向量
            // 这样可以避免父级scale对距离计算的影响
            Vector3 localOffsetVector = transform.parent != null ?
                transform.parent.InverseTransformVector(offsetVector) : offsetVector;

            // 计算在触发方向上的投影距离（使用本地坐标）
            float projectedDistance = Vector3.Dot(localOffsetVector, triggerDirection);

            // 当在触发方向上的投影距离达到触发距离时，触发收集逻辑
            if (projectedDistance >= collectTriggerDistance)
            {
                hasTriggeredCollect = true;
                TriggerFlyAnimation();
            }
        }

        public void TriggerFlyAnimation()
        {
            if (item == null) return;

            Destroy(gameObject);

            // 安全获取RectTransform
            RectTransform rectTransform = transform as RectTransform;
            float size = rectTransform != null ? rectTransform.sizeDelta.x : 50f; // 默认大小

            UIFlyUtility.FlyCustom(item.tid, 1, transform.position,
                UIFlyFactory.ResolveFlyTarget(FlyType.MergeItemFlyTarget),
                FlyStyle.Common,
                FlyType.MergeItemFlyTarget,
                null, size: size);
        }

        private void OnDestroy()
        {
            // 清理引用，防止内存泄漏
            item = null;
            view = null;
        }
    }
}
