/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏UI界面 
 * @Date: 2024-09-26 14:09:08
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FAT;
using EL;
using fat.rawdata;
using TMPro;

namespace MiniGame
{
    public class UIBeads : UIBase
    {
        [SerializeField] private float delayOpenTime;   //延迟打开结果界面
        [SerializeField] private TMP_Text levelName;
        [SerializeField] private List<GameObject> rootNodeList;
        [SerializeField] private GameObject beadRootPoolCell;
        [SerializeField] private GameObject beadPoolCell;
        [SerializeField] private UIBeadsDrag beadsDrag;
        [SerializeField] private RectTransform originPosTrans;
        [SerializeField] private Animator beadsAnimator;
        //特效相关
        [SerializeField] private GameObject beadRootEffectPoolCell;
        [SerializeField] private List<GameObject> effectNodeList;
        
        //串珠子小游戏数据类
        private MiniGameBeads _miniGameBeads;
        //加载协程
        private Coroutine _coroutine;
        //存一份当前所有珠子底座cell的List 方便查找与最后回收
        private List<UIBeadRootCell> _beadRootCellList = new();
        //存一份当前所有珠子底座Effect cell的List 方便查找与最后回收
        private List<UIBeadRootEffectCell> _beadRootEffectCellList = new();
        //存储一份当前所有珠子cell的dict key为数据上模拟的坐标 方便查找与最后回收
        private Dictionary<(int x, int y), UIBeadCell> _beadItemCellDict = new();
        //动画是否播放完成
        private bool _isPlayAnimFinish;
        //延迟打开协程
        private Coroutine _delayCoroutine;
        //是否在等待打开结算界面
        private bool _isWaitOpenResult;
        
        //拖拽相关
        private int _coordCellWidth = 170;    //坐标系中单位宽度
        private int _coordCellHeight = 164;   //坐标系中单位高度
        private UIBeadCell _curDragBead;     //目前正在拖拽的珠子(实际代码控制移动的珠子)
        private Vector2 _beadBeginPos;       //目前正在拖拽的珠子的起始位置
        //引导相关
        private bool _isGuide = false;    //目前是否是引导表现
        
