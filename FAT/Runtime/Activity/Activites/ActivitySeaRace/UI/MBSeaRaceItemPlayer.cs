using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace FAT
{
    public class MBSeaRaceItemPlayer : MonoBehaviour
    {
        #region  base info
        [SerializeField] private UIImageState m_ImgStateBg;
        [SerializeField] private TextMeshProUGUI m_TxtName;
        [SerializeField] private Animator m_Animator;
        #endregion

        #region  score info
        [SerializeField] private GameObject m_ObjGlow;
        [SerializeField] private Image m_ImgScoreBg;
        [SerializeField] private Image m_ImgShadow;
        [SerializeField] private TextMeshProUGUI m_TxtScore;
        [SerializeField] private TextMeshProUGUI m_TxtRank;
        [SerializeField] private AnimationCurve m_CurveScore;
        [SerializeField][Range(0f, 1f)] private float m_Interval = 0.1f; //第二个角色开始移动时添加延迟
        #endregion

        public float addMoveDelay => m_Interval;

        private SeaRacePlayerInfo m_Info;
        private string m_SelfName = "YOU";
        private MBSeaRaceItemStageRank m_RankStage;

        public void OnDisable()
        {
            BindStage(null);
        }

        public void ShowSelf()
        {
            m_TxtName.text = m_SelfName;
        }

        public void SetData(SeaRacePlayerInfo info)
        {
            m_Info = info;
        }

        public SeaRacePlayerInfo GetData()
        {
            return m_Info;
        }

        public void UpdateBase()
        {
            var isMe = m_Info.Uid == m_Info.Activity.UserUid;
            var nameStr = (char)('A' + m_Info.Uid);
            m_TxtName.text = isMe ? m_SelfName : nameStr.ToString();
            m_TxtName.fontSizeMax = isMe ? 54 : 60;
            m_ImgStateBg.Select(m_Info.Uid);
            if (m_ObjGlow != null) m_ObjGlow.SetActive(false);
            if (m_ImgShadow != null) m_ImgShadow.gameObject.SetActive(false);
        }

        public void UpdateAll()
        {
            UpdateBase();
            m_TxtScore.text = m_Info.Score.ToString();
            m_TxtRank.text = m_Info.GetRank().ToString();
            m_ImgScoreBg.gameObject.SetActive(m_Info.Score >= 0);
            m_TxtScore.gameObject.SetActive(m_Info.Score >= 0);
            m_ObjGlow.SetActive(m_Info.Score < 0 && IsSelf());
            m_ImgShadow.gameObject.SetActive(m_Info.Score < 0);
        }

        public Tween TweenToScore(int score, float time = 1f)
        {
            var oldScore = m_Info.Score;
            var newScore = score;
            var currentScore = oldScore;
            var tween = DOTween.To(() => currentScore, (x) => currentScore = x, newScore, time).OnUpdate(() =>
            {
                m_TxtScore.text = currentScore.ToString();
            }).SetEase(m_CurveScore);
            m_TxtScore.text = currentScore.ToString();
            return tween;
        }

        public void HideScore()
        {
            m_TxtScore.gameObject.SetActive(false);
            m_ImgScoreBg.gameObject.SetActive(false);
            m_ObjGlow.SetActive(IsSelf());
            m_ImgShadow.gameObject.SetActive(true);
        }

        public Tween TweenToStageX(MBSeaRaceItemStageRank stage, int index, bool isToNew, float addDelay, bool isRank = false)
        {
            transform.DOKill(true);
            m_Animator.SetTrigger("Idle");
            var pos = isToNew && !IsSelf() ? stage.GetNextPos() : stage.GetPos(0);
            var tween = transform.DOMoveX(pos.x, stage.duration).SetDelay(stage.delay + addDelay).SetEase(stage.curveX);
            tween.OnStart(() =>
            {
                m_Animator.SetTrigger("Move");
                m_Info.Activity.TriggerSound("Jump");
            });
            if (!isRank)
            {
                tween.OnComplete(() =>
                {
                    stage.PlayParticle();
                });
            }
            return tween;
        }

        public Tween TweenToStageY(MBSeaRaceItemStageRank stage, int index, bool isToNew, float addDelay)
        {
            var pos = isToNew && !IsSelf() ? stage.GetNextPos() : stage.GetPos(0);
            var tween = transform.DOMoveY(pos.y, stage.duration).SetDelay(stage.delay + addDelay).SetEase(stage.curveY);
            return tween;
        }

        public Tween TweenToStageX(MBSeaRaceItemStage stage)
        {
            m_Animator.SetTrigger("Idle");
            var pos = stage.GetPos();
            var tween = transform.DOMoveX(pos.x, stage.duration).SetDelay(stage.delay).SetEase(stage.curveX);
            tween.OnStart(() =>
            {
                m_Animator.SetTrigger("Move");
            });
            return tween;
        }

        public Tween TweenToStageY(MBSeaRaceItemStage stage)
        {
            var pos = stage.GetPos();
            var tween = transform.DOMoveY(pos.y, stage.duration).SetDelay(stage.delay).SetEase(stage.curveY);
            return tween;
        }


        public void BindStage(MBSeaRaceItemStageRank stage)
        {
            if (m_RankStage != stage)
            {
                if (m_RankStage != null)
                {
                    m_RankStage.OutStage(this);
                }
                m_RankStage = stage;
            }
        }

        public MBSeaRaceItemStageRank GetStage()
        {
            return m_RankStage;
        }

        public bool IsSelf()
        {
            return m_Info.Uid == m_Info.Activity.UserUid;
        }


    }
}
