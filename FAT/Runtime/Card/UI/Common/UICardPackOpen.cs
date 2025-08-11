/*
 *@Author:chaoran.zhang
 *@Desc:
 *@Created Time:2024.01.26 星期五 20:01:37
 */

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EL;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FAT
{
    public class UICardPackOpen : UIBase
    {
        //临时暴露的接口，方便对动画效果进行调试
        public float interval = 0.1f; //时间间隔
        public float duration = 0.5f; //位移时长
        public float rotation = 30f; //卡牌初始旋转角度
        public float showScale = 0.5f; //卡牌出现时的缩放
        public float delay = 0.1f;
        public int num = 2;
        public GameObject lightNode;
        public float lightSpeed;

        [FormerlySerializedAs("CardPos")] [SerializeField]
        private GameObject cardPos; //与卡牌相同大小的占位用空物体

        [FormerlySerializedAs("CardTemplate")] [SerializeField]
        private GameObject cardTemplate;

        [FormerlySerializedAs("Distance1")] [SerializeField]
        private int distance1; //两张卡牌时水平排列组建中元素距顶部的长度

        [FormerlySerializedAs("Distance2")] [SerializeField]
        private int distance2; //三到四张卡牌时水平排列组建中元素距顶部的长度

        [FormerlySerializedAs("Distance3")] [SerializeField]
        private int distance3; //五到六卡牌时水平排列组建中元素距顶部的长度

        [FormerlySerializedAs("Distance4")] [SerializeField]
        private int distance4; //七到九卡牌时水平排列组建中元素距顶部的长度

        [FormerlySerializedAs("_cardPosNode")] [SerializeField]
        private Transform _cardPosTempNode;

        [SerializeField] private Transform _cardTempNode;
        [SerializeField] private GameObject _cardAlnumNode;
        [SerializeField] private TextMeshProUGUI _starNum;
        public readonly float HorSpaceNarrow = 42f;
        public readonly float HorSpaceWide = 66f;
        public readonly float LargeScale = 1.2f;

        public readonly float VerSpaceNarrow = 28f;
        public readonly float VerSpaceWide = 30f;
        private bool _block; //当前是否可以触发点击事件（播放动画时应当阻止点击）
        private List<CardAnimData> _cardAnimDataList;
        private List<int> _cardIdList;
        private Transform _cardNode;

        private int _cardNum;
        private SkeletonGraphic _cardPackAnim; //抽卡Spine动画

        private Animator _cardPackEffectAnimator; //抽卡特效动画控制器

        private Transform _cardStartPos;
        private List<Transform> _cardStartPosList;
        private Transform _cardTargetPos;
        private List<Transform> _cardTargetPosList;

        private AnimState _curState = AnimState.None;
        private List<int> _distanceList;
        private int _finishAnimCard = 0; //已完成抽卡动画的卡片数量
        private bool _hasLoaded = false; //是否已经完成了spine动画的加载
        private bool _hasShown = false; //是否已经开始播放开卡包动画
        private bool _lightFinish = false;
        private TextMeshProUGUI _openTips;
        private GameObject _spinePrefab;
        private int _totalStar;

        private void Update()
        {
            if (!_lightFinish && _curState == AnimState.WaitClose && _finishAnimCard == _cardAnimDataList.Count)
                foreach (var kv in _cardAnimDataList)
                    if (kv.Card.transform.Find("New").gameObject.activeSelf)
                    {
                        kv.Card.transform.GetChild(0).GetChild(1).GetChild(0).position = lightNode.transform.position;
                        kv.Card.transform.GetChild(0).GetChild(0).GetChild(0).position = lightNode.transform.position;
                    }
        }

        protected override void OnCreate()
        {
            _cardStartPos = transform.Find("Content/Panel/StartPos");
            _cardTargetPos = transform.Find("Content/Panel/TargetPos");
            _cardNode = transform.Find("Content/Panel/CardNode");
            transform.AddButton("BtnContinue", _ClickContinue);
            _openTips = transform.Find("Content/TxtClick").GetComponent<TextMeshProUGUI>();
        }

        protected override void OnPreOpen()
        {
            MessageCenter.Get<MSG.GAME_CARD_PACK_OPEN>().Dispatch();
        }

        protected override void OnParse(params object[] items)
        {
            _distanceList = new List<int>() { distance1, distance2, distance3, distance4 };
            StartCoroutine(_LoadSpine((int)items[1]));
            _cardIdList = new List<int>();
            foreach (var item in items[0] as List<int>) _cardIdList.Add(item);

            if (_cardIdList.Count == 0)
                for (var i = 0; i < num; i++)
                    _cardIdList.Add(220000001);

            _cardNum = _cardIdList.Count;
            _SetAllCardAnim();
            _openTips.text = I18N.Text("#SysComDesc110");
        }

        private IEnumerator _LoadSpine(int id)
        {
            var pack = Game.Manager.objectMan.GetCardPackConfig(id);
            if (pack != null)
            {
                //播放卡包显示音效
                Game.Manager.audioMan.TriggerSound("CardDisplay");
                var res = pack.Spine.ConvertToAssetConfig();
                var req = EL.Resource.ResManager.LoadAsset(res.Group, res.Asset);
                yield return req;
                if (req.isSuccess && req.asset != null)
                {
                    var obj = Instantiate(req.asset, transform.Find("Content/SpineNode")) as GameObject;
                    if (obj)
                    {
                        transform.Find("Content/SpineNode").gameObject.SetActive(false);
                        _cardPackEffectAnimator = obj.transform.Find("Ani").GetComponent<Animator>();
                        _cardPackAnim = obj.transform.GetChild(0).GetChild(3).GetComponent<SkeletonGraphic>();
                        if (_cardPackAnim == null)
                            _cardPackAnim = obj.transform.GetChild(0).GetChild(2).GetComponent<SkeletonGraphic>();
                        if (_spinePrefab != null)
                        {
                            DestroyImmediate(_spinePrefab);
                            _spinePrefab = null;
                        }

                        _spinePrefab = obj;
                        _hasLoaded = true;
                        _TryPlayCardPackAni();
                    }
                    else
                    {
                        DebugEx.Error($"UICardPackOpen Spine missing {pack.Spine}");
                    }
                }
                else
                {
                    _hasLoaded = true;
                    _TryPlayCardPackAni();
                }
            }
            else
            {
                //test
                _hasLoaded = true;
                _TryPlayCardPackAni();
                transform.Find("Content/SpineNode").localScale = Vector3.zero;
            }
        }

        protected override void OnPostOpen()
        {
            _lightFinish = false;
            _TryPlayCardPackAni();
        }

        private void _TryPlayCardPackAni()
        {
            if (IsOpening() || _hasShown || !_hasLoaded)
                return;

            _hasShown = true;
            transform.Find("Content/SpineNode").gameObject.SetActive(true);
            _curState = AnimState.Show;
            _block = true;
            _cardPackAnim.AnimationState.SetAnimation(0, "cardpack_show", false).Complete += delegate(TrackEntry entry)
            {
                _block = false;
                _cardPackAnim.AnimationState.SetAnimation(0, "cardpack_waitopen", true);
                _curState = AnimState.Idle;
            };
        }

        protected override void OnPostClose()
        {
            //卡牌位置节点与卡牌回收复用
            while (_cardNode.childCount > 0)
            {
                _cardNode.GetChild(0).gameObject.SetActive(false);
                _cardNode.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);
                _cardNode.GetChild(0).GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(true);
                _cardNode.GetChild(0).SetParent(_cardTempNode);
            }

            foreach (var kv in _cardTargetPosList) kv.SetParent(_cardPosTempNode);

            foreach (var kv in _cardStartPosList) kv.SetParent(_cardPosTempNode);

            _cardIdList.Clear();
            _cardAnimDataList.Clear();
            _cardStartPosList.Clear();
            _cardTargetPosList.Clear();
            _distanceList.Clear();
            _finishAnimCard = 0;
            _hasShown = false;
            _hasLoaded = false;

            if (_spinePrefab != null)
            {
                DestroyImmediate(_spinePrefab);
                _spinePrefab = null;
            }

            transform.Find("Content/SpineNode").gameObject.SetActive(false);
            transform.Find("Mask").GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            _openTips.gameObject.SetActive(true);

            //尝试执行之后的抽卡流程表现
            Game.Manager.cardMan.TryOpenPackDisplay();
            MessageCenter.Get<MSG.GAME_CARD_PACK_OPEN_END>().Dispatch();
        }

        private void _SetLayoutGroup(int num, bool isWide)
        {
            _cardTargetPos.GetComponent<VerticalLayoutGroup>().padding.top = _distanceList[num];
            _cardTargetPos.GetComponent<VerticalLayoutGroup>().spacing = isWide ? VerSpaceWide : VerSpaceNarrow;
            for (var i = 0; i < _cardTargetPos.transform.childCount; i++)
                _cardTargetPos.GetChild(i).GetComponent<HorizontalLayoutGroup>().spacing =
                    isWide ? HorSpaceWide : HorSpaceNarrow;
        }

        //index为行数
        private void _SetCardAnimData(int index, float targetScale, float startScale, float delay, float rota)
        {
            //卡牌最终落点
            GameObject target;
            GameObject start;
            if (_cardPosTempNode.childCount > 0)
            {
                target = _cardPosTempNode.GetChild(0).gameObject;
                target.transform.SetParent(_cardTargetPos.transform.GetChild(index));
            }
            else
            {
                target = Instantiate(cardPos, _cardTargetPos.transform.GetChild(index));
            }

            target.transform.localScale = targetScale * Vector3.one;
            _cardTargetPosList.Add(target.transform);
            //卡牌初始位置
            if (_cardPosTempNode.childCount > 0)
            {
                start = _cardPosTempNode.GetChild(0).gameObject;
                start.transform.SetParent(_cardStartPos.GetChild(index));
            }
            else
            {
                start = Instantiate(cardPos, _cardStartPos.GetChild(index));
            }

            start.transform.localScale = startScale * Vector3.one;
            start.transform.eulerAngles = new Vector3(0f, 0f, rota);
            _cardStartPosList.Add(start.transform);

            var data = new CardAnimData();
            data.SetDelayTime(delay);
            _cardAnimDataList.Add(data);
        }

        //根据卡牌数量，初始化所有卡牌、计算每张卡牌的抽卡动画最终位置并以CardAnimData的结构存储
        private void _SetAllCardAnim()
        {
            _cardTargetPosList = new List<Transform>();
            _cardStartPosList = new List<Transform>();
            _cardAnimDataList = new List<CardAnimData>();
            switch (_cardNum)
            {
                case 1:
                {
                    _SetLayoutGroup(0, true);
                    _SetCardAnimData(0, 1.6f, showScale, 0f, 0);
                    break;
                }
                case 2:
                {
                    _SetLayoutGroup(0, true);
                    for (var i = 0; i < _cardNum; i++)
                        if (i % 2 == 0)
                            _SetCardAnimData(0, LargeScale, showScale, 0f, -rotation);
                        else
                            _SetCardAnimData(0, LargeScale, showScale, 0f, rotation);
                    break;
                }
                case 3:
                case 4:
                {
                    _SetLayoutGroup(1, true);
                    for (var i = 0; i < _cardNum; i++)
                        if (i < 2)
                        {
                            if (i % 2 == 0)
                                _SetCardAnimData(0, LargeScale, showScale, 0f, -rotation);
                            else
                                _SetCardAnimData(0, LargeScale, showScale, 0f, rotation);
                        }
                        else
                        {
                            if (i % 2 == 0)
                                _SetCardAnimData(1, LargeScale, showScale, 0f, rotation);
                            else
                                _SetCardAnimData(1, LargeScale, showScale, 0f, -rotation);
                        }

                    break;
                }
                case 5:
                case 6:
                {
                    _SetLayoutGroup(2, false);
                    cardPos.transform.localScale = Vector3.one;
                    for (var i = 0; i < _cardNum; i++)
                        if (i < 3)
                        {
                            if (i % 2 == 0)
                                _SetCardAnimData(0, 1f, showScale, 0f, -rotation);
                            else
                                _SetCardAnimData(0, 1f, showScale, 0f, rotation);
                        }
                        else
                        {
                            if (i % 2 == 0)
                                _SetCardAnimData(1, 1f, showScale, 0f, rotation);
                            else
                                _SetCardAnimData(1, 1f, showScale, 0f, -rotation);
                        }

                    break;
                }
                case 7:
                case 8:
                case 9:
                {
                    _SetLayoutGroup(3, false);
                    cardPos.transform.localScale = Vector3.one;
                    for (var i = 0; i < _cardNum; i++)
                        if (i < 3)
                        {
                            if (i % 2 == 0)
                                _SetCardAnimData(0, 1f, showScale, 0f, -rotation);
                            else
                                _SetCardAnimData(0, 1f, showScale, 0f, rotation);
                        }
                        else if (i < 6)
                        {
                            if (i % 2 == 0)
                                _SetCardAnimData(1, 1f, showScale, 0f, rotation);
                            else
                                _SetCardAnimData(1, 1f, showScale, 0f, -rotation);
                        }
                        else
                        {
                            if (i % 2 == 0)
                                _SetCardAnimData(2, 1f, showScale, 0f, -rotation);
                            else
                                _SetCardAnimData(2, 1f, showScale, 0f, rotation);
                        }

                    break;
                }
            }

            _UpdateCardAnimDataList();
        }

        private void _UpdateCardAnimDataList()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_cardTargetPos.transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_cardStartPos.transform as RectTransform);
            _totalStar = 0;
            for (var i = 0; i < _cardIdList.Count; i++)
            {
                _cardAnimDataList[i].Card = _CreateCard(i);
                _cardAnimDataList[i].SetTarget(_cardTargetPosList[i]);
                _cardAnimDataList[i].SetStart(_cardStartPosList[i]);
            }

            _starNum.text = "+" + _totalStar;
        }

        private GameObject _CreateCard(int num)
        {
            GameObject instantiate;
            if (_cardTempNode.childCount > 0)
            {
                instantiate = _cardTempNode.GetChild(0).gameObject;
                instantiate.transform.SetParent(_cardNode);
            }
            else
            {
                instantiate = Instantiate(cardTemplate, _cardNode);
            }

            var albumData = Game.Manager.cardMan.GetCardAlbumData();
            if (albumData == null)
                return instantiate;
            var cardData = albumData.TryGetCardData(_cardIdList[num]);
            if (cardData == null)
                return instantiate;
            var cardConfig = cardData.GetConfig();
            if (cardConfig == null)
                return instantiate;
            var objBasicConfig = cardData.GetObjBasicConfig();
            if (objBasicConfig == null)
                return instantiate;

            var isOwn = cardData.IsOwn;
            if (cardData.OwnCount > 1)
                _totalStar += cardConfig.Star;
            var isGold = cardConfig.IsGold;
            instantiate.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = I18N.Text(objBasicConfig.Name);
            var fontResIndex = isOwn ? isGold ? 38 : 8 : isGold ? 39 : 8;
            if (isGold)
                instantiate.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(false);
            else
                instantiate.transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(false);
            var config = FontMaterialRes.Instance.GetFontMatResConf(fontResIndex);
            if (config != null)
            {
                var tmpText = instantiate.transform.Find("Name").GetComponent<TextMeshProUGUI>();
                if (tmpText != null)
                    config.ApplyFontMatResConfig(tmpText);
            }

            instantiate.transform.Find("Star").GetComponent<CardStar>().Setup(cardConfig.Star);
            instantiate.transform.Find("Icon").GetComponent<UIImageRes>().SetImage(objBasicConfig.Icon);
            if (cardData.OwnCount == 1)
                instantiate.transform.Find("New").gameObject.SetActive(true);
            else
                instantiate.transform.Find("New").gameObject.SetActive(false);

            return instantiate;
        }

        private void _PlayShowCardAnim()
        {
            foreach (var kv in _cardAnimDataList)
            {
                kv.Card.SetActive(true);
                StartCoroutine(DelayInvoke(kv.DelayTime, () =>
                {
                    kv.Card.transform.DOScale(kv.Target.localScale, duration);
                    kv.Card.transform.DOMove(kv.Target.position, duration).onComplete += () => { _finishAnimCard++; };
                    kv.Card.transform.DORotate(Vector3.zero, duration);
                }));
            }
        }

        private IEnumerator DelayInvoke(float delay, Action call)
        {
            yield return new WaitForSeconds(delay);
            call.Invoke();
        }

        private void _ClickContinue()
        {
            if (_block)
                return;
            if (_curState == AnimState.None)
                return;

            switch (_curState)
            {
                case AnimState.Show:
                {
                    break;
                }
                case AnimState.Idle:
                {
                    _block = true;
                    _cardPackAnim.AnimationState.SetAnimation(0, "cardpack_open", false).Complete +=
                        delegate(TrackEntry entry)
                        {
                            _cardPackAnim.AnimationState.SetAnimation(0, "cardpack_waitclose", true);
                            _cardPackEffectAnimator.SetBool("close", true);
                            _curState = AnimState.WaitClose;
                            _block = false;
                            _openTips.text = I18N.Text("#SysComDesc111");
                            lightNode.transform.position = new Vector3(-Screen.width - 628, Screen.height / 2 + 200, 0);
                            lightNode.transform.DOMove(new Vector3(Screen.width + 628, Screen.height / 2 + 200, 0),
                                lightSpeed).onComplete += () => { _lightFinish = true; };
                        };
                    //播放卡包打开音效
                    Game.Manager.audioMan.TriggerSound("CardOpen");
                    //StartCoroutine(DelayInvoke(0.1f, () => Game.Manager.audioMan.TriggerSound("CardOpen")));
                    StartCoroutine(DelayInvoke(delay, _PlayShowCardAnim));
                    _curState = AnimState.Open;
                    _cardPackEffectAnimator.SetBool("open", true);
                    break;
                }
                case AnimState.Open:
                {
                    break;
                }
                case AnimState.WaitClose:
                {
                    if (_finishAnimCard == _cardAnimDataList.Count)
                    {
                        //点击领取时的音效
                        Game.Manager.audioMan.TriggerSound("CardClick");

                        _block = true;

                        _openTips.gameObject.SetActive(false);

                        transform.Find("Mask").GetComponent<Image>().DOFade(0, 0.5f);
                        _spinePrefab.SetActive(false);
                        //地图按钮位置
                        var pos = UIManager.Instance.GetLayerRootByType(UILayer.BelowStatus).FindInChild("Adapter")
                            .GetComponent<MBBoardAreaAdapter>().JumpCardAlbumIconPos();
                        _cardAlnumNode.transform.position = pos;

                        _cardAlnumNode.GetComponent<Image>().DOFade(1, 0.3f).onComplete += () =>
                        {
                            if (Game.Manager.cardMan.IsOpenStarExchange() && _totalStar > 0)
                                _starNum.transform.parent.GetComponent<Animator>().SetTrigger("Punch");
                        };
                        StartCoroutine(DelayInvoke(0.6f, () =>
                        {
                            _cardAlnumNode.GetComponent<Image>().DOFade(0, 0.5f);
                            _cardAlnumNode.transform.DOMove(pos, 0.5f).onComplete += () =>
                            {
                                //收集到容器时的音效
                                Game.Manager.audioMan.TriggerSound("CardRecycle");
                                //所有动画播放结束
                                Close();
                            };
                        }));

                        foreach (var kv in _cardAnimDataList)
                        {
                            kv.Card.transform.DOMove(pos + new Vector3(-80, 80, 0), 0.5f);
                            kv.Card.transform.DOScale(0.1f * Vector3.one, 0.5f).onComplete += () =>
                            {
                                kv.Card.SetActive(false);
                            };
                        }
                    }

                    break;
                }
            }
        }

        private enum AnimState
        {
            None,
            Show,
            Idle,
            Open,
            WaitClose
        }
    }
}