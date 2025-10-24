// ===================================================
// Author: mengqc
// Date: 2025/09/10
// ===================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BezierSolution;
using EL;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FAT
{
    public class VineLeapPlayerManager
    {
        public int Count => _showList.Count;
        public ActivityVineLeap activity => _view.activity;
        private RectTransform _showNode;
        private RectTransform _hideNode;
        private MBVineLeapPlayer _me;
        private readonly List<MBVineLeapPlayer> _showList = new();
        private readonly List<MBVineLeapPlayer> _hideList = new();
        private UIVineLeapMain _view;
        private UnityEngine.Pool.ObjectPool<BezierSpline> _splinePool;

        public void Init(int num, UIVineLeapMain view)
        {
            _view = view;
            _splinePool = new UnityEngine.Pool.ObjectPool<BezierSpline>(
                () => Object.Instantiate(_view.spline, _view.playerMoveContent).GetComponent<BezierSpline>(),
                (item) => item.gameObject.SetActive(true),
                (item) => item.gameObject.SetActive(false),
                (item) => Object.Destroy(item.gameObject)
            );
            _hideNode = _view.hideRoot;
            _me = _view.playerObj.GetComponent<MBVineLeapPlayer>();
            _me.Init(true);
            for (var i = 0; i < num; i++)
            {
                var temp = Object.Instantiate(_view.playerObj, _hideNode);
                _hideList.Add(temp.GetComponent<MBVineLeapPlayer>());
            }

            foreach (var player in _hideList)
            {
                player.Init();
            }
        }

        public void InitPlayers(int lvIndex, bool isRunning)
        {
            ResetPlayers();
            var item = _view.vineScrollList.Item[lvIndex];
            _showNode = item.standPos;
            if (isRunning)
            {
                var levelCfg = activity.GetLevelConf(lvIndex);
                var seatsNum = levelCfg.Avatar;
                for (var i = 0; i < seatsNum - 1; i++)
                {
                    _showList.Add(_hideList.First());
                    _hideList.RemoveAt(0);
                }

                for (var i = _showList.Count - 1; i >= 0; i--)
                {
                    var player = _showList[i];
                    player.transform.SetParent(_showNode);
                    player.transform.localPosition = GetPlayerPosition(_showNode, lvIndex, i);
                }
            }

            _me.transform.SetParent(_showNode);
            _me.transform.localPosition = GetPlayerPosition(_showNode, lvIndex);
            if (isRunning)
            {
                _me.PlayIdle();
            }
            else
            {
                _me.PlaySleep();
            }
        }

        public IEnumerator MoveToNextLevel(int nextLvIndex, int rank, float delay = 0f)
        {
            UIManager.Instance.Block(true);
            _view.vineScrollList.scrollRect.vertical = false;
            var group = activity.GetCurGroupConf();
            var isToMilestone = nextLvIndex >= group.LevelId.Count;
            var nextContent = isToMilestone ? _view.milestoneStandPos.standPos : _view.vineScrollList.Item[nextLvIndex].standPos;
            var preLeapItem = _view.vineScrollList.Item[nextLvIndex - 1];
            var nextLeapItem = isToMilestone ? null : _view.vineScrollList.Item[nextLvIndex];
            preLeapItem.imgComplete.SetActive(false);
            // 处理淘汰玩家
            var outNum = GetEliminationCount(nextLvIndex - 1, rank);
            var outList = new List<MBVineLeapPlayer>();
            for (var j = 0; j < outNum && _showList.Count > 0; j++)
            {
                outList.Add(_showList[^1]);
                _showList.RemoveAt(_showList.Count - 1);
            }

            for (var i = _showList.Count - 1; i >= 0; i--)
            {
                var player = _showList[i];
                player.transform.SetParent(_view.playerMoveContent);
            }

            _me.transform.SetParent(_view.playerMoveContent);

            Action ReleaseSpine(BezierSpline sp)
            {
                return () => _splinePool.Release(sp);
            }

            // 播放跳跃音效
            // Game.Manager.audioMan.TriggerSound("VineLeapJump");
            var spline = _splinePool.Get();
            UIVineLeapUtil.Reshape(spline, _me.transform.position, GetPlayerPosition(null, nextLvIndex, 0));
            Game.StartCoroutine(_me.MoveNext(spline, delay, ReleaseSpine(spline)));

            // 移动剩余玩家
            for (var i = 0; i < _showList.Count; i++)
            {
                var player = _showList[i];
                var playerDelay = delay + (i + 1) * 0.1f;
                spline = _splinePool.Get();
                UIVineLeapUtil.Reshape(spline, player.transform.position, GetPlayerPosition(null, nextLvIndex, i + 1));
                Game.StartCoroutine(player.MoveNext(spline, playerDelay, ReleaseSpine(spline)));
            }

            if (nextLeapItem)
            {
                Game.StartCoroutine(nextLeapItem.PlayPunch(0));
            }

            yield return new WaitForSeconds(delay + (_showList.Count + 1) * 0.1f + 1f);

            // 处理淘汰玩家动画
            for (var i = 0; i < outList.Count; i++)
            {
                var eliminatedPlayer = outList[i];
                Game.StartCoroutine(eliminatedPlayer.EliminateAnim(i * 0.1f, _hideNode));
            }

            _hideList.AddRange(outList);
            Game.StartCoroutine(preLeapItem.PlayComplete(0));

            // 动画结束后放到容器中
            yield return new WaitForSeconds(2f);
            UIManager.Instance.Block(false);
            for (var i = _showList.Count - 1; i >= 0; i--)
            {
                var player = _showList[i];
                player.transform.SetParent(nextContent);
            }

            _me.transform.SetParent(nextContent);
            _view.vineScrollList.scrollRect.vertical = true;
            _view.UpdateStateArea();
            if (isToMilestone)
            {
                var chestIcon = activity.GetChestIcon();
                UIManager.Instance.OpenWindow(UIConfig.UIActivityReward, _view.imgChest.transform.position, activity.FinalRewardWaitCommit, chestIcon, I18N.Text("#SysComDesc1730"));

                void OnMilestoneEnd()
                {
                    // 弹出结束框
                    MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLOSE>().RemoveListener(OnMilestoneEnd);
                    _view.Close();
                }

                MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLOSE>().AddListener(OnMilestoneEnd);
            }
            else
            {
                InitPlayers(nextLvIndex, false);
                var levelCfg = activity.GetLevelConf(nextLvIndex);
                if (levelCfg.RewardId > 0)
                {
                    // 领奖后弹出过关
                    activity.VisualLevelReward.res.ActiveR.Open(activity.LevelRewardWaitCommit, new Action(() => { activity.VisualPass.res.ActiveR.Open(activity); }));
                }
                else
                {
                    // 直接弹出过关
                    activity.VisualPass.res.ActiveR.Open(activity);
                }
            }
        }

        public void ResetPlayers()
        {
            foreach (var player in _showList)
            {
                player.transform.SetParent(_hideNode);
                _hideList.Add(player);
            }

            _showList.Clear();
            _me.transform.SetParent(_hideNode);
        }

        public IEnumerator ShowLevelStart(float delay = 0)
        {
            InitPlayers(activity.CurLevel, true);
            for (var i = 0; i < _showList.Count; i++)
            {
                var player = _showList[i];
                Game.StartCoroutine(player.SpawnAnime(delay + i * 0.1f));
            }

            yield return new WaitForSeconds(delay);
            Game.Manager.audioMan.TriggerSound("VineLeapLevelStart");

            yield return new WaitForSeconds(_showList.Count * 0.1f + 0.1f);
        }

        private Vector3 GetPlayerPosition(RectTransform content, int lvIndex, int index = 0)
        {
            var group = activity.GetCurGroupConf();
            var isToMilestone = lvIndex >= group.LevelId.Count;
            return isToMilestone ? _view.milestoneStandPos.GetStandPos(content, index) : _view.vineScrollList.GetPlayerLocalPosition(content, lvIndex, index);
        }

        // 根据关卡等级和过关时的排行确定淘汰人员数量
        private int GetEliminationCount(int lvIndex, int rank)
        {
            var rankKey = rank - 1;
            var lvCfg = activity.GetLevelConf(lvIndex);
            foreach (var pair in lvCfg.AdvAvatar)
            {
                var strArray = pair.Value.Split('=');
                if (rankKey >= pair.Key && rankKey < strArray[0].ConvertToInt())
                {
                    return lvCfg.Avatar - strArray[1].ConvertToInt();
                }
            }

            return 0;
        }
    }
}