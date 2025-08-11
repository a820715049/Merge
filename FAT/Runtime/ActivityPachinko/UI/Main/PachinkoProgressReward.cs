/*
 * @Author: tang.yan
 * @Description: 弹珠游戏进度条上的奖励
 * @Date: 2024-12-16 14:12:31
 */

using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class PachinkoProgressReward : MonoBehaviour
    {
        [SerializeField] private UIImageRes itemIcon;
        [SerializeField] private Button itemBtn;
        [SerializeField] private GameObject itemTips;
        [SerializeField] private TMP_Text itemCount;
        [SerializeField] private Animator receiveAnim;
        [SerializeField] private Animator animator;
        //当前正在展示的物品id
        private int _curShowItemId;
        //tips偏移值 默认为4
        private int _tipsOffset = 4;
        //新加字段 用于判断是否setup 避免外部忘记调用
        private bool _isSetup = false;
        //记录目前奖励是否已领取
        private bool _isReceive = false;
        
        //初始化OnCreate时或者从池中取出时调用
        public void Setup()
        {
            if (_isSetup) return;
            _isSetup = true;
            itemBtn.onClick.AddListener(_OnBtnClick);
        }

        //放入池中时调用
        public void Clear()
        {
            _curShowItemId = 0;
            _isSetup = false;
            itemBtn.onClick.RemoveAllListeners();
        }

        public bool GetIsReceive()
        {
            return _isReceive;
        }

        public void PlayReceiveAnim()
        {
            _isReceive = true;
            animator.ResetTrigger("Punch");
            animator.SetTrigger("Punch");
            receiveAnim.ResetTrigger("Punch");
            receiveAnim.SetTrigger("Punch");
        }

        public void Refresh(EventPachinkoMilestone conf, bool isReceive)
        {
            var valid = conf != null && conf.MilestoneReward.Count > 0;
            gameObject.SetActive(valid);
            _isReceive = isReceive;
            if (!valid) return;
            var reward = conf.MilestoneReward[0].ConvertToRewardConfig();
            if (reward != null)
            {
                _Refresh(reward.Id, reward.Count);
                animator.ResetTrigger("Punch");
                var trigger = _isReceive ? "Idle" : "Hide";
                receiveAnim.SetTrigger(trigger);
            }
        }

        private void _Refresh(int itemId, int count)
        {
            var conf = Game.Manager.objectMan.GetBasicConfig(itemId);
            if (conf == null) return;
            _curShowItemId = itemId;
            //刷新icon
            itemIcon.SetImage(conf.Icon);
            //刷新tips按钮
            bool showTips = UIItemUtility.ItemTipsInfoValid(itemId);
            itemTips.SetActive(showTips);
            //若刷新时image为空 则尝试直接获取一下
            if (itemIcon.image == null)
            {
                if (itemIcon.TryGetComponent<Image>(out var img))
                {
                    img.raycastTarget = showTips;
                }
            }
            else
            {
                itemIcon.image.raycastTarget = showTips;
            }
            if (itemBtn != null)
                itemBtn.interactable = showTips;
            //刷新数量
            itemCount.text = UIUtility.SpecialCountText(itemId, count, out var countStr) ? countStr : count.ToString();
        }
        
        private void _OnBtnClick()
        {
            if (_curShowItemId <= 0)
                return;
            var root = itemIcon.image.rectTransform;
            UIItemUtility.ShowItemTipsInfo(_curShowItemId, root.position, _tipsOffset + root.rect.size.y * 0.5f);
        }
    }
}