// ==================================================
// // File: MBTrainMissionProgressItem.cs
// // Author: liyueran
// // Date: 2025-07-29 15:07:45
// // Desc: $
// // ==================================================

using System.Collections.Generic;
using System.Linq;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBTrainMissionProgressItem : MonoBehaviour
    {
        public Animator animator;
        public UICommonItem item;
        public GameObject count;
        public Image check;
        private UITrainMissionProgressModule _root;
        private TrainMissionActivity _activity;
        private int _index;
        public bool punched;

        public void Init(TrainMissionActivity act, UITrainMissionProgressModule progressModule, int index)
        {
            this._root = progressModule;
            this._activity = act;
            this._index = index;
            item.gameObject.SetActive(ShowBox());

            var curLv = _activity.GetCurMilestoneProgress();

            punched = _index <= curLv;
            check.gameObject.SetActive(_index <= curLv && ShowBox());
            check.transform.localScale = _index <= curLv ? new Vector3(0.4f, 0.4f, 0.4f) : new Vector3(0f, 0.4f, 0.4f);
            count.SetActive(_index > curLv);
        }

        public void Punch()
        {
            if (punched)
            {
                return;
            }

            check.gameObject.SetActive(ShowBox());
            animator.SetTrigger("Punch");
            punched = true;
            count.SetActive(false);
        }

        private bool ShowBox()
        {
            List<TrainMilestone> list = new();
            _activity.GetTrainMilestones(list);

            var config = list.FirstOrDefault(x => x.MissionNum == _index);
            if (config == null)
            {
                return false;
            }

            var (id, count, _) = config.Reward.ConvertToInt3();
            item.Refresh(id, count);

            return true;
        }
    }
}