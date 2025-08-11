/*
 *@Author:chaoran.zhang
 *@Desc:装饰区活动对创建
 *@Created Time:2024.05.21 星期二 11:07:29
 */

using fat.conf;
using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    public class GroupDecorate : ActivityGroup
    {
        public override (bool, string) TryAdd(Activity activity_, (int, int) id_, EventType type_, ActivityInstance data_, in Option option_)
        {
            if (activity_.IsActive(id_)) return (true, "active");
            if (activity_.IsExpire(id_)) return (false, "expire");
            var (r, rs) = ActivityLite.ReadyToCreate(id_, option_, out var lite);
            if (!r) return activity_.Invalid(id_, $"{id_} not available reason:{rs}");
            (r, rs) = TryCreate(lite, type_, out var pack);
            if (!r) return (false, rs);
            pack.LoadData(data_);
            option_.Apply(pack);
            activity_.AddActive(id_, pack, new_: data_ == null);
            Game.Manager.decorateMan.RefreshData(pack);
            return (true, null);
        }

        public (bool, string) TryCreate(LiteInfo lite_, EventType type_, out DecorateActivity activity)
        {
            activity = null;
            if (!Game.Manager.decorateMan.IsUnlock)
                return (false, "not unlock");
            var (r, rs) = ActivityLite.TryCreate(lite_, type_, out var lite);
            if (!r) return (r, rs);
            var deco = GetEventDecorate(lite.Param);
            if (deco == null)
                return (false, $"detail config for {lite.Param} not found");
            activity = new DecorateActivity();
            activity.SetUp(lite, deco);
            return (true, null);
        }

        public override void End(Activity activity_, ActivityLike acti_, bool expire_)
        {
            Game.Manager.decorateMan.WhenActivityEnd();
        }
    }
}