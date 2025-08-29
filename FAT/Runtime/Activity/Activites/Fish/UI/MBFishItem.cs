/**
 * @Author: zhangpengjian
 * @Date: 2025/4/1 15:51:11
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/4/1 15:51:11
 * Description: 钓鱼棋盘图鉴
 */

using System.Collections;
using fat.rawdata;
using UnityEngine;
using UnityEngine.UI;
using static FAT.ActivityFishing;

namespace FAT
{
    public class MBFishItem : MonoBehaviour
    {
        public UIImageRes icon;
        public UIImageRes iconDark;
        public UIImageRes bg;
        public GameObject[] stars;
        public GameObject starRoot;
        public MBRewardProgress progress;
        public Animator animator;
        public FishInfo fish;
        private ActivityFishing activityFish;

        public void OnEnable()
        {
            animator.enabled = false;
            foreach (var item in stars)
            {
                item.GetComponent<Animator>().enabled = false;
            }
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        public void OnDisable()
        {
            GetComponent<Button>().onClick.RemoveListener(OnClick);
        }

        public void OnClick()
        {
            var fishCount = activityFish.GetFishCaughtCount(fish.Id);
            var star = activityFish.CalcFishStarByCount(fish.Id, fishCount);
            if (star > 0)
            {
                GetComponentInParent<UIActivityFishMain>().EnsureItemFullyVisible(transform as RectTransform, fish.Id);
            }
            else
            {
                Game.Manager.commonTipsMan.ShowPopTips(Toast.FishNoHave, transform.position);
            }
        }

        public void PlayAnim(FishCaughtInfo info)
        {
            if (info.fishId != fish.Id)
            {
                return;
            }
            progress.gameObject.SetActive(true);
            if (info.nowStar - info.preStar > 0)
            {
                starRoot.SetActive(true);
                for (int i = 0; i < fish.Star.Count; i++)
                {
                    stars[i].gameObject.SetActive(true);
                }
                var (_, needPre, _) = fat.conf.Data.GetFishInfo(fish.Id).Star[info.preStar].ConvertToInt3();
                if (info.nowStar >= fish.Star.Count)
                {
                    StartCoroutine(DelayHide(needPre, info.nowStar));
                }
                else
                {
                    var (_, need, _) = fat.conf.Data.GetFishInfo(fish.Id).Star[info.nowStar].ConvertToInt3();
                    var have = activityFish.CalcFishStarRequireCount(fish.Id, info.nowStar);
                    StartCoroutine(DelayRefresh(info.nowCount - have, need, needPre, info.nowStar));
                }
            }
            else
            {
                var (_, n, _) = fat.conf.Data.GetFishInfo(fish.Id).Star[info.nowStar].ConvertToInt3();
                var r = activityFish.CalcFishStarRequireCount(fish.Id, info.nowStar);
                StartCoroutine(DelayAnim(info.nowCount - r, n));
            }
            icon.gameObject.SetActive(true);
            icon.SetImage(fish.Icon);
            animator.enabled = true;
            animator.SetTrigger("Punch");
        }

        private IEnumerator DelayAnim(int n, int r)
        {
            yield return new WaitForSeconds(0.58f);
            progress.Refresh(n, r, 0.5f);
        }

        private IEnumerator DelayRefresh(int v, int t, int pre, int nowStar)
        {
            yield return new WaitForSeconds(0.58f);
            progress.Refresh(pre, pre, 0.5f);
            yield return new WaitForSeconds(0.5f);
            stars[nowStar - 1].transform.SetAsLastSibling();
            stars[nowStar - 1].GetComponent<Animator>().enabled = true;
            stars[nowStar - 1].GetComponent<Animator>().SetTrigger("Punch");
            Game.Manager.audioMan.TriggerSound("FishBoardStar");
            for (int i = 0; i < nowStar; i++)
            {
                stars[i].transform.GetChild(0).gameObject.SetActive(true);
            }
            progress.Refresh(v, t);
        }

        private IEnumerator DelayHide(int t, int nowStar)
        {
            yield return new WaitForSeconds(0.58f);
            progress.Refresh(t, t, 0.5f);
            yield return new WaitForSeconds(0.5f);
            stars[nowStar - 1].transform.SetAsLastSibling();
            stars[nowStar - 1].GetComponent<Animator>().enabled = true;
            stars[nowStar - 1].GetComponent<Animator>().SetTrigger("Punch");
            Game.Manager.audioMan.TriggerSound("FishBoardStar");
            for (int i = 0; i < nowStar; i++)
            {
                stars[i].transform.GetChild(0).gameObject.SetActive(true);
            }
            progress.gameObject.SetActive(false);
        }

        public void Setup(ActivityFishing activity, FishInfo fish, bool isNormal = true)
        {
            activityFish = activity;
            this.fish = fish;
            foreach (var item in stars)
            {
                item.gameObject.SetActive(true);
                item.transform.GetChild(0).gameObject.SetActive(false);
            }
            var fishCount = activity.GetFishCaughtCount(fish.Id);
            var star = activity.CalcFishStarByCount(fish.Id, fishCount);
            starRoot.SetActive(fishCount > 0);
            progress.gameObject.SetActive(fishCount > 0);
            iconDark.gameObject.SetActive(fishCount <= 0);
            icon.gameObject.SetActive(fishCount > 0);
            bg.SetImage(fat.conf.Data.GetFishRarity(fish.Rarity).BookImg);
            if (star > 0)
            {
                icon.SetImage(fish.Icon);
                for (int i = 0; i < star; i++)
                {
                    stars[i].transform.GetChild(0).gameObject.SetActive(true);
                }
                if (star >= fish.Star.Count)
                {
                    progress.gameObject.SetActive(false);
                }
                else
                {
                    var (_, need, _) = fat.conf.Data.GetFishInfo(fish.Id).Star[star].ConvertToInt3();
                    var have = activity.CalcFishStarRequireCount(fish.Id, star);
                    progress.Refresh(fishCount - have, need);
                }
            }
            else
            {
                var (_, need, _) = fat.conf.Data.GetFishInfo(fish.Id).Star[star].ConvertToInt3();
                progress.Refresh(0, need);
                iconDark.SetImage(fish.IconDark);
            }
            if (!isNormal)
            {
                progress.gameObject.SetActive(false);
            }
        }
    }
}