using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public class MBFarmTempIcon : MonoBehaviour
    {
        public Transform HideNode;
        public Item item;
        public MBItemView view;
        public float speed;

        public void Init(Item collectItem, Transform hideNode, RectTransform parent)
        {
            HideNode = hideNode;
            this.item = collectItem;

            // 设置自己的位置为被收集前的位置
            transform.SetParent(parent, false);

            // 找到被收集的ItemView 置为自己的子节点
            view = BoardViewManager.Instance.boardView.boardHolder.FindItemView(item.id);
            view.transform.SetParent(transform, false);
            transform.localPosition = BoardUtility.GetPosByCoord(item.coord.x, item.coord.y);
        }

        public void SetSpeed(float speed)
        {
            this.speed = speed;
        }

        private void Update()
        {
            if (view == null) return;
            if (transform.localPosition.y < -136 * 6f) // 对应为最下边一行的y坐标
            {
                UIFlyUtility.FlyCustom(item.tid, 1, transform.position,
                    UIFlyFactory.ResolveFlyTarget(FlyType.MergeItemFlyTarget),
                    FlyStyle.Common,
                    FlyType.MergeItemFlyTarget, () => { }, size: (transform as RectTransform).sizeDelta.x);
                transform.SetParent(HideNode, false);
                BoardViewManager.Instance.boardView.boardHolder.ReleaseItem(item.id);
                view = null;
                speed = 0f;
            }
            else
            {
                transform.localPosition -= new Vector3(0, speed * Time.deltaTime, 0);
            }
        }
    }
}