using UnityEngine;
using FAT.Merge;
namespace FAT
{
    public class MBMineTempIcon : MonoBehaviour
    {
        public Transform HideNode;
        public Item item;
        public MBItemView view;
        public float speed;
        public void SetImage(Item item, Transform hideNode)
        {
            HideNode = hideNode;
            this.item = item;
            //var icon = transform.GetComponent<UIImageRes>();
            //icon.SetImage(Game.Manager.objectMan.GetBasicConfig(item.tid).Icon);
            view = BoardViewManager.Instance.boardView.boardHolder.FindItemView(item.id);
            view.transform.SetParent(transform, false);
            transform.localPosition = BoardUtility.GetPosByCoord(item.coord.x, item.coord.y);
        }

        public void SetSpeed(float speed)
        {
            this.speed = speed;
        }

        public void Update()
        {
            if (view == null) return;
            if (transform.localPosition.y > -68f)
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
                transform.localPosition += new Vector3(0, speed * Time.deltaTime, 0);
            }
        }
    }
}
