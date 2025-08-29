/**
 * @Author: zhangpengjian
 * @Date: 2025/6/19 17:28:09
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/6/19 17:28:09
 * Description: 许愿棋盘锁的表现
 */

using System.Collections.Generic;
using System.Linq;
using FAT.Merge;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBWishBoardLockView : MonoBehaviour
    {
        [SerializeField] private Button lockBtn;
        [SerializeField] private UIImageRes bubble;
        [SerializeField] private Animator lockAnim;

        public Transform Bubble => bubble.transform;

        public void SetUp()
        {
            lockBtn.onClick.AddListener(OnClickLock);
        }

        public void Init(int id)
        {
            var cfg = Env.Instance.GetItemConfig(id);
            bubble.SetImage(cfg.Icon);
        }

        public void OnClickLock()
        {
            var ui = UIManager.Instance.TryGetUI(UIConfig.UIWishBoardMain);
            if (ui != null && ui is UIWishBoardMain main)
            {
                var cloud = main.CloudHolder.GetNextCloud();
                List<(int, int)> coords = Enumerable.ToList(cloud.CloudArea);

                if (coords.Count == 0)
                {
                    return;
                }

                foreach (var coord in coords)
                {
                    if (main.CloudHolder.GetCloudViewByCoord(coord.Item1, coord.Item2, out var cloudView))
                    {
                        cloudView.PlayPunch();
                    }
                }
            }

            Game.Manager.commonTipsMan.ShowPopTips(Toast.FarmCloudLocked, transform.position);

            PlayHint();
        }

        public void PlayShow()
        {
            lockAnim.SetTrigger("Show");
        }

        public void PlayOpen()
        {
            // 播放音效 开锁
            Game.Manager.audioMan.TriggerSound("FarmboardUnlock");
            lockAnim.SetTrigger("Open");
        }

        public void PlayHint()
        {
            lockAnim.SetTrigger("Hint");
        }
    }
}