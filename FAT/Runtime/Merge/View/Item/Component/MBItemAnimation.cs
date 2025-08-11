/*
 * @Author: qun.chao
 * @Date: 2021-02-24 12:04:06
 */
namespace FAT
{
    using UnityEngine;
    using Merge;

    public enum ItemHintState
    {
        Idle = 0,
        ReadyToUse,
        Match,
        Bubble,
        IdleScale,    // 某种原地缩放效果
    }

    /*
    点击反馈动画 和 状态类动画 完全正交
    原因是不希望随时播放的点击动画打乱状态类动画的周期规律 比如导致两个item的匹配动画不同步
    */

    public class MBItemAnimation : MonoBehaviour
    {
        private static readonly string kAnimTriggerTap = "Tap";
        private static readonly string kAnimTriggerSpawn = "Spawn";
        private static readonly string kAnimTriggerDrop = "Drop";

        [SerializeField] private Animator animator;
        private MBItemView mView;

        private int mState;
        private int mFinalAnimStateVal = -1;
        private bool mIsDirty = false;

        public void SetData(MBItemView view)
        {
            mView = view;
            mState = 0;

            if (view.data.HasComponent(ItemComponentType.Bubble))
            {
                AddHintType(ItemHintState.Bubble);
            }
            else if (view.data.TryGetItemComponent<ItemBonusCompoent>(out var bonus) && bonus.scaleAnim)
            {
                AddHintType(ItemHintState.IdleScale);
            }
            else if (view.data.TryGetItemComponent<ItemActiveSourceComponent>(out var activity) && activity.Config.IsScaleAnim)
            {
                AddHintType(ItemHintState.IdleScale);
            }
        }

        public void ClearData()
        {
            mFinalAnimStateVal = -1;
            mView = null;
        }

        public void PlayTap()
        {
            animator.SetTrigger(kAnimTriggerTap);
        }

        public void PlaySpawn()
        {
            animator.SetTrigger(kAnimTriggerSpawn);
        }

        public void PlayDropToGround()
        {
            animator.SetTrigger(kAnimTriggerDrop);
        }

        public void AddHintType(ItemHintState state)
        {
            mState |= (1 << (int)state) >> 1;
            mIsDirty = true;
        }

        public void RemoveHintType(ItemHintState state)
        {
            mState &= ~((1 << (int)state) >> 1);
            mIsDirty = true;
        }

        public void UpdateEx()
        {
            if (!mIsDirty)
                return;
            mIsDirty = false;

            _UpdateAnim();
        }

        private void _UpdateAnim()
        {
            int val = 0;
            int temp = mState;
            while (temp > 0)
            {
                temp = temp >> 1;
                ++val;
            }

            if (gameObject.activeSelf)
            {
                if (val != mFinalAnimStateVal)
                {
                    mFinalAnimStateVal = val;
                    animator.SetTrigger(((ItemHintState)val).ToString());
                }
            }
        }
    }
}
