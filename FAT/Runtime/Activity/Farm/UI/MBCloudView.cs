// ================================================
// File: MBCloudView.cs
// Author: yueran.li
// Date: 2025/04/25 19:00:36 星期五
// Desc: 云的表现
// ================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EL;
using FAT.Merge;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class MBCloudView : MonoBehaviour
    {
        [SerializeField] private UIImageRes _cover;
        [SerializeField] private Image _mask;
        [SerializeField] private Animator cloudAnim;

        private readonly int _Hide = Animator.StringToHash("Hide");
        private readonly int _Punch = Animator.StringToHash("Punch");

        private Cloud belongCloud;
        private (int, int) coord;
        private MBBoardCloudHolder _cloudHolder;

        public void Init(Cloud cloud, int x, int y)
        {
            var res = BoardViewManager.Instance.boardView.BoardRes;
            _cover.SetImage(res.cloudSprite);
            transform.AddButton("cover", OnClickCover);
            this.belongCloud = cloud;
            coord = (x, y);

            if (UIManager.Instance.TryGetUI(UIConfig.UIFarmBoardMain) is UIFarmBoardMain main)
            {
                _cloudHolder = main.CloudHolder;
            }

            RefreshMask();
        }

        public void RefreshMask()
        {
            var act = Game.Manager.activity.LookupAny(EventType.FarmBoard) as FarmBoardActivity;
            if (act == null)
            {
                return;
            }

            var cloud = _cloudHolder.GetNextCloud();

            _mask.raycastTarget = belongCloud != cloud;
        }

        private void OnClickCover()
        {
            List<(int, int)> coords = Enumerable.ToList(belongCloud.CloudArea);
            foreach (var coord in coords)
            {
                _cloudHolder.GetCloudViewByCoord(coord.Item1, coord.Item2, out var view);
                view.PlayPunch();
            }

            var ui = UIManager.Instance.TryGetUI(UIConfig.UIFarmBoardMain);
            if (ui != null && ui is UIFarmBoardMain main)
            {
                main.LockView.PlayHint();
                Game.Manager.commonTipsMan.ShowPopTips(Toast.FarmCloudLocked, main.LockView.transform.position);
            }
        }

        // 暗->明表现
        public void PlayPunch()
        {
            cloudAnim.SetTrigger(_Punch);
        }

        // 解锁表现
        public IEnumerator CoPlayUnlock()
        {
            cloudAnim.SetTrigger(_Hide);
            yield return new WaitForSeconds(1.3f);
            _cloudHolder.ReleaseView(coord);
        }
    }
}