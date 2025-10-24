using EL;
using UnityEngine;

namespace FAT
{
    [HelpURL("https://centurygames.feishu.cn/wiki/Hkp4wyHF7i1IBnkd9kecsjRenfc")]
    public class UISeaRaceBase : UIBase
    {
        protected ActivitySeaRace m_Activity;

        public bool IsEnd => m_Activity != null && m_Activity.IsEnd;
        public bool IsInOpen => m_InOpen;

        private bool m_InOpen = false;

        /// <summary>
        /// 检查活动结束介于动画中无法关闭，动画结束后重新调用
        /// </summary>
        public void CheckEnd()
        {
            if (IsEnd)
            {
                OnEnd();
            }
        }

        protected override void OnPreOpen()
        {
            m_InOpen = true;
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(OnActivityEnd);
        }

        protected override void OnPostOpen()
        {
            m_InOpen = false;
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(OnActivityEnd);
        }

        protected override void OnPostClose()
        {
            m_Activity = null;
        }

        protected override void OnParse(params object[] items)
        {
            m_Activity = items[0] as ActivitySeaRace;
        }

        protected void SetBlock(bool block)
        {
            if (block)
            {
                Game.Manager.screenPopup.Block(delay_: true);
            }
            else
            {
                Game.Manager.screenPopup.Block(false, false);
            }
        }

        protected virtual void OnActivityEnd(ActivityLike act, bool expire)
        {
            if (act == m_Activity)
            {
                OnEnd();
            }
        }

        /// <summary>
        /// 活动结束
        /// </summary>
        protected virtual void OnEnd()
        {
            Close();
        }
    }
}