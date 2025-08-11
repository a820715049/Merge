/*
 * @Author: tang.yan
 * @Description: 串珠子小游戏UI界面中的引导界面
 * @Date: 2024-10-27 12:48:29
 */

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using FAT;
using EL;
using fat.rawdata;

namespace MiniGame
{
    public class UIBeadsGuide : UIBase
    {
        [SerializeField] private List<GameObject> rootNodeList;
        [SerializeField] private GameObject guideRoot;

        //串珠子小游戏数据类
        private MiniGameBeads _miniGameBeads;
        //加载协程
        private Coroutine _coroutine;
        //存一份当前所有珠子底座cell的List 方便查找与最后回收
        private List<UIBeadRootCell> _beadRootCellList = new();
        //存储一份当前所有珠子cell的dict key为数据上模拟的坐标 方便查找与最后回收
        private Dictionary<(int x, int y), UIBeadCell> _beadItemCellDict = new();
        //目前正在拖拽的珠子(实际代码控制移动的珠子)
        private UIBeadCell _curDragBead;     
        private bool _isPlaying;
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", _OnBtnCloseClick).FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            _miniGameBeads = MiniGameManager.Instance.CurMiniGame as MiniGameBeads;
        }

        protected override void OnPreOpen()
        {
            _StopCoroutine();
            guideRoot.SetActive(false);
            _coroutine = Game.Instance.StartCoroutineGlobal(_CoPrepareAndPlay());
        }

        protected override void OnAddListener() { }

        protected override void OnRemoveListener() { }

        protected override void OnPostClose()
        {
            _ReleaseCell();
            _StopCoroutine();
            _isPlaying = false;
            _curDragBead = null;
            var curLevelId = _miniGameBeads.Index;
            _miniGameBeads = null;
            //结束当前引导game
            MiniGameManager.Instance.EndCurMiniGame();
            //开始当前关卡真正的game
            MiniGameManager.Instance.TryStartMiniGame(MiniGameType.MiniGameBeads, curLevelId);
        }

        private IEnumerator _CoPrepareAndPlay()
        {
            if (_miniGameBeads == null)
                yield break;
            _isPlaying = true;
            yield return _CoPrepareBeadsInfo();
            yield return _CoPlayGuide();
        }

        //异步分帧加载初始串珠子小游戏盘面
        private IEnumerator _CoPrepareBeadsInfo()
        {
            var configMan = MiniGameConfig.Instance;
            var poolMan = GameObjectPoolManager.Instance;
            _beadRootCellList.Clear();
            _beadItemCellDict.Clear();
            var length = _miniGameBeads.BeadsBases.Count;
            for (int x = 0; x < rootNodeList.Count; x++)
            {
                rootNodeList[x].gameObject.SetActive(x < length);
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
            }
            yield return null;
        }

        private IEnumerator _CoPlayGuide()
        {
            var guideInfo = _miniGameBeads.BeadGuides;
            var count = guideInfo.Count;
            if (count <= 0) yield break;
            var waitTime = 1f;
            //初始时先停顿一下
            yield return new WaitForSeconds(waitTime / 2);
            for (int index = 0; index < count; index++)
            {
                var guideAct = guideInfo[index];
                var startX = guideAct.Start.Item1;
                var startY = guideAct.Start.Item2;
                var endX = guideAct.End.Item1;
                var endY = guideAct.End.Item2;
                startY = _miniGameBeads.OnPointerDown(startX, startY);
                //y值不合法或根据坐标没有找到实际要拖拽的点位时都停止
                if (startY < 0 || !_beadItemCellDict.TryGetValue((startX, startY), out var cell))
                    yield break;
                //落点不成功时停止
                var isSuccess = _miniGameBeads.OnPointerUp(endX, endY, out var x, out var y);
                if (!isSuccess)
                    yield break;
                Transform parentRoot = null;
                //处理表现层 将目前拖拽的珠子串挂到合适的节点上
                if (y <= 0)
                {
                    //挂到基座的node节点上
                    if (_beadRootCellList.TryGetByIndex(x, out var rootCell)) parentRoot = rootCell.GetBeadNode();
                }
                else
                {
                    //挂到上方(y-1)珠子的node节点上
                    if (_beadItemCellDict.TryGetValue((x, y - 1), out var beadCell)) parentRoot = beadCell.GetChildNode();
                }
                //找不到要挂的父节点时返回
                if (parentRoot == null) yield break;
                //设置目前要拖拽的cell
                _curDragBead = cell;
                _curDragBead.SetGuideHandVisible(true);
                //使用tween动画执行表现
                _curDragBead.GetRoot().DOMove(parentRoot.position, waitTime).SetEase(Ease.Linear);
                yield return new WaitForSeconds(waitTime);
                //动画表现完后设置父节点并重置位置
                _SetParent(_curDragBead.GetRoot(), parentRoot);
                _curDragBead.GetRoot().localPosition = Vector3.zero;  //重置位置
                //刷新数据层
                _RefreshCellDict(x, y);
                //相关数据重置
                _curDragBead.SetGuideHandVisible(false);
                _curDragBead = null;
                //一次表现完后再等一会
                yield return new WaitForSeconds(waitTime);
            }
            guideRoot.SetActive(true);
            _isPlaying = false;
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
                if (_beadRootCellList.TryGetByIndex(x, out var rootTrans))
                    poolMan.ReleaseObject(PoolItemType.MINIGAME_BEADS_ROOT_CELL, rootTrans.GetRoot().gameObject);
            }
            _beadRootCellList.Clear();
            _beadItemCellDict.Clear();
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

        private void _OnBtnCloseClick()
        {
            if (_isPlaying) return;
            Close();
        }

        private void _RefreshCellDict(int x, int y)
        {
            if (_miniGameBeads == null || _curDragBead == null) return;
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
        }
    }
}