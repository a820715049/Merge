/*
 *@Author:chaoran.zhang
 *@Desc:弹珠活动创建
 *@Created Time:2024.12.09 星期一 11:26:49
 */

using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    public class GroupPachinko : ActivityGroup
    {
        public override (bool, string) CreateCheck(EventType type_, LiteInfo lite_)
        {
            var feature = Game.Manager.featureUnlockMan;
            var rf = nameof(feature);
            return (feature.IsFeatureEntryUnlocked(FeatureEntry.FeaturePachinko), rf);
        }

        public override (bool, string) TryAdd(Activity activity_, (int, int) id_, EventType type_,
            ActivityInstance data_, in Option option_)
        {
            if (activity_.IsActive(id_)) return (true, "active");
            if (activity_.IsExpire(id_)) return (false, "expire");
            var (r, rs) = ActivityLite.ReadyToCreate(id_, option_, out var lite);
            if (!r) return activity_.Invalid(id_, $"{id_} not available reason:{rs}");
            (r, rs) = TryCreateByType(activity_, id_, type_, lite, data_, option_, out var pack);
            if (!r) return (r, rs);
            pack.LoadData(data_);
            if (!pack.Valid) return activity_.Invalid(id_, $"{id_} detail invalid");
            option_.Apply(pack);
            activity_.AddActive(id_, pack, data_ == null);
            Game.Manager.pachinkoMan.AddActivity(pack as ActivityPachinko);
            return (true, null);
        }


        public override ActivityLike Create(EventType type_, ActivityLite lite_)
        {
            return new ActivityPachinko(lite_);
        }

        public override void End(Activity activity_, ActivityLike acti_, bool expire_)
        {
            Game.Manager.pachinkoMan.EndActivity(acti_ as ActivityPachinko);
        }
    }
}