using EL;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class MBSignReward : MonoBehaviour
    {
        public UIImageRes icon;
        public TextMeshProUGUI count;
        public TextMeshProUGUI day;
        public SkeletonGraphic cover;
        public GameObject effect;
        public Animator rootAnimator;
        public Animator dayAnimator;
        private int _day;
        private bool _hasClick;
        public void OnValidate()
        {
            transform.Access("Icon", out icon);
            transform.Access("Icon/Num", out count);
            transform.Access("Cover/DayTxt", out day);
            cover = transform.GetChild(1).GetChild(0).GetComponent<SkeletonGraphic>();
            effect = transform.GetChild(3).GetChild(0).gameObject;
            rootAnimator = transform.GetComponent<Animator>();
            transform.Access("Cover/DayTxt", out dayAnimator);
        }

        public void Awake()
        {
            transform.AddButton("Cover", OnClickGet);
        }

        /// <summary>
        /// 初始化信息 
        /// </summary>
        /// <param name="rewardid">奖励id</param>
        /// <param name="num">奖励数量</param>
        /// <param name="day">天数</param>
        public void SetUp(int rewardid, int num, int day)
        {
            _day = day;
            SetReward(rewardid, num);
        }


        /// <summary>
        /// 设置奖励
        /// </summary>
        /// <param name="id">奖励id</param>
        /// <param name="num">奖励数量</param>
        private void SetReward(int id, int num)
        {
            if (id == 0) return;
            icon.SetImage(Game.Manager.objectMan.GetBasicConfig(id).Icon);
            count.text = num == 0 ? "" : num.ToString();
            if (Game.Manager.loginSignMan.ConsecutiveSignInDay == _day)
                count.text = Game.Manager.loginSignMan.SignInReward.rewardCount.ToString();
        }

        /// <summary>
        /// 初始化显示状态
        /// </summary>
        /// <param name="isGet"></param>
        public void InitState(bool isGet)
        {
            SetGetState(isGet);
        }

        private void SetGetState(bool isGet)
        {
            effect.SetActive(_day == Game.Manager.loginSignMan.ConsecutiveSignInDay);
            cover.AnimationState.SetAnimation(0, isGet ? "hide" : "normal", false);
            if (isGet) dayAnimator.SetTrigger("Hide");
            if (!isGet) rootAnimator.SetTrigger("Wait");
        }

        public void OnClickGet()
        {
            if (_hasClick) return;
            if (!Game.Manager.loginSignMan.CheckSignInToday(_day)) return;
            _hasClick = true;
            UIManager.Instance.Block(true);
            cover.AnimationState.SetAnimation(0, "activate", false).Complete += delegate (TrackEntry entry)
            {
                UIManager.Instance.Block(false);
                rootAnimator.SetTrigger("Punch");
                MessageCenter.Get<MSG.GAME_SIGN_IN_CLICK>().Dispatch();
                if (Game.Manager.loginSignMan.SignInReward != null && Game.Manager.loginSignMan.SignInReward.rewardType != ObjConfigType.RandomBox)
                {
                    UIFlyUtility.FlyReward(Game.Manager.loginSignMan.SignInReward, icon.transform.position);
                    Game.Manager.loginSignMan.ClearConsecutiveReward();
                }
            };
            dayAnimator.SetTrigger("Punch");
            effect.SetActive(false);
        }
    }
}