        protected override void OnCreate()
        {
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MINIGAME_BEADS_ROOT_CELL, beadRootPoolCell);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MINIGAME_BEADS_CELL, beadPoolCell);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MINIGAME_BEADS_ROOT_EFFECT_CELL, beadRootEffectPoolCell);
            transform.AddButton("Content/BtnClose/Btn", _OnBtnCloseClick).FixPivot();
        }
        
        protected override void OnParse(params object[] items)
        {
            if (items.Length == 1)
            {
                _isGuide = (bool)items[0];
            }
            _miniGameBeads = MiniGameManager.Instance.CurMiniGame as MiniGameBeads;
        }

        protected override void OnPreOpen()
        {
            _StopCoroutine();
            levelName.text = I18N.FormatText("#SysComDesc606", _miniGameBeads?.Index + 1 ?? 1);
            if (_isGuide)
            {
                UIManager.Instance.OpenWindow(UIConfig.UIBeadsGuide);
                return;
            }
            _coroutine = Game.Instance.StartCoroutineGlobal(_CoPrepare());
        }
        
        //如果是引导关卡UiBeads会先打开但不执行相关表现，待到引导界面完成时会再次OpenWindow(UiBeads) 此时会走OnRefresh时机
        protected override void OnRefresh()
        {
            _StopCoroutine();
            _coroutine = Game.Instance.StartCoroutineGlobal(_CoPrepare());
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.MINIGAME_RESULT>().AddListener(_OnGameFinish);
            MessageCenter.Get<MSG.MINIGAME_BEADS_BASE_COMPLETE>().AddListener(_OnRootComplete);
            MessageCenter.Get<FAT.MSG.UI_SIMPLE_ANIM_FINISH>().AddListener(_OnAnimPlayEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.MINIGAME_RESULT>().RemoveListener(_OnGameFinish);
            MessageCenter.Get<MSG.MINIGAME_BEADS_BASE_COMPLETE>().RemoveListener(_OnRootComplete);
            MessageCenter.Get<FAT.MSG.UI_SIMPLE_ANIM_FINISH>().RemoveListener(_OnAnimPlayEnd);
        }

        protected override void OnPostClose()
        {
            _ReleaseCell();
            _StopCoroutine();
            _StopDelayCoroutine();
            _ClearDragInfo();
            beadsDrag.Clear();
            _miniGameBeads = null;
            _isGuide = false;
            _isPlayAnimFinish = false;
            _isWaitOpenResult = false;
            beadsAnimator.ResetTrigger("BeadsShow");
            MiniGameManager.Instance.EndCurMiniGame();  //游戏中途也可以直接退出游戏
        }

        private IEnumerator _CoPrepare()
        {
            if (_miniGameBeads == null)
                yield break;
            //根据数据初始化珠子布局
            yield return _CoPrepareBeadsInfo();
            //如果此时不是引导状态 则直接播放入场动画
            if (!_isGuide)
            {
                _isPlayAnimFinish = false;
                beadsAnimator.SetTrigger("BeadsShow");
            }
            yield return new WaitUntil(() => _isPlayAnimFinish);
            //统一在屏幕坐标系中计算
            var originPos = RectTransformUtility.WorldToScreenPoint(null, originPosTrans.position);
            var lossyScale = originPosTrans.lossyScale.x;
            beadsDrag.Init(originPos, _coordCellWidth * lossyScale, _coordCellHeight * lossyScale);
            beadsDrag.RegisterListener(_OnDragBegin, _OnDrag, _OnDragEnd);
        }

        //异步分帧加载初始串珠子小游戏盘面
        private IEnumerator _CoPrepareBeadsInfo()
        {
            var configMan = MiniGameConfig.Instance;
            var poolMan = GameObjectPoolManager.Instance;
            _beadRootCellList.Clear();
            _beadItemCellDict.Clear();
            _beadRootEffectCellList.Clear();
            var length = _miniGameBeads.BeadsBases.Count;
            for (int x = 0; x < rootNodeList.Count; x++)
            {
                rootNodeList[x].gameObject.SetActive(x < length);
            }
            for (int x = 0; x < effectNodeList.Count; x++)
            {
                effectNodeList[x].gameObject.SetActive(x < length);
            }
            yield return null;
            for (int x = 0; x < rootNodeList.Count; x++)
            {
                if (x >= length) break;
                var rootInfo = _miniGameBeads.BeadsBases[x];
                var rootConf = configMan.GetBeadsBase(rootInfo.ConfID);
                if (rootConf == null) continue;
                var rootCellGo = poolMan.CreateObject(PoolItemType.MINIGAME_BEADS_ROOT_CELL);
                if (rootCellGo == null) continue;
                _SetParent(rootCellGo.transform, rootNodeList[x].transform);
                //创建底座
                var beadRootCell = new UIBeadRootCell();
                beadRootCell.Prepare(rootCellGo.transform, rootConf.BaseImage);
                _beadRootCellList.Add(beadRootCell);
                //创建该底座中的珠子cell
                var curParent = beadRootCell.GetBeadNode();
                for (int y = 0; y < rootInfo.Beads.Count; y++)
                {
                    var beadInfo = rootInfo.Beads[y];
                    var beadConf = configMan.GetBeadsCell(beadInfo.ConfID);
                    if (beadConf == null) continue;
                    var beadCellGo = poolMan.CreateObject(PoolItemType.MINIGAME_BEADS_CELL, curParent);
                    if (beadCellGo == null) continue;
                    _SetParent(beadCellGo.transform, curParent);
                    var beadCell = new UIBeadCell();
                    var coord = (x, y);
                    beadCell.Prepare(beadCellGo.transform, beadConf.CellImage);
                    beadCell.SetCoord(coord);
                    _beadItemCellDict.Add(coord, beadCell);
                    curParent = beadCell.GetChildNode();    //更换当前parent 达到嵌套add child的效果
                }
                yield return null;
            }
            for (int x = 0; x < effectNodeList.Count; x++)
            {
                if (x >= length) break;
                var rootEffectGo = poolMan.CreateObject(PoolItemType.MINIGAME_BEADS_ROOT_EFFECT_CELL);
                if (rootEffectGo == null) continue;
                _SetParent(rootEffectGo.transform, effectNodeList[x].transform);
                //创建底座
                var rootEffectCell = new UIBeadRootEffectCell();
                rootEffectCell.Prepare(rootEffectGo.transform);
                _beadRootEffectCellList.Add(rootEffectCell);
            }
            yield return null;
        }

        private void _OnAnimPlayEnd(AnimatorStateInfo stateInfo)
        {
            if (stateInfo.IsName("UIBeads_ani_show"))
            {
                _isPlayAnimFinish = true;
            }
        }
        
        //回收珠子底座和珠子cell 珠子父子节点关系 倒序回收
        private void _ReleaseCell()
        {
            var poolMan = GameObjectPoolManager.Instance;
            for (int x = 0; x < _miniGameBeads.BeadsBases.Count; x++)
            {
                var rootInfo = _miniGameBeads.BeadsBases[x];
                //倒序回收
                for (int y = rootInfo.Beads.Count - 1; y >= 0; y--)
                {
                    if (_beadItemCellDict.TryGetValue((x, y), out var beadCell))
                        poolMan.ReleaseObject(PoolItemType.MINIGAME_BEADS_CELL, beadCell.GetRoot().gameObject);
                }
                if (_beadRootCellList.TryGetByIndex(x, out var rootCell))
                {
                    poolMan.ReleaseObject(PoolItemType.MINIGAME_BEADS_ROOT_CELL, rootCell.GetRoot().gameObject);
                }
                if (_beadRootEffectCellList.TryGetByIndex(x, out var rootEffectCell))
                {
                    rootEffectCell.SetEffectVisible(false);  //回收时隐藏特效
                    poolMan.ReleaseObject(PoolItemType.MINIGAME_BEADS_ROOT_EFFECT_CELL, rootEffectCell.GetRoot().gameObject);
                }
            }
            _beadRootCellList.Clear();
            _beadItemCellDict.Clear();
            _beadRootEffectCellList.Clear();
        }

        private void _StopCoroutine()
        {
            if (_coroutine != null)
                Game.Instance.StopCoroutineGlobal(_coroutine);
            _coroutine = null;
        }

        private void _SetParent(Transform child, Transform parent)
        {
            child.SetParent(parent);
            child.localScale = Vector3.one;
            child.localPosition = Vector3.zero;
        }
        
        private void _OnDragBegin(int x, int y, Vector2 beginDragPos)
        {
            _ClearDragInfo();
            if (_miniGameBeads == null) return;
            var startY = _miniGameBeads.OnPointerDown(x, y);
            //小于0说明没找到
            if (startY < 0) return;
            //找到实际要拖拽的点位
            if (!_beadItemCellDict.TryGetValue((x, startY), out var cell))
                return;
            _curDragBead = cell;
            _beadBeginPos = _curDragBead.GetRoot().position;
            beadsDrag.SetDragOffset(beginDragPos - _beadBeginPos);
        }
        
        private void _OnDrag(Vector2 deltaPos)
        {
            if (_miniGameBeads == null || _curDragBead == null) return;
            _curDragBead.GetRoot().position = _beadBeginPos + deltaPos;
        }
        
        //isSuccess 是否成功挂到目标位置
        private void _OnDragEnd(int baseIndex, int cellIndex)
        {
            if (_miniGameBeads == null || _curDragBead == null) return;
            var isSuccess = _miniGameBeads.OnPointerUp(baseIndex, cellIndex, out var x, out var y);
            //没有成功的话返回原位置
            if (!isSuccess)
            {
                _curDragBead.GetRoot().localPosition = Vector3.zero;
            }
            //如果成功了 则挂到新的父节点下
            else
            {
                //处理表现层 将目前拖拽的珠子串挂到合适的节点上
                if (y <= 0)
                {
                    //挂到基座的node节点上
                    if (_beadRootCellList.TryGetByIndex(x, out var rootCell))
                        _SetParent(_curDragBead.GetRoot(), rootCell.GetBeadNode());
                }
                else
                {
                    //挂到上方(y-1)珠子的node节点上
                    if (_beadItemCellDict.TryGetValue((x, y - 1), out var beadCell))
                        _SetParent(_curDragBead.GetRoot(), beadCell.GetChildNode());
                }
                _curDragBead.GetRoot().localPosition = Vector3.zero;  //重置位置
                //处理UI数据层 刷新dict
                var oldCoord = _curDragBead.GetCoord();
                //计算目前一共有多少个珠子在移动 这里使用数据层移动后的数据与移动前的目标坐标y做差值 得出珠子数量
                var moveCount = _miniGameBeads.BeadsBases[x].Beads.Count - y;
                //将原来dict中老坐标对应的key value移除  然后赋值到新的坐标key下面
                int tempX = oldCoord.Item1;
                int tempY = oldCoord.Item2;
                for (int i = 0; i < moveCount; i++)
                {
                    var tempOldCoord = (tempX, tempY + i);
                    if (_beadItemCellDict.Remove(tempOldCoord, out var tempCell))
                    {
                        var newCoord = (x, y + i);
                        tempCell.SetCoord(newCoord);
                        _beadItemCellDict.Add(newCoord, tempCell);
                    }
                }
                //将珠子接到新链条上时播音效
                Game.Manager.audioMan.TriggerSound("BeadCellMove");
            }
            _ClearDragInfo();
        }

        private void _ClearDragInfo()
        {
            _curDragBead = null;
            _beadBeginPos = Vector2.zero;
            beadsDrag.SetDragOffset(Vector2.zero);
        }

        private void _OnRootComplete(int rootIndex)
        {
            if (_isGuide) return;
            if (_beadRootEffectCellList.TryGetByIndex(rootIndex, out var rootEffectCell))
            {
                //基座完成时播特效
                rootEffectCell.SetEffectVisible(true);
                //基座完成时播音效
                Game.Manager.audioMan.TriggerSound("BeadRootSuccess");
            }
        }

        private void _OnBtnCloseClick()
        {
            if (_isGuide || _isWaitOpenResult) return;
            Game.Manager.commonTipsMan.ShowMessageTips(I18N.Text("#SysComDesc612"), I18N.Text("#SysComDesc611"), null, _OnSureLeave);
        }

        private void _OnSureLeave()
        {
            DataTracker.TrackMiniGameLeave(MiniGameType.MiniGameBeads, _miniGameBeads?.Index ?? 0, _miniGameBeads?.LevelID ?? 0);
            Close();
        }
        
        private void _OnGameFinish(int levelIndex, bool result)
        {
            if (_isGuide) return;
            _StopDelayCoroutine();
            _delayCoroutine = Game.Instance.StartCoroutineGlobal(_CoOpenBeadsResult(result));
        }

        private IEnumerator _CoOpenBeadsResult(bool result)
        {
            _isWaitOpenResult = true;
            yield return new WaitForSeconds(delayOpenTime);
            UIManager.Instance.OpenWindow(UIConfig.UIBeadsResult, result);
            _isWaitOpenResult = false;
        }
        
        private void _StopDelayCoroutine()
        {
            if (_delayCoroutine != null)
                Game.Instance.StopCoroutineGlobal(_delayCoroutine);
            _delayCoroutine = null;
        }
    }
}
