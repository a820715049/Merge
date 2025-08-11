using fat.rawdata;
using System.Collections.Generic;
using Config;

namespace FAT {
    public class BonusReward {
        public int packId;
        public bool album;
        public int iapId;
        public readonly List<RewardConfig> reward = new();
        public RewardConfig this[int n] => n >= 0 && n< reward.Count ? reward[n] : default;
        public bool AlbumActive => Game.Manager.activity.IsActive(EventType.CardAlbum);

        public void Clear() {
            packId = 0;
            album = false;
            reward.Clear();
        }

        public bool Refresh(int packId_) {
            var iap = Game.Manager.iap;
            if (!iap.FindIAPPack(packId_, out var conf)) {
                Clear();
                return false;
            }
            return Refresh(packId_, conf);
        }

        public bool Refresh(IAPPack conf_) {
            if (conf_ == null) {
                Clear();
                return false;
            }
            return Refresh(conf_.Id, conf_);
        }

        public bool Refresh(int packId_, IAPPack conf_) {
            var albumActive = AlbumActive;
            if (packId == packId_ && album == albumActive) return true;
            iapId = conf_.IapId;
            packId = packId_;
            album = albumActive;
            reward.Clear();
            var list = albumActive ? conf_.RewardAlbum : conf_.Reward;
            foreach (var r in list) {
                reward.Add(r.ConvertToRewardConfig());
            }
            return true;
        }

        public bool RefreshShop(int slot_, int packId_) {
            var activity = Game.Manager.activity;
            if (!activity.LookupAny(EventType.MarketIapgift, out var a)) return Refresh(packId_);
            var acti = (ActivityMIG)a;
            packId_ = acti.ReplaceSlot(slot_, packId_);
            return Refresh(packId_);
        }
    }
}