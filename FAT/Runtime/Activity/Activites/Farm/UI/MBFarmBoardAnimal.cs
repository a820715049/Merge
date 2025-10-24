// ==================================================
// File: MBFarmBoardAnimal.cs
// Author: liyueran
// Date: 2025-05-09 17:05:22
// Desc: 农场主棋盘动物
// ==================================================

using EL;
using FAT.Merge;
using fat.rawdata;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBFarmBoardAnimal : MonoBehaviour
    {
        public enum AnimalState
        {
            Idle = 0,
            Cd = 1,
            Reward = 2,
        }

        [SerializeField] protected Button animalBtn;
        public RectTransform bubbleTrans;
        [SerializeField] protected RectTransform cdTrans;
        public AnimalState CurAnimalState { get; set; } = AnimalState.Idle;

        // 活动实例
        protected FarmBoardActivity _activity;

        public virtual void SetUp()
        {
            animalBtn.onClick.AddListener(OnClickAnimal);
        }

        public virtual void InitOnPreOpen(FarmBoardActivity act)
        {
            _activity = act;
            InitAnimalState();
        }

        protected virtual void InitAnimalState()
        {
        }


        public virtual void OnDragItemEndCustom(Item item)
        {
            if (_activity == null)
            {
                return;
            }

            if (!_activity.TryConsumeItem(item))
            {
                return;
            }
        }

        protected virtual void OnClickAnimal()
        {
            var ui = (UIFarmBoardMain)UIManager.Instance.TryGetUI(_activity.VisualBoard.res.ActiveR);
            if (ui != null)
            {
                ui.AutoGuideController.Interrupt();
            }
        }
    }
}