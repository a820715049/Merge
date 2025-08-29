using System.Collections;
using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIMineCartBoardBannerTip : UIBase
    {
        [SerializeField] private TextMeshProUGUI _desc;
        [SerializeField] private Animation _anim;
        [SerializeField] private Animation _maskAnim;
        [SerializeField] private UIImageRes _headIcon;
        [SerializeField] private UIImageRes _bg;
        private MineCartActivity _activity;
        protected override void OnCreate()
        {
            base.OnCreate();
            transform.Access("Root/Desc_txt", out _desc);
            transform.Access("Root", out _anim);
            transform.Access("Mask", out _maskAnim);
        }

        protected override void OnParse(params object[] items)
        {
            //Game.Manager.audioMan.TriggerSound("MineBanner");

            if (items.Length < 2) return;
            _activity = items[0] as MineCartActivity;
            var text = items[1] as string;
            _desc.text = text;

            _anim?.Play();
            _maskAnim?.Play();
        }
        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            _activity.VisualBanner.visual.Refresh(_desc, "text");
            _activity.VisualBanner.visual.Refresh(_headIcon, "head");
            _activity.VisualBanner.visual.Refresh(_bg, "bg");
            IEnumerator coroutine()
            {
                yield return new WaitForSeconds(2f);
                Close();
            }
            Game.Instance.StartCoroutineGlobal(coroutine());
        }
    }
}
