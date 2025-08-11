/**
 * @Author: zhangpengjian
 * @Date: 2024/10/30 16:23:13
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/10/30 16:23:13
 * Description: 连续限时订单胜利界面
 */

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using EL;
using System.Collections.Generic;
using DG.Tweening;

namespace FAT
{
    public class UIOrderChallengeVictory : UIBase
    {
        [SerializeField] private TextMeshProUGUI rewardCount;
        [SerializeField] private UIImageRes rewardIcon;
        [SerializeField] private TextMeshProUGUI tip;
        [SerializeField] private TextMeshProUGUI numText;
        [SerializeField] private Button button;
        [SerializeField] private List<GameObject> playerList;
        [SerializeField] private Animator animator;
        public float delay;
        private ActivityOrderChallenge activityOrderChallenge;
        private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private int maxNum = 6;
        protected override void OnCreate()
        {
            button.onClick.AddListener(OnClickClose);
        }

        private void OnClickClose()
        {
            if (activityOrderChallenge != null && activityOrderChallenge.rewardCommitData != null)
            {
                UIFlyUtility.FlyReward(activityOrderChallenge.rewardCommitData, rewardIcon.transform.position, null, 66f);
                UIUtility.FadeOut(this, transform.GetComponent<Animator>());
                if (!activityOrderChallenge.IsOver())
                    activityOrderChallenge.StartRes.ActiveR.Open(activityOrderChallenge);
            }
            else
            {
                UIUtility.FadeOut(this, transform.GetComponent<Animator>());
            }
        }

        protected override void OnPreOpen()
        {
            transform.GetComponent<Animator>().SetTrigger("Show");
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.ZeroQuest, out var activity);
            if (activity == null)
            {
                return;
            }
            activityOrderChallenge = (ActivityOrderChallenge)activity;

            UIManager.Instance.Block(true);
            var num = 0;
            DOTween.To(() => num, x =>
            {
                num = x;
                numText.text = num.ToString();
            }, activityOrderChallenge.finalGetRewardNum, delay).OnComplete(() =>
            {
                animator.SetTrigger("Punch");
                UIManager.Instance.Block(false);
            });
            Game.Manager.audioMan.TriggerSound("OrderChallengeWin");

            IEnumerator sound()
            {
                yield return new WaitForSeconds(0.2f);
                Game.Manager.audioMan.TriggerSound("OrderChallengeWinNum");
            }

            Game.Instance.StartCoroutineGlobal(sound());
            rewardIcon.SetImage(Game.Manager.rewardMan.GetRewardIcon(activityOrderChallenge.curLevelTotalReward.Id, activityOrderChallenge.curLevelTotalReward.Count));
            rewardCount.SetText(activityOrderChallenge.curLevelTotalReward.Count.ToString());
            tip.SetText(I18N.FormatText("#SysComDesc694", activityOrderChallenge.FinalLeftPlayer - 1));
            // numText.SetText(activityOrderChallenge.finalGetRewardNum.ToString());
            System.Random random = new System.Random();
            var n = activityOrderChallenge.CalcOutShowNum(activityOrderChallenge.FinalLeftNum);
            foreach (var item in playerList)
            {
                item.gameObject.SetActive(false);
            }
            for (int i = 0; i < n; i++)
            {
                if (i < playerList.Count)
                {
                    playerList[i].gameObject.SetActive(true);
                    var t = playerList[i].transform.Find("you").GetComponent<TextMeshProUGUI>();
                    int index = random.Next(alphabet.Length);
                    char randomLetter = alphabet[index];
                    var letter = randomLetter.ToString();
                    t.SetText(letter);
                    if (i == maxNum - 1)
                    {
                        var bg = playerList[i].transform.GetChild(0);
                        for (int j = 0; j < bg.childCount; j++)
                        {
                            bg.GetChild(j).gameObject.SetActive(false);
                        }
                        var r = random.Next(0, 4);
                        bg.GetChild(r).gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}