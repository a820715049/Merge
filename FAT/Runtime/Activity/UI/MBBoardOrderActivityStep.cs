/*
 * @Author: qun.chao
 * @Date: 2024-03-25 19:06:25
 */
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    /// <summary>
    /// 阶梯活动订单附加脚本
    /// 订单可提交时 特效激活
    /// 订单出现时 显示进度
    /// </summary>
    public class MBBoardOrderActivityStep : MonoBehaviour
    {
        [SerializeField] private MBBoardOrder orderInst;
        [SerializeField] private GameObject[] effects;
        [SerializeField] private GameObject progressGo;
        [SerializeField] private RectTransform progressMask;
        [SerializeField] private TMPro.TextMeshProUGUI progressText;
        [SerializeField] private Button btnGoToActivity;

        private bool mEffectShowing;

        private void Awake()
        {
            btnGoToActivity.onClick.AddListener(_OnBtnGoToActivity);
        }

        private void OnEnable()
        {
            _Refresh();
        }

        private void Update()
        {
            if (_ShouldShowEffect(orderInst.data))
            {
                if (!mEffectShowing)
                {
                    _RefreshEffect();
                }
            }
            else
            {
                if (mEffectShowing)
                {
                    _RefreshEffect();
                }
            }
        }

        private bool _ShouldShowEffect(IOrderData order)
        {
            if (orderInst.data == null)
                return false;
            return order.State == OrderState.Rewarded || order.State == OrderState.Finished;
        }

        private void _Refresh()
        {
            _RefreshProgress();
            _RefreshEffect();
        }

        private void _RefreshProgress()
        {
            var act = Game.Manager.activity.LookupAny(fat.rawdata.EventType.Step) as ActivityStep;
            if (act == null)
            {
                progressGo.SetActive(false);
            }
            else
            {
                var total = (progressMask.parent as RectTransform).rect.width;
                var cur = total * act.TaskIndex / act.list.Count;
                progressMask.sizeDelta = new Vector2(cur, progressMask.sizeDelta.y);
                progressText.text = $"{act.TaskIndex}/{act.list.Count}";
                progressGo.SetActive(true);
            }
        }

        private void _RefreshEffect()
        {
            mEffectShowing = _ShouldShowEffect(orderInst.data);
            foreach (var eff in effects)
            {
                if (eff != null)
                {
                    eff.SetActive(mEffectShowing);
                }
            }
        }

        private void _OnBtnGoToActivity()
        {
            var act = Game.Manager.activity.LookupAny(fat.rawdata.EventType.Step) as ActivityStep;
            if (act != null)
            {
                act.Open();
            }
        }
    }
}