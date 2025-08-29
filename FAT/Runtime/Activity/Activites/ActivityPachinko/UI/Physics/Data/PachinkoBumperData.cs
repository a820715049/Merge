/*
 * @Author: tang.yan
 * @Description: 弹珠游戏-UI层面保险杠相关数据类 
 * @Date: 2024-12-10 16:12:29
 */

using Coffee.UIExtensions;
using Cysharp.Text;
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class PachinkoBumperData : PachinkoEntityData
    {
        //记录每次球发射时 整个流程中和当前保险杠和小球的碰撞次数
        private int _colliderCount;
        private Animator _animator;
        private Animator _animatorHigh;
        private Image _progress;
        private UIParticle _easyEffect;
        private UIParticle _rewardEffect1;
        private UIParticle _rewardEffect2;
        private UIParticle _rewardEffect3;
        private UICommonItem _reward;

        private PachinkoBumper _bumperData;
        private int _flyIconId;
        private Vector3 _flyPos;

        protected override void AfterBindRoot()
        {
            _iconTrans = _root.FindEx<RectTransform>("scale/Icon");
            _animator = _root.GetComponent<Animator>();
            _animatorHigh = _root.FindEx<Animator>("BigReward");
            _progress = _root.FindEx<Image>("scale/Icon/Progress");
            _easyEffect = _root.FindEx<UIParticle>("fx_pachinko_collision_easy");
            _rewardEffect1 = _root.FindEx<UIParticle>("scale/fx_pachinko_collision_complex_a");
            _rewardEffect2 = _root.FindEx<UIParticle>("scale/fx_pachinko_collision_complex_b");
            _rewardEffect3 = _root.FindEx<UIParticle>("scale/fx_pachinko_collision_complex_c");
            _reward = _root.FindEx<UICommonItem>("scale/Reward");
            _reward.Setup();
        }

        public void RefreshBumperUI()
        {
            //每次刷新都获取下数据层data
            _bumperData = Game.Manager.pachinkoMan.GetBumperByIndex(_index);
            _flyIconId = Game.Manager.pachinkoMan.EnergyID;
            var offset = _root.rect.height / 2;
            var rootPos = _root.position;
            _flyPos = new Vector3(rootPos.x, rootPos.y + offset, 0);
            _RefreshProgress();
            _RefreshReward();
        }

        //获得最终大奖时播放保险杠相关特效
        public void PlayFinalRewardEffect()
        {
            
        }
        
        public int GetColliderCount()
        {
            return _colliderCount;
        }

        public void ResetColliderCount()
        {
            _colliderCount = 0;
        }

        protected override void OnColliderBegin(bool isDebug)
        {
            _AddColliderCount();
            //非debug模式下才播动效
            if (!isDebug)
            {
                _ExecuteCollider();
            }
        }
        
        private void _AddColliderCount()
        {
            _colliderCount++;
        }

        private void _ExecuteCollider()
        {
            var result = Game.Manager.pachinkoMan.ImpactBumper(_index);
            var flyNum = ZString.Concat("+", result.Item1);
            var reward = result.Item2;
            var hasReward = reward != null;
            //1.在指定位置播放飘字
            UIManager.Instance.OpenWindow(UIConfig.UIPachinkoPopFly, _flyIconId, flyNum, _flyPos);
            //2.播特效
            if (!hasReward)
            {
                //没奖时播对应特效 并且直接刷新进度条进度
                _RefreshProgress();
                _PlayEasyEffect();
            }
            else
            {
                _RefreshProgress();
                //有奖时播对应特效 直接发奖励 在动画播完后 刷新进度条进度和奖励内容
                _PlayRewardEffect();
                UIFlyUtility.FlyReward(reward, _iconTrans.position);
            }
        }
        
        private void _RefreshProgress()
        {
            if (_bumperData == null) return;
            _progress.fillAmount = (float)_bumperData.Energy / _bumperData.MaxEnergy;
        }

        private void _RefreshReward()
        {
            if (_bumperData == null) return;
            _reward.Refresh(_bumperData.RewardConfig);
        }

        public void OnAnimPlayEnd(AnimatorStateInfo stateInfo)
        {
            if (stateInfo.IsName("UIBumper_Punch_more") && _isPlayRewardEffect)
            {
                _isPlayRewardEffect = false;
                _RefreshProgress();
                _RefreshReward();
            }
        }
        
        //播放保险杠碰撞时的动特效
        private void _PlayEasyEffect()
        {
            if (_isPlayRewardEffect) return;
            //播碰撞音效
            Game.Manager.audioMan.TriggerSound("PachinkoHitBumper");
            _easyEffect.Stop();
            _easyEffect.Play();
            _animator.ResetTrigger("PunchOnce");
            _animator.SetTrigger("PunchOnce");
        }

        private bool _isPlayRewardEffect = false;   //当前是否正在播得奖特效
        //播放保险杠碰撞后获得奖励时的动特效
        private void _PlayRewardEffect()
        {
            if (_isPlayRewardEffect) return;
            //播碰撞音效
            Game.Manager.audioMan.TriggerSound("PachinkoHitReward");
            _rewardEffect1.Stop();
            _rewardEffect1.Play();
            _rewardEffect2.Stop();
            _rewardEffect2.Play();
            _rewardEffect3.Stop();
            _rewardEffect3.Play();
            _animator.ResetTrigger("PunchMore");
            _animator.SetTrigger("PunchMore");
            _isPlayRewardEffect = true;
        }

        public void PlayBigRewardEffect()
        {
            _animatorHigh.ResetTrigger("PunchHigh");
            _animatorHigh.SetTrigger("PunchHigh");
        }
    }
}