/*
 * @Author: pengjian.zhang
 * 此脚本承载的功能：飞到中间展示 表演结束后 走通用飞奖励
 * @Date: 2024-03-20 16:55:04
 */

using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace FAT
{
    public class MBBoardFly : MonoBehaviour
    {
        [SerializeField] private GameObject boardFlyItem;
        [SerializeField] private TextMeshProUGUI num;
        [SerializeField] private GameObject normalEffect;
        [SerializeField] private GameObject scoreEffect;
        [SerializeField] private GameObject boardFlyRoot;
        private List<Transform> listFlyItem = new();

        public void Setup()
        {
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.BOARD_FLY_ITEM, boardFlyItem);
        }

        public void InitOnPreOpen()
        {
            MessageCenter.Get<MSG.SCORE_FLY_REWARD_CENTER>().AddListener(ShowFlyCenterReward);
            _FirstTimeShow();
        }

        public void CleanupOnPostClose()
        {
            MessageCenter.Get<MSG.SCORE_FLY_REWARD_CENTER>().RemoveListener(ShowFlyCenterReward);
            foreach (var item in listFlyItem)
            {
                item.gameObject.SetActive(false);
            }
            listFlyItem.Clear();
            normalEffect.gameObject.SetActive(false);
            scoreEffect.gameObject.SetActive(false);
            num.gameObject.SetActive(false);
        }

        public void ShowFlyCenterReward((Vector3 from, RewardCommitData re, ActivityLike act) obj)
        {
            var trans = GameObjectPoolManager.Instance.CreateObject(PoolItemType.BOARD_FLY_ITEM, boardFlyRoot.transform).transform as RectTransform;
            if (trans != null)
            {
                float releaseTime = 2f;
                if (obj.act is ActivityMultiplierRanking)
                {
                    var boardFlyItem = trans.GetComponent<BoardFlyItem>();
                    if (boardFlyItem != null && (obj.act as ActivityMultiplierRanking).GetCurMultiplierNum() > 1)
                    {
                        //策划约定1倍率不进行展示动画
                        releaseTime = boardFlyItem.GetRankAnimLength();
                    }
                    MessageCenter.Get<MSG.BOARD_FLY_START>().Dispatch();
                }
                BoardUtility.AddAutoReleaseComponent(trans.gameObject, releaseTime, PoolItemType.BOARD_FLY_ITEM);
                trans.gameObject.SetActive(true);
                trans.transform.position = obj.from;
                trans.transform.GetChild(0).GetComponent<UIImageRes>().SetImage(Game.Manager.rewardMan.GetRewardIcon(obj.re.rewardId, obj.re.rewardCount));
                var to = UIFlyFactory.ResolveFlyTarget(FlyType.BoardCenter);
                var seq = DOTween.Sequence();
                seq.Append(trans.transform.DOScale(1f, 0.25f).From(Vector3.one * 0.1f, true));
                seq.Join(trans.DOMove(to, 0.25f).SetEase(Ease.Linear));
                seq.OnComplete(() => { Game.Instance.StartCoroutineGlobal(_OnFlyCenterComplete(trans, obj.re, to, obj.act)); });
                seq.Play();
                listFlyItem.Add(trans);
            }
        }

        private IEnumerator _OnFlyCenterComplete(Transform trans, RewardCommitData re, Vector3 from, ActivityLike act)
        {
            Game.Manager.audioMan.TriggerSound("WhiteBallBig");
            float waitTime = 1f;
            if (act is ActivityMultiplierRanking)
            {
                if ((act as ActivityMultiplierRanking).GetCurMultiplierNum() > 1)
                {
                    //倍率排行榜因为要播放动画，所以等待时间会延长
                    waitTime = 1.5f;
                }
            }
            var wait = new WaitForSeconds(waitTime);
            num.gameObject.SetActive(true);
            //根据不同活动 表现不同 例如积分活动需要区分特效和字体描边
            if (act is ActivityScore scoreAct && act.Valid && re.rewardId == scoreAct.ConfD.RequireCoinId)
            {
                scoreAct.Visual.RefreshStyle(num, scoreAct.themeFontStyleId_Score);
                scoreEffect.gameObject.SetActive(true);
            }
            else
            {
                normalEffect.gameObject.SetActive(true);
            }
            num.text = re.rewardCount.ToString();
            BoardFlyItem boardFlyItem = null;
            if (act is ActivityMultiplierRanking)
            {
                //倍率排行榜需要播放额外的动画表现
                boardFlyItem = trans.GetComponent<BoardFlyItem>();
                if (boardFlyItem != null)
                {
                    boardFlyItem.SetRankAnimView(act, re, num);
                }
            }
            yield return wait;
            float waitProgress = 0;
            if (act is ActivityScore scoreActivity && Game.Manager.mapSceneMan.scene.Active)
            {
                waitProgress = 0.5f;
                MessageCenter.Get<MSG.GAME_SCORE_GET_PROGRESS_BOARD>().Dispatch(scoreActivity.TotalScore - re.rewardCount, scoreActivity.TotalScore);
            }
            trans.gameObject.SetActive(false);
            normalEffect.gameObject.SetActive(false);
            scoreEffect.gameObject.SetActive(false);
            num.gameObject.SetActive(false);
            if (boardFlyItem != null)
            {
                boardFlyItem.Reset();
            }
            //飞奖励：生成奖励icon 飞；而随机宝箱不飞奖励 直接commit后 尝试打开
            if (UIFlyFactory.CheckNeedFlyIcon(re.rewardId))
            {
                if (waitProgress > 0)
                {
                    yield return new WaitForSeconds(waitProgress);
                }
                UIFlyUtility.FlyReward(re, from, ((() =>
                {
                    if (act is ActivityScore)
                    {
                        //飞到棋盘中间 积分活动有两种形式：1订单积分飞中间 2里程碑奖励飞中间 此消息只处理订单积分飞中间后事件 用来播放动画
                        var scoreActivity = (ActivityScore)act;
                        if (re.rewardId == scoreActivity.ConfD.RequireCoinId)
                            MessageCenter.Get<MSG.SCORE_PROGRESS_ANIMATE>().Dispatch();
                    }
                    else if (act is ActivityTreasure)
                    {
                        MessageCenter.Get<MSG.TREASURE_HELP_REFRESH_RED_DOT>().Dispatch();
                    }
                    else if (act is ActivityRace)
                    {
                        //RaceManager.GetInstance().Race?.TryShowStartNew();
                    }
                    else if (act is ActivityDigging)
                    {
                        MessageCenter.Get<MSG.DIGGING_ENTRY_REFRESH_RED_DOT>().Dispatch();
                    }
                    else if (act is ActivityGuess)
                    {
                        MessageCenter.Get<MSG.ACTIVITY_GUESS_ENTRY_REFRESH_RED_DOT>().Dispatch();
                    }
                    else if (act is ActivityRedeemShopLike)
                    {
                        MessageCenter.Get<MSG.REDEEMSHOP_ENTRY_REFRESH_RED_DOT>().Dispatch();
                    }
                })));
            }
            else
            {
                Game.Manager.rewardMan.CommitReward(re);
            }
            yield return wait;
        }

        private void _FirstTimeShow()
        {
            boardFlyItem.gameObject.SetActive(false);
        }
    }
}