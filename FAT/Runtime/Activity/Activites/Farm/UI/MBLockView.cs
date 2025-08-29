// ================================================
// File: MBLockView.cs
// Author: yueran.li
// Date: 2025/05/07 10:32:12 星期三
// Desc: 锁的表现
// ================================================

using System.Collections.Generic;
using System.Linq;
using FAT.Merge;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBLockView : MonoBehaviour
    {
        // [SerializeField] private UIImageRes bg;
        [SerializeField] private Button lockBtn;
        [SerializeField] private UIImageRes _bubble;
        [SerializeField] private Animator lockAnim;
        public UIImageRes Bubble => _bubble;

        public void SetUp()
        {
            lockBtn.onClick.AddListener(OnClickLock);
        }

        public void Init(int id)
        {
            var cfg = Env.Instance.GetItemConfig(id);
            _bubble.SetImage(cfg.Icon);
        }

        public void OnClickLock()
        {
            var ui = UIManager.Instance.TryGetUI(UIConfig.UIFarmBoardMain);
            if (ui != null && ui is UIFarmBoardMain main)
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

        #region Anim
        private void Play(string key)
        {
            lockAnim.SetTrigger($"{key}");
        }

        public void PlayShow()
        {
            Play("Show");
        }

        public void PlayOpen()
        {
            // 播放音效 开锁
            Game.Manager.audioMan.TriggerSound("FarmboardUnlock");
            Play("Open");
        }

        public void PlayHint()
        {
            Play("Hint");
        }
        #endregion
    }
}