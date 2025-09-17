using TMPro;
using System.Collections;
using EL;
using UnityEngine;

namespace FAT
{
    public class MbFlyScoreItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Animator anim;
        [SerializeField] private float yOffset;
        [SerializeField] private float xOffset;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            text = transform.Find("num").GetComponent<TextMeshProUGUI>();
            anim = transform.GetComponent<Animator>();
        }
#endif

        public void FlyScore(ScoreEntity.ScoreFlyRewardData r)
        {
            text.SetText(r.rewardCount.ToString());
            anim.SetTrigger("Punch");
            Game.Instance.StartCoroutineGlobal(OnFlyScore(r));
        }

        private IEnumerator OnFlyScore(ScoreEntity.ScoreFlyRewardData r)
        {
            var position = transform.position;
            position.Set(position.x - xOffset, position.y + yOffset, position.z);
            //等棋牌内棋子上方的动画播放完毕后飞奖励
            yield return new WaitForSeconds(0.5f);
            float waitProgress = 0;
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.Score, out var activity);
            if (activity is ActivityScore scoreActivity)
            {
                if (Game.Manager.mapSceneMan.scene.Active)
                {
                    waitProgress = 0.5f;
                    MessageCenter.Get<MSG.GAME_SCORE_GET_PROGRESS_BOARD>()
                        .Dispatch(scoreActivity.TotalScore - r.rewardCount, scoreActivity.TotalScore);
                }
                else if (Game.Manager.mergeBoardMan.activeWorld?.isEquivalentToMain ?? false)
                {

                    waitProgress = 0.5f;
                    MessageCenter.Get<MSG.GAME_SCORE_GET_PROGRESS_BOARD>()
                        .Dispatch(scoreActivity.TotalScore - r.rewardCount, scoreActivity.TotalScore);
                }

            }
            if (waitProgress > 0)
            {
                yield return new WaitForSeconds(waitProgress);
            }

            var ft = UIFlyFactory.ResolveFlyType(r.rewardId);
            var to = UIFlyFactory.ResolveFlyTarget(ft);
            UIFlyUtility.FlyCustom(r.rewardId, r.rewardCount, position, to, FlyStyle.Reward, ft,
                (() => MessageCenter.Get<MSG.SCORE_PROGRESS_ANIMATE>().Dispatch()));
        }
    }
}