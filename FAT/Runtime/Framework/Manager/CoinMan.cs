/**
 * @Author: handong.liu
 * @Date: 2020-09-07 21:18:28
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using Config;
using fat.rawdata;

namespace FAT
{
    public struct CoinChange {
        public CoinType type;
        public int amount;
        public ReasonString reason;

        public CoinChange(CoinType type_, int amount_, ReasonString reason_) {
            type = type_;
            amount = amount_;
            reason = reason_;
        }
    }

    public class CoinMan : IGameModule, IUserDataHolder, IUserDataInitializer //, IUserDataVersionUpgrader
    {
        private Dictionary<CoinType, int> mTypeMap = new Dictionary<CoinType, int>();
        private Dictionary<int, CoinType> mIdMap = new Dictionary<int, CoinType>();
        private Dictionary<CoinType, EncryptInt> mCoin = new Dictionary<CoinType, EncryptInt>();
        private Dictionary<CoinType, int> mFlyingCoin = new Dictionary<CoinType, int>();
        private int gemCost;

        public void OnConfigLoaded()
        {
            var iter = Game.Manager.objectMan.WalkAllIdWithType(ObjConfigType.Coin);
            while (iter.MoveNext())
            {
                var coinConfig = Game.Manager.objectMan.GetCoinConfig(iter.Current);
                mTypeMap.Add(coinConfig.Type, coinConfig.Id);
                mIdMap.Add(coinConfig.Id, coinConfig.Type);
                mCoin.Add(coinConfig.Type, new EncryptInt().SetValue(0));
            }
        }

        public AssetConfig GetImageByCount(int coinId, int count)
        {
            AssetConfig ret = null;
            var config = Game.Manager.objectMan.GetCoinConfig(coinId);
            if (config != null && config.Imgs != null && config.Imgs.Count > 0)
            {
                ret = config.Imgs[0].ConvertToAssetConfig();
                for (int i = 1; i < config.ImgsAmount.Count && i < config.Imgs.Count; i++)
                {
                    if (count >= config.ImgsAmount[i])
                    {
                        ret = config.Imgs[i].ConvertToAssetConfig();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                ret = GetConfigByCoinId(coinId)?.Icon?.ConvertToAssetConfig();
            }
            return ret;
        }

        public ObjBasic GetConfigByCoinType(CoinType type)
        {
            int id = 0;
            if (mTypeMap.TryGetValue(type, out id))
            {
                return Game.Manager.objectMan.GetBasicConfig(id);
            }
            else
            {
                return null;
            }
        }

        public ObjBasic GetConfigByCoinId(int id)
        {
            return Game.Manager.objectMan.GetBasicConfig(id);
        }

        public CoinType GetCoinTypeById(int id)
        {
            CoinType ret = 0;
            if (mIdMap.TryGetValue(id, out ret))
            {
                return (CoinType)ret;
            }
            else
            {
                return CoinType.NoneCoin;
            }
        }

        public int GetIdByCoinType(CoinType type)
        {
            var ret = GetConfigByCoinType(type);
            if (ret != null)
            {
                return ret.Id;
            }
            else
            {
                return 0;
            }
        }

        public bool CanUseCoin(CoinType type, int amount)
        {
            return GetCoin(type) >= amount;
        }

        //传入id尝试消耗对应id的资源货币
        public bool UseCoinById(int coinId, int amount, ReasonString reason)
        {
            var type = GetCoinTypeById(coinId);
            if (type == CoinType.NoneCoin)
            {
                DebugEx.FormatWarning("CoinMan::UseCoinById ----> invalid coinId = {0}, amount = {1}, reason = {2} ", coinId, amount, reason.ToString());
                return false;
            }
            return UseCoin(type, amount, reason);
        }

        public bool UseCoin(CoinType type, int amount, ReasonString reason)
        {
            if (!mCoin.TryGetValue(type, out var coin))
            {
                DebugEx.FormatWarning("CoinMan::UseCoin ----> non-coin type used for {0}, {1}", amount, reason.ToString());
                return false;
            }
            if (amount < 0)
            {
                DebugEx.FormatError("CoinMan::UseCoin ----> negative value provided {0}, {1}, {2}", type, amount, reason);
                Game.Instance.Abort(I18N.GameErrorText((long)GameErrorCode.CoinError), (long)GameErrorCode.CoinError);
                return false;
            }
            if (coin.GetValue() >= amount)
            {
                coin.SetValue(coin.GetValue() - amount);
                DataTracker.TrackIntCoinChange(type, reason, false, amount);

                MessageCenter.Get<MSG.GAME_COIN_CHANGE>().Dispatch(type);
                MessageCenter.Get<MSG.GAME_COIN_USE>().Dispatch(new(type, amount, reason));
                if (type == CoinType.Gem)
                {
                    //消耗钻石时播音效
                    if (reason != ReasonString.skip && reason != ReasonString.bubble)
                    {
                        // 棋子加速不播放钻石音效 加速本身有音效
                        Game.Manager.audioMan.TriggerSound("ShopGem");
                    }
                    RecordGemCost(amount);
                }
                return true;
            }
            else
            {
                //如果消耗的货币为钻石 则打开IAP商店 并弹提示
                if (type == CoinType.Gem) {
                    UIUtility.ShowGemNotEnough();
                }
                return false;
            }
        }

        public BigNumber GetCoin(CoinType type)
        {
            if (!mCoin.TryGetValue(type, out var coin))
            {
                return 0;
            }
            BigNumber ret = coin.GetValue();
            return ret;
        }

        public BigNumber GetCoin(int coinId)
        {
            if (!mIdMap.TryGetValue(coinId, out var type))
            {
                return 0;
            }
            return GetCoin(type);
        }

        public BigNumber GetDisplayCoin(CoinType type)
        {
            if (!mCoin.TryGetValue(type, out var coin))
            {
                return 0;
            }
            BigNumber flyCoin = mFlyingCoin.GetDefault(type, 0);
            BigNumber ret = coin.GetValue() - flyCoin;
            //显示上的货币数始终大于0 避免在飞货币的过程中使用货币，导致显示出负数
            if (ret < 0)
                ret = 0;
            return ret;
        }

        public BigNumber GetDisplayCoin(int coinId)
        {
            if (!mIdMap.TryGetValue(coinId, out var type))
            {
                return 0;
            }
            return GetDisplayCoin(type);
        }

        public void AddFlyCoin(CoinType type, int amount, ReasonString reason)
        {
            mFlyingCoin[type] = mFlyingCoin.GetDefault(type, 0) + amount;
            AddCoin(type, amount, reason);
            DebugEx.FormatInfo("CoinMan.AddFlyCoin ----> fly coin add {0} {1}:{2}", reason, type, amount);
        }

        public void FinishFlyCoin(CoinType type, int amount)
        {
            mFlyingCoin[type] = mFlyingCoin.GetDefault(type, 0) - amount;
            DebugEx.FormatInfo("CoinMan.FinishFlyCoin ----> fly coin finish {0}:{1}", type, amount);
            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().Dispatch(type);
        }

        public void AddCoin(CoinType type, int amount, ReasonString reason)
        {
            if (!mCoin.TryGetValue(type, out var coin))
            {
                DebugEx.FormatWarning("CoinMan.AddCoin ----> add coin with no type {0}, {1}", type, amount);
                return;
            }
            DebugEx.FormatInfo("CoinMan.AddCoin ----> add coin type {0}, {1}", type, amount);
            coin.SetValue(coin.GetValue() + amount);

            if (type == CoinType.MergeCoin)
            {
                RecordCoinGet(amount);
            }

            DataTracker.TrackIntCoinChange(type, reason, true, amount);

            MessageCenter.Get<MSG.GAME_COIN_CHANGE>().Dispatch(type);
            MessageCenter.Get<MSG.GAME_COIN_ADD>().Dispatch(new(type, amount, reason));
        }

        public BigNumber CalculateFinalPrice(ObjBasic config, ObjConfigType type = ObjConfigType.None)
        {
            if (config != null)
            {
                if (type == ObjConfigType.None)
                {
                    type = Game.Manager.objectMan.DeduceTypeForId(config.Id);
                }
                float discount = 0;
                if (discount > 0)
                {
                    return EL.MathUtility.ApplyPercentage((BigNumber)config.Price, -discount);
                }
                else
                {
                    return config.Price;
                }
            }
            else
            {
                return 0;
            }
        }

        #region record

        // 记录最近累积获得的非膨胀货币(排除等级加成)
        private int coinGet;
        // round取整 最小为1
        private int CalcNormalizedCoin(int amount)
        {
            var rate = Game.Manager.mergeLevelMan.GetCurrentLevelRate();
            var coin = Mathf.RoundToInt(amount / (rate / 100f));
            return coin < 1 ? 1 : coin;
        }
        private void RecordCoinGet(int amount) => coinGet += CalcNormalizedCoin(amount);
        public int FetchCoinGet() {
            var v = coinGet;
            coinGet = 0;
            return v;
        }

        private void RecordGemCost(int amount) => gemCost += amount;
        public int FetchGemCost() {
            var v = gemCost;
            gemCost = 0;
            return v;
        }

        #endregion

        #region imp

        void IGameModule.Reset()
        {
            mTypeMap.Clear();
            mIdMap.Clear();
            mCoin.Clear();
            mFlyingCoin.Clear();
        }
        void IGameModule.LoadConfig() { OnConfigLoaded(); }
        void IGameModule.Startup() { }

        void IUserDataHolder.FillData(fat.gamekitdata.LocalSaveData data)
        {
            var gameData = data.PlayerBaseData;
            if (gameData != null)
            {
                foreach (var kv in mCoin)
                {
                    gameData.Coins[(int)kv.Key] = kv.Value;
                }
            }
        }

        void IUserDataHolder.SetData(fat.gamekitdata.LocalSaveData data)
        {
            foreach (var v in mCoin.Values)
            {
                v.SetValue(0);
            }
            var gameData = data.PlayerBaseData;
            foreach (var kv in gameData.Coins)
            {
                if (!mCoin.TryGetValue((CoinType)kv.Key, out var coin))
                {
                    coin = mCoin[(CoinType)kv.Key] = new EncryptInt().SetValue(0);
                }
                coin.SetValue(kv.Value);
            }
        }

        // void IUserDataVersionUpgrader.OnDataVersionUpgrade(GameNet.User src, GameNet.User dst)
        // {
        // }

        //新号创建时 设置其货币初始值  一个号只会执行一次
        void IUserDataInitializer.InitUserData()
        {
            var objMan = Game.Manager.objectMan;
            var iter = objMan.WalkAllIdWithType(ObjConfigType.Coin);
            while (iter.MoveNext())
            {
                var config = objMan.GetCoinConfig(iter.Current);
                mCoin[config.Type].SetValue(config.InitCount);
                Game.Manager.networkMan.ExecuteAfterLogin(() =>
                {
                    DataTracker.TrackIntCoinChange(config.Type, ReasonString.init, true, config.InitCount);
                });
            }
        }

        #endregion
    }
}