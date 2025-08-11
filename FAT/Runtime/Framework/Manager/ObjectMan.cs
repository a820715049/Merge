/**
 * @Author: handong.liu
 * @Date: 2020-08-27 16:46:01
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using Config;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    public class ObjFullConfig
    {
        public readonly int id;
        public readonly ObjBasic basicConfig;
        public readonly ObjItemConfig itemConfig;
        public readonly ObjRandomChest randomBoxConfig;
        public readonly ObjMergeItem mergeItemConfig;
        public ObjFullConfig(int _id, ObjectMan objMan)
        {
            id = _id;
            basicConfig = objMan.GetBasicConfig(_id);
            itemConfig = objMan.GetItemConfig(_id);
            randomBoxConfig = objMan.GetRandomBoxConfig(_id);
            mergeItemConfig = objMan.GetMergeItemConfig(_id);
        }

        public override string ToString()
        {
            return id.ToString();
        }
    }

    public class ObjectMan : IGameModule
    {
        private Dictionary<int, ObjBasic> mAllBasicConfig = new Dictionary<int, ObjBasic>();
        private Dictionary<int, ObjCoin> mAllCoinConfig = new Dictionary<int, ObjCoin>();
        private Dictionary<int, ObjItemConfig> mAllItemConfig = new Dictionary<int, ObjItemConfig>();
        private Dictionary<int, ObjRandomChest> mAllRandomBoxConfig = new Dictionary<int, ObjRandomChest>();
        private Dictionary<int, ObjMergeItem> mAllMergeItemConfig = new Dictionary<int, ObjMergeItem>();
        private Dictionary<int, ObjFullConfig> mAllFullConfig = new Dictionary<int, ObjFullConfig>();
        private Dictionary<int, ObjCardPack> mAllCardPackConfig = new Dictionary<int, ObjCardPack>();
        private Dictionary<int, ObjCard> mAllCardConfig = new Dictionary<int, ObjCard>();
        private Dictionary<int, ObjCardJoker> mAllCardJokerConfig = new Dictionary<int, ObjCardJoker>();
        private Dictionary<int, ObjSeasonItem> mAllSeasonItemConfig = new Dictionary<int, ObjSeasonItem>();
        private Dictionary<int, int> mAllTypeMasks = new Dictionary<int, int>();
        private Dictionary<int, ObjToken> mAllTokenConfig = new Dictionary<int, ObjToken>();
        private List<int> mFreeItem = new List<int>();
        private List<ObjConfigType> mAllType = new List<ObjConfigType>();
        private Dictionary<ObjConfigType, int> mCurrentConfigVersion = new Dictionary<ObjConfigType, int>();

        private delegate int IdGetter<T>(T t);

        private void _OnConfigLoaded()
        {
            var configMan = Game.Manager.configMan;
            _InitConfig(mAllBasicConfig, configMan.GetObjBasicConfigs(), (t) => t.Id, ObjConfigType.Basic);
            _InitConfig(mAllTokenConfig, configMan.GetObjTokenConfigs().Values, (t) => t.Id, ObjConfigType.ActivityToken);
            _InitConfig(mAllCoinConfig, configMan.GetCoinConfigs(), (t) => t.Id, ObjConfigType.Coin);
            _InitConfig(mAllItemConfig, configMan.GetItemConfigs(), (t) => t.Id, ObjConfigType.Item);
            _InitConfig(mAllRandomBoxConfig, configMan.GetRandomBoxConfigs(), (t) => t.Id, ObjConfigType.RandomBox);
            _InitConfig(mAllCardPackConfig, configMan.GetCardPackConfigs(), t => t.Id, ObjConfigType.CardPack);
            _InitConfig(mAllCardConfig, configMan.GetCardConfigs(), t => t.Id, ObjConfigType.Card);
            _InitConfig(mAllCardJokerConfig, configMan.GetCardJokerConfigs(), t => t.Id, ObjConfigType.CardJoker);
            _InitConfig(mAllSeasonItemConfig, configMan.GetObjSeasonItemConfigs(), t => t.Id, ObjConfigType.SeasonItem);

            OnMergeBoardVersionUpdate(0);

            //collect free item
            foreach (var config in mAllBasicConfig.Values)
            {
                if (config.Price <= 0 && config.PriceType != CoinType.NoneCoin)
                {
                    mFreeItem.Add(config.Id);
                }
            }
        }

        public void OnMergeBoardVersionUpdate(int version)
        {
            var mergeObjConfigs = Game.Manager.configMan.GetObjMergeItemConfigs(version, out var realVersion);
            if (!mCurrentConfigVersion.TryGetValue(ObjConfigType.MergeItem, out var lastVersion) || realVersion != lastVersion)            //nothing changed
            {
                _ClearConfig(mAllMergeItemConfig, (t) => t.Id, ObjConfigType.MergeItem);
                _InitConfig(mAllMergeItemConfig, mergeObjConfigs, (t) => t.Id, ObjConfigType.MergeItem);
                mCurrentConfigVersion[ObjConfigType.MergeItem] = realVersion;
            }
        }

        public IEnumerator<int> WalkAllFreeItem()
        {
            return mFreeItem.GetEnumerator();
        }

        public IEnumerator<int> WalkAllIdWithType(ObjConfigType type)
        {
            return WalkAllIdWithMask((int)type);
        }

        public IEnumerator<int> WalkAllIdWithMask(int typeMask = 0)
        {
            var iter = mAllTypeMasks.GetEnumerator();
            while (iter.MoveNext())
            {
                if ((iter.Current.Value & typeMask) == typeMask)
                {
                    yield return iter.Current.Key;
                }
            }
        }

        public bool IsObject(int id)
        {
            return mAllTypeMasks.ContainsKey(id);
        }

        public bool IsOneOfType(int id, int typeMask)
        {
            int mask = 0;
            mAllTypeMasks.TryGetValue(id, out mask);
            return (mask & typeMask) != 0;
        }

        public bool IsType(int id, int typeMask)
        {
            int mask = 0;
            mAllTypeMasks.TryGetValue(id, out mask);
            return (mask & typeMask) == typeMask;
        }

        public bool IsType(int id, ObjConfigType typeMask)
        {
            int mask = 0;
            mAllTypeMasks.TryGetValue(id, out mask);
            return (mask & (int)typeMask) == (int)typeMask;
        }

        public ObjConfigType DeduceTypeForId(int id)
        {
            int mask = 0;
            if (!mAllTypeMasks.TryGetValue(id, out mask))
            {
                return ObjConfigType.None;
            }
            else
            {
                foreach (var type in mAllType)
                {
                    if (((int)type & mask) == (int)type)
                    {
                        return type;
                    }
                }
                return ObjConfigType.None;
            }
        }

        public ObjFullConfig GetFullConfig(int id)
        {
            ObjFullConfig ret = null;
            if (!mAllFullConfig.TryGetValue(id, out ret))
            {
                ret = new ObjFullConfig(id, this);
                mAllFullConfig.Add(id, ret);
            }
            return ret;
        }

        public ObjBasic GetBasicConfig(int id)
        {
            return _FindConfig(mAllBasicConfig, id);
        }

        public ObjCoin GetCoinConfig(int id)
        {
            return _FindConfig(mAllCoinConfig, id);
        }

        public ObjItemConfig GetItemConfig(int id)
        {
            return _FindConfig(mAllItemConfig, id);
        }

        public ObjRandomChest GetRandomBoxConfig(int id)
        {
            return _FindConfig(mAllRandomBoxConfig, id);
        }

        public ObjMergeItem GetMergeItemConfig(int id)
        {
            return _FindConfig(mAllMergeItemConfig, id);
        }

        public ObjToken GetTokenConfig(int id)
        {
            return _FindConfig(mAllTokenConfig, id);
        }
        
        //传入id获取对应卡包配置数据
        public ObjCardPack GetCardPackConfig(int id)
        {
            return _FindConfig(mAllCardPackConfig, id);
        }
        
        //传入id获取对应卡片配置数据
        public ObjCard GetCardConfig(int id)
        {
            return _FindConfig(mAllCardConfig, id);
        }
        
        //传入id获取对应卡片配置数据
        public ObjCardJoker GetCardJokerConfig(int id)
        {
            return _FindConfig(mAllCardJokerConfig, id);
        }
        
        //传入id获取对应赛季物品配置数据
        public ObjSeasonItem GetSeasonItemConfig(int id)
        {
            return _FindConfig(mAllSeasonItemConfig, id);
        }

        //传入SeasonItem id 返回其对应的真实id
        public int TransSeasonItemToRealId(int oldId)
        {
            var conf = GetSeasonItemConfig(oldId);
            if (conf == null)
                return oldId;
            var curRoundId = 0;
            var cardMan = Game.Manager.cardMan;
            if (cardMan.CheckValid())
            {
                curRoundId = cardMan.GetCardRoundData()?.CardRoundId ?? 0;
            }
            foreach (var info in conf.IndexInfo)
            {
                if (info.Key == curRoundId)
                {
                    return info.Value;
                }
            }
            return oldId;
        }

        public string GetItemDesc(int id)
        {
            var basicConfig = GetBasicConfig(id);
            if (basicConfig == null)
            {
                return "";
            }
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.MergeItem))
                return Merge.ItemUtility.FormatMergeItemDesc(id, basicConfig.Desc);
            else
                return I18N.Text(basicConfig.Desc);
        }

        public string GetItemRewardDesc(int id, int count)
        {
            var basicConfig = GetBasicConfig(id);
            if (basicConfig == null)
            {
                return "";
            }
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.MergeItem))
                return Merge.ItemUtility.FormatMergeItemDesc(id, basicConfig.Desc, count);
            else
                return I18N.Text(basicConfig.Desc);
        }

        private void _ClearConfig<T>(Dictionary<int, T> container, IdGetter<T> idGetter, ObjConfigType mask)
        {
            foreach (var oldConfig in container.Values)
            {
                int id = idGetter(oldConfig);
                var typeMask = 0;
                mAllTypeMasks.TryGetValue(id, out typeMask);
                typeMask &= ~((int)mask);
                mAllTypeMasks[id] = typeMask;
                mAllFullConfig.Remove(id);
            }
            container.Clear();
        }

        private void _InitConfig<T>(Dictionary<int, T> container, IEnumerable<T> configs, IdGetter<T> idGetter, ObjConfigType mask)
        {
            if (mask != ObjConfigType.Basic)
            {
                mAllType.AddIfAbsent(mask);
            }
            if (configs == null)
            {
                return;
            }
            foreach (var config in configs)
            {
                int id = idGetter(config);
                if (container.ContainsKey(id))
                {
                    DebugEx.FormatWarning("ObjectMan::_InitConfig ----> id crash {0} {1}", typeof(T).FullName, id);
                    continue;
                }
                container.Add(id, config);
                var typeMask = 0;
                mAllTypeMasks.TryGetValue(id, out typeMask);
                typeMask |= (int)mask;
                mAllTypeMasks[id] = typeMask;
            }
        }

        private T _FindConfig<T>(Dictionary<int, T> container, int id)
        {
            T t = default(T);
            container.TryGetValue(id, out t);
            return t;
        }

        void IGameModule.Reset()
        {
            mCurrentConfigVersion.Clear();

            mAllBasicConfig.Clear();
            mAllCoinConfig.Clear();
            mAllRandomBoxConfig.Clear();
            mAllMergeItemConfig.Clear();
            mAllFullConfig.Clear();
            mAllTokenConfig.Clear();
            mFreeItem.Clear();
            mAllCardPackConfig.Clear();
            mAllCardConfig.Clear();
            mAllCardJokerConfig.Clear();

            mAllTypeMasks.Clear();
            mAllType.Clear();
        }

        void IGameModule.LoadConfig() { _OnConfigLoaded(); }

        void IGameModule.Startup() { }
    }
}