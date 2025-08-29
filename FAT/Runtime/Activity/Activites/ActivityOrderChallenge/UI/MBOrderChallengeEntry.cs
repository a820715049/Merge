/**
 * @Author: zhangpengjian
 * @Date: 2024/11/12 15:40:51
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/11/12 15:40:51
 * Description: 
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FAT
{
    public class MBOrderChallengeEntry : MonoBehaviour
    {
        [SerializeField] private Button btn;
        [SerializeField] private TMP_Text num;
        [SerializeField] private MBBoardOrder orderInst;
        [SerializeField] private Animation anim;
        [SerializeField] private GameObject effect;
        private bool isEffectShowing;

        private void Awake()
        {
            btn.onClick.AddListener(_OnBtnGoToActivity);
        }

        private void Update()
        {
            if (IsOrderCanCommit(orderInst.data))
            {
                if (!isEffectShowing)
                {
                    RefreshEffect();
                }
            }
            else
            {
                if (isEffectShowing)
                {
                    RefreshEffect();
                }
            }
        }

        private void RefreshEffect()
        {
            isEffectShowing = IsOrderCanCommit(orderInst.data);
            if (isEffectShowing)
                anim.Play("OrderItem_Challenge_Sweep");
            else
                anim.Stop();
        }

        private bool IsOrderCanCommit(IOrderData order)
        {
            if (orderInst.data == null)
                return false;
            return order.State == OrderState.Rewarded || order.State == OrderState.Finished;
        }

        private void OnEnable()
        {
            var act = Game.Manager.activity.LookupAny(fat.rawdata.EventType.ZeroQuest) as ActivityOrderChallenge;
            if (act != null)
                num.SetText(act.ChallengeShowInfo);
            RefreshEffect();                
        }

        private void _OnBtnGoToActivity()
        {
            var act = Game.Manager.activity.LookupAny(fat.rawdata.EventType.ZeroQuest) as ActivityOrderChallenge;
            if (act != null)
            {
                act.OpenPanel();
            }
        }
    }
}