using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FAT.MSG;
using System.Collections.Generic;
using System;
using System.Collections;
using DG.Tweening;

namespace FAT
{
    using static EL.MessageCenter;

    public class UIActivityOrderDash : UIBase
    {
        public struct Node
        {
            public UIStateGroup icon;
            public Image locked;
            public GameObject tick;
            public Animator anim;
            public GameObject e1, e2;
        }

        public struct Bridge
        {
            public Image icon;
            public Animator anim;
        }

        internal TextMeshProUGUI cd;
        internal GameObject groupStart;
        internal GameObject objClose;
        internal Transform anchorPrize;
        internal MBRewardIcon prize;
        internal readonly List<Node> node = new();
        internal readonly List<Bridge> bridge = new();
        [SerializeField]
        private MapButton m_goBtn;//这个按钮不需要换皮，不改配置了.
        public UIVisualGroup visualGroup;
        public float convertDelay = 0.51f;
        public float bridgeDelay = 0f;
        public float unlockDelay = 0f;
        public float switchDelay = 0.51f;
        public GameObject goBlock;

        private ActivityOrderDash activity;
        private Action WhenTick;
        private Action<ActivityLike, bool> WhenEnd;

        private bool m_isStart = false;
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out visualGroup);
            var root = transform.Find("Content");
            visualGroup.Prepare(root.Access<UIImageRes>("bg"), "bgPrefab");
            visualGroup.Prepare(root.Access<UIImageRes>("bg1"), "titleBg");
            visualGroup.Prepare(root.Access<TextProOnACircle>("title"), "mainTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("desc"), "subTitle");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_cd/text"), "time");
            visualGroup.Prepare(root.Access<UIImageRes>("_cd/frame"), "time");
            visualGroup.Prepare(root.Access<UITextState>("_group/node1/icon/text"), "index");
            visualGroup.Prepare(root.Access<UITextState>("_group/node2/icon/text"), "index");
            visualGroup.Prepare(root.Access<UITextState>("_group/node3/icon/text"), "index");
            visualGroup.Prepare(root.Access<UITextState>("_group/node4/icon/text"), "index");
            visualGroup.Prepare(root.Access<UITextState>("_group/node5/icon/text"), "index");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_group/node5/text1"), "prize");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_group/node5/text2"), "prize");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_group/node5/text1"), "prize1");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_group/node5/text2"), "prize2");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_start/desc"), "desc1");
            visualGroup.Prepare(root.Access<TextMeshProUGUI>("_start/confirm/text"), "confirm");
            visualGroup.CollectTrim();
        }

        protected override void OnCreate()
        {
            var root = transform.Find("Content");
            root.Access("_cd/text", out cd);
            root.Access("_group/node5/entry", out prize);
            groupStart = root.TryFind("_start");
            anchorPrize = root.Find("_group/node5/prize");
            for (var k = 1; k <= 5; ++k)
            {
                var r = root.Find($"_group/node{k}");
                node.Add(ParseNode(r));
            }
            for (var k = 1; k < 5; ++k)
            {
                bridge.Add(ParseBridge(root.Find($"_group/b{k}")));
            }
            root.Access("close", out MapButton close);
            close.WhenClick = Close;
            objClose = close.gameObject;
            root.Access("_start/confirm", out MapButton confirm);
            confirm.WhenClick = ConfirmClick;
            WhenTick ??= RefreshCD;
            WhenEnd ??= RefreshEnd;
            m_goBtn = root.Access<MapButton>("go");
            m_goBtn.WhenClick = ConfirmClick;
            m_goBtn.WithClickScale();
        }


        private void ShowGoBtnWithAnim()
        {
            var btnTrans = m_goBtn.transform;
            btnTrans.localScale = Vector3.zero;
            m_goBtn.gameObject.SetActive(true);
            btnTrans.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        public static Node ParseNode(Transform root_)
        {
            return new()
            {
                icon = root_.Access<UIStateGroup>("icon"),
                locked = root_.Access<Image>("icon/lock"),
                tick = root_.TryFind("tick"),
                anim = root_.Access<Animator>(),
                e1 = root_.TryFind("fx_orderdash_unlock_broken"),
                e2 = root_.TryFind("fx_orderdash_convert_glow"),
            };
        }

        public static Bridge ParseBridge(Transform root_)
        {
            var icon = root_.Access<Image>();
            var sync = root_.Access<CloudMatSync>();
            var mat = GameObject.Instantiate(icon.material);
            sync.mat = mat;
            icon.material = mat;
            return new()
            {
                icon = icon,
                anim = root_.Access<Animator>(),
            };
        }

        protected override void OnParse(params object[] items)
        {
            activity = (ActivityOrderDash)items[0];
        }

        protected override void OnPreOpen()
        {
            goBlock.SetActive(false);
            RefreshTheme();
            RefreshCD();
            RefreshReward();
            RefreshStart();
            RefreshNode();
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnPreClose()
        {
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            Get<ACTIVITY_END>().RemoveListener(WhenEnd);
            Get<ACTIVITY_REFRESH>().Dispatch(activity);
        }

        internal void RefreshEnd(ActivityLike acti_, bool expire_)
        {
            if (acti_ != activity || !expire_) return;
            Close();
        }

        public void RefreshTheme()
        {
            var visual = activity.Visual;
            visual.Refresh(visualGroup);
            visual.RefreshText(visualGroup, "subTitle", activity.orderTotal);
        }

        public void RefreshCD()
        {
            var diff = (long)Mathf.Max(0, activity.Countdown);
            UIUtility.CountDownFormat(cd, diff);
        }

        public void RefreshReward()
        {
            prize.RefreshInfo(activity.reward[0].Id);
        }

        public void RefreshStart()
        {
            m_isStart = !activity.start && activity.orderIndex == 0;
            groupStart.SetActive(m_isStart);
            objClose.SetActive(!m_isStart);
            activity.start = true;
        }

        public void RefreshNode()
        {
            var s = activity.visualIndex;
            var index = activity.orderIndex;
            activity.visualIndex = index;
            void R(int index)
            {
                for (var k = 0; k < node.Count; ++k)
                {
                    RefreshNode(k, index);
                }
                for (var k = 0; k < bridge.Count; ++k)
                {
                    var b = bridge[k];
                    var complete = k < index;
                    var a = complete ? "UIOrderdashRainbow_Idle" : "Hide";
                    b.anim.Play(a);
                }
            }
            if (s < index)
            {
                R(s);
                Animate(s, index);
                m_goBtn.gameObject.SetActive(false);
                return;
            }
            R(index);
            m_goBtn.gameObject.SetActive(!m_isStart);
        }

        public void RefreshNode(int k, int index)
        {
            var n = node[k];
            var unlock = k > index;
            var complete = k < index;
            n.anim.enabled = false;
            n.icon.Enabled(!unlock);
            n.icon.gameObject.SetActive(!complete);
            n.icon.transform.localScale = Vector3.one;
            n.locked.gameObject.SetActive(unlock);
            n.locked.color = Color.white;
            n.locked.transform.localScale = Vector3.one;
            n.tick.SetActive(complete);
            n.tick.transform.localScale = Vector3.one;
            n.e1.SetActive(false);
            n.e2.SetActive(false);
        }

        public void RefreshNodeA(int k, int index)
        {
            var n = node[k];
            var unlock = k > index;
            var complete = k < index;
            n.icon.Enabled(!unlock);
            n.icon.gameObject.SetActive(!complete);
            n.locked.gameObject.SetActive(unlock);
            n.tick.SetActive(complete);
        }

        public void Animate(int s, int index)
        {
            IEnumerator R()
            {
                goBlock.SetActive(true);
                var c = Mathf.Min(index + 1, node.Count);
                activity.visualIndex = index;
                for (var k = s; k < c; ++k)
                {
                    var n = node[k];
                    var unlock = k > index;
                    var complete = k < index;
                    var unlocking = k == index;
                    var a = k switch
                    {
                        _ when unlocking => "UIOrderdashNode_Convert01",
                        _ when complete => "UIOrderdashNode_Convert02",
                        _ when !unlock => "UIOrderdashNode_Idle",
                        _ => "None",
                    };
                    n.anim.enabled = true;
                    n.anim.Play(a);
                    var d = n.anim.GetCurrentAnimatorStateInfo(0).length;
                    DOVirtual.DelayedCall(convertDelay, () => RefreshNodeA(k, index));
                    if (unlocking) DOVirtual.DelayedCall(unlockDelay, () => Game.Manager.audioMan.TriggerSound("OrderDashUnlock"));
                    if (complete) DOVirtual.DelayedCall(switchDelay, () => Game.Manager.audioMan.TriggerSound("OrderDashSwitch"));
                    yield return new WaitForSeconds(d);
                    if (complete && k < bridge.Count)
                    {
                        var b = bridge[k];
                        b.anim.Play("UIOrderdashRainbow_Punch");
                        d = b.anim.GetCurrentAnimatorStateInfo(0).length;
                        DOVirtual.DelayedCall(bridgeDelay, () => Game.Manager.audioMan.TriggerSound("OrderDashLevelip"));
                        yield return new WaitForSeconds(d);
                    }
                }
                if (activity.cache.Valid)
                {
                    yield return StartCoroutine(RR());
                }
                else
                {
                    if (!m_goBtn.gameObject.activeSelf)
                    {
                        ShowGoBtnWithAnim();
                    }
                }
                goBlock.SetActive(false);
            }
            IEnumerator RR()
            {
                var pos = anchorPrize.position;
                var cache = activity.cache;
                UIFlyUtility.FlyRewardList(cache.obj, pos);
                cache.Free();
                yield return new WaitForSeconds(1.2f);
                Close();
            }
            StartCoroutine(R());
        }

        public void ConfirmClick()
        {
            GameProcedure.SceneToMerge();
            Close();
        }
    }
}
