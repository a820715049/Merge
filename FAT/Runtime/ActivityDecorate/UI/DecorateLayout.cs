/*
 *@Author:chaoran.zhang
 *@Desc:装饰品组
 *@Created Time:2024.05.23 星期四 17:14:32
 */

using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static fat.conf.Data;

namespace FAT
{
    public class DecorateLayout : MonoBehaviour
    {
        [SerializeField] private GameObject temp;
        public int font_complete;
        public int font_noamal;
        private Transform _root;
        private UIImageState _bg;
        private MBRewardIcon _reward1;
        private MBRewardIcon _reward2;
        private TextMeshProUGUI _desc;
        private TextMeshProUGUI _level;
        private TextMeshProUGUI _desc2;
        private int _id = -1;
        private List<DecorateCell> _list = new List<DecorateCell>();
        private bool _complete;
        private bool _active;

        public void Init(int id)
        {
            SetID(id);
            InitComp();
            InitInfo();
            InitCell();
        }

        private void SetID(int id)
        {
            _id = id;
            _active = Game.Manager.decorateMan.CheckLevelActive(_id);
            _complete = Game.Manager.decorateMan.CheckLevelComplete(_id);
        }

        private void InitComp()
        {
            _root = transform.Find("Layout");
            _bg = transform.Find("Top/Reward/Bg").GetComponent<UIImageState>();
            _reward1 = transform.Find("Top/Reward/Bg/Reward1").GetComponent<MBRewardIcon>();
            _reward2 = transform.Find("Top/Reward/Bg/Reward2").GetComponent<MBRewardIcon>();
            _desc = transform.Find("Top/Level/Desc").GetComponent<TextMeshProUGUI>();
            _desc2 = transform.Find("Top/Level/complete").GetComponent<TextMeshProUGUI>();
            _level = transform.Find("Top/Level/Level").GetComponent<TextMeshProUGUI>();
        }

        private void InitInfo()
        {
            var conf = GetEventDecorateLevel(_id);
            _desc.text = I18N.FormatText("#SysComDesc390", ' ');
            _desc2.text = I18N.Text("#SysComDesc391");
            _level.text = (Game.Manager.decorateMan.Activity.CurGroupConf.IncludeLvId.IndexOf(_id) + 1).ToString();
            _bg.Select(_complete ? 1 : 0);
            var c = FontMaterialRes.Instance.GetFontMatResConf(_complete ? font_complete : font_noamal);
            if (c != null)
            {
                c.ApplyFontMatResConfig(_reward1.count);
                c.ApplyFontMatResConfig(_reward2.count);
            }

            _desc2.gameObject.SetActive(_complete);
            RefreshReward(conf);
        }

        private void RefreshReward(EventDecorateLevel conf)
        {
            switch (conf.Reward.Count)
            {
                case 0:
                {
                    DebugEx.Error("DecorateLevel's reward is null,id:{0}" + _id);
                    break;
                }
                case 1:
                {
                    _reward1.gameObject.SetActive(true);
                    _reward2.gameObject.SetActive(false);
                    var reward = conf.Reward[0].ConvertToInt3();
                    _reward1.Refresh(reward.Item1,
                        Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(reward.Item2, reward.Item3));
                    break;
                }
                case 2:
                {
                    _reward1.gameObject.SetActive(true);
                    _reward2.gameObject.SetActive(true);
                    var reward = conf.Reward[0].ConvertToInt3();
                    var num1 = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(reward.Item2, reward.Item3);
                    _reward1.Refresh(reward.Item1, num1);
                    reward = conf.Reward[1].ConvertToInt3();
                    var num2 = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(reward.Item2, reward.Item3);
                    _reward2.Refresh(reward.Item1, num2);
                    if (num1 > 1 && num2 > 1)
                    {
                        _reward1.count.fontSizeMax = 50f;
                        _reward2.count.fontSizeMax = 50f;
                    }

                    break;
                }
            }

            IEnumerator enumerator()
            {
                yield return null;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_reward1.transform.parent as RectTransform);
            }

            Game.Instance.StartCoroutineGlobal(enumerator());
        }

        private void InitCell()
        {
            var conf = GetEventDecorateLevel(_id);
            foreach (var kv in conf.DecorateID)
            {
                var obj = Instantiate(temp, _root);
                obj.SetActive(true);
                var cell = obj.GetComponent<DecorateCell>();
                cell.Init(kv, _active);
                _list.Add(cell);
            }
        }

        public void Refresh()
        {
            if (_complete != Game.Manager.decorateMan.CheckLevelComplete(_id))
            {
                _complete = !_complete;
                Game.Manager.decorateMan.AnimList.Add((2, PlayComplete));
            }

            RefreshCell();
        }

        private void PlayComplete()
        {
            var list = Game.Manager.decorateMan.LevelReward;
            if (!UIFlyFactory.CheckNeedFlyIcon(list[0].rewardId) || !UIFlyFactory.CheckNeedFlyIcon(list[1].rewardId))
            {
                UIFlyUtility.FlyReward(list[0], _reward1.transform.position, null, 66f);
                UIFlyUtility.FlyReward(list[1], _reward2.transform.position, null, 66f);
                UIManager.Instance.CloseWindow(Game.Manager.decorateMan.Panel);
                Game.Manager.decorateMan.RegisterWaitAction(() =>
                    UIManager.Instance.OpenWindow(Game.Manager.decorateMan.Panel));
            }
            else
            {
                UIFlyUtility.FlyReward(list[0], _reward1.transform.position,
                    MessageCenter.Get<MSG.DECORATE_ANIM_END>().Dispatch,
                    66f); //只需要回调一次即可
                UIFlyUtility.FlyReward(list[1], _reward2.transform.position, null, 66f);
                if (CheckShowGame(list[0].rewardId) || CheckShowGame(list[1].rewardId))
                    (UIManager.Instance.TryGetUI(UIConfig.UIDecorateRes) as UIDecorateRes)?.ShowGameNode();
                if (CheckShowRes(list[0].rewardId) || CheckShowRes(list[1].rewardId))
                    (UIManager.Instance.TryGetUI(UIConfig.UIDecorateRes) as UIDecorateRes)?.ShowOtherRes();
            }

            _bg.Select(_complete ? 1 : 0);
            var c = FontMaterialRes.Instance.GetFontMatResConf(_complete ? font_complete : font_noamal);
            if (c != null)
            {
                c.ApplyFontMatResConfig(_reward1.count);
                c.ApplyFontMatResConfig(_reward2.count);
            }
            _desc2.gameObject.SetActive(_complete);
            Game.Manager.decorateMan.LevelReward.Clear();
        }

        private void RefreshCell()
        {
            _active = Game.Manager.decorateMan.CheckLevelActive(_id);
            foreach (var kv in _list)
            {
                kv.Refresh(_active);
            }
        }

        private bool CheckShowGame(int id)
        {
            if (id == Constant.kMergeEnergyObjId)
                return false;
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.Coin))
                return false;
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.ActivityToken))
                return false;
            return true;
        }

        private bool CheckShowRes(int id)
        {
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.Coin))
                return true;
            if (id == Constant.kMergeEnergyObjId)
                return true;
            return false;
        }

        public Transform FindFirstEnable()
        {
            foreach (var kv in _list)
            {
                if (kv.IsEnable())
                    return kv.GetButtonTrans();
            }

            return null;
        }
    }
}