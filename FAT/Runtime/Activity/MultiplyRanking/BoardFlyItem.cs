/*
 * @Author: yanfuxing
 * @Date: 2025-07-30 15:40:09
 */
using TMPro;
using UnityEngine;

namespace FAT
{
    public class BoardFlyItem : MonoBehaviour
    {
        [Header("倍率排行榜")]
        [SerializeField] private GameObject rankBoardTrans;
        [SerializeField] private Animation rankAnim;
        [SerializeField] private AnimationEvent rankAnimEvent;
        [SerializeField] private TextMeshProUGUI multiplierAfterNum;
        [SerializeField] private TextMeshProUGUI multiplierNumText;

        /// <summary>
        /// 设置倍率排行榜动画表现
        /// </summary>
        /// <param name="act"></param>
        /// <param name="re"></param>
        /// <param name="num"></param>
        public void SetRankAnimView(ActivityLike act, RewardCommitData re, TextMeshProUGUI num)
        {
            if (rankBoardTrans != null)
            {
                rankBoardTrans.gameObject.SetActive(true);
                if (rankAnim != null)
                {
                    var multiplierIndex = (act as ActivityMultiplierRanking).GetMultiplierIndex() - 1;
                    int slotNum = (act as ActivityMultiplierRanking).GetMultiplier(multiplierIndex);
                    if(slotNum <= 1)
                    {
                        //策划约定1倍率不进行展示动画
                        return;
                    }
                    rankAnim.Play();
                    multiplierAfterNum.text = (re.rewardCount * slotNum).ToString();
                    multiplierNumText.text = "x" + slotNum;
                    rankAnimEvent.SetCallBack(AnimationEvent.AnimationTrigger, () =>
                    {
                        num.gameObject.SetActive(false);
                        multiplierAfterNum.gameObject.SetActive(true);
                    });
                }
            }
        }

        /// <summary>
        /// 获取倍率排行榜动画长度
        /// </summary>
        /// <returns></returns>
        public float GetRankAnimLength()
        {
            if (rankAnim != null)
            {
                return rankAnim.clip.length;
            }
            return -1f;
        }

        /// <summary>
        /// 重置节点状态
        /// </summary>
        public void Reset()
        {
            multiplierAfterNum.gameObject.SetActive(false);
            rankBoardTrans.gameObject.SetActive(false);
        }
    }
}