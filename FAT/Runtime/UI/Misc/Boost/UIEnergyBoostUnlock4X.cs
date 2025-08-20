/*
 * @Author: qun.chao
 * @Date: 2024-12-24 15:06:26
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using EL;

namespace FAT
{
    public class UIEnergyBoostUnlock4X : UIBase
    {
        public static Vector3 rewardFromPos;

        [SerializeField] private Button btnClaim;
        [SerializeField] private TextMeshProUGUI txtTitle; // title在使用弧形文字 通过主动设置key避免文字不弯曲
        [SerializeField] private TextMeshProUGUI txtRewardNum;
        [SerializeField] private GameObject goEff;  // 4倍图标飞期间需要隐藏背光特效
        [SerializeField] private GameObject go4X;   // 4倍图标对应的节点
        [SerializeField] private float flyDelay = 0.3f;
        [SerializeField] private float flyDuration = 0.5f;
        [SerializeField] private float flyScaleCoe = 1f;
        [SerializeField] private Ease flyCurve = Ease.OutSine;
        [SerializeField] private TextMeshProUGUI tip;
        private readonly int rewardNum = 101;
        private Tween flyAnim;
        private GameObject flyObj;

        protected override void OnCreate()
        {
            btnClaim.onClick.AddListener(base.Close);
        }

        protected override void OnPreOpen()
        {
            var cfg = Game.Manager.configMan.GetEnergyBoostConfig((int)Merge.EnergyBoostState.X4);

            int requireEnergyNum = cfg.RequireEnergyNum;
            if (requireEnergyNum == 0)
            {
                tip.text = I18N.Text("#SysComDesc1419");
            }
            else
            {
                tip.text = I18N.FormatText("#SysComDesc780", requireEnergyNum);
            }

            MBI18NText.SetKey(txtTitle.gameObject, "#SysComDesc779");
            txtRewardNum.text = $"{rewardNum}";

            ResetFlyAnim();
        }

        protected override void OnPostOpen()
        {
            rewardFromPos = txtRewardNum.transform.position;
        }

        protected override void OnPreClose()
        {
            FlyToFeatureEntry();
        }

        private void ResetFlyAnim()
        {
            go4X.SetActive(true);
            goEff.SetActive(true);

            flyAnim?.Kill();
            flyAnim = null;
            if (flyObj != null)
            {
                Destroy(flyObj);
                flyObj = null;
            }
        }

        // 图标飞到功能所在位置
        private void FlyToFeatureEntry()
        {
            var to = UIFlyFactory.ResolveFlyTarget(FlyType.EnergyBoost);
            var root = UIManager.Instance.GetLayerRootByType(UILayer.Effect);
            var obj = GameObject.Instantiate(go4X, root);
            flyObj = obj;
            obj.transform.position = go4X.transform.position;
            obj.transform.localScale = go4X.transform.localScale;
            obj.transform.localEulerAngles = go4X.transform.localEulerAngles;
            // 隐藏原obj
            go4X.SetActive(false);
            goEff.SetActive(false);
            // 飞图标
            var seq = DOTween.Sequence();
            seq.SetDelay(flyDelay);
            seq.Append(obj.transform.DORotate(Vector3.zero, flyDuration));
            seq.Join(obj.transform.DOScale(Vector3.one * flyScaleCoe, flyDuration));
            seq.Join(obj.transform.DOMove(to, flyDuration).
                    SetEase(flyCurve).
                    OnComplete(() => EL.MessageCenter.Get<MSG.UI_ENERGY_BOOST_UNLOCK_FLY_FEEDBACK>().Dispatch()));
            seq.OnComplete(ResetFlyAnim);
            seq.Play();
        }
    }
}
