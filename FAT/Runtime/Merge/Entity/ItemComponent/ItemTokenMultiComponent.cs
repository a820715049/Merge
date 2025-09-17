/*
 * @Author: tang.yan
 * @Description: 用于将棋子上挂载的活动积分token数量翻倍的组件 - 目前认为全局只会有一个持有本组件的棋子生效
 * @Doc: https://centurygames.feishu.cn/wiki/FCr6wUVEZiwH77kZn6pcjmTxn1g
 * @Date: 2025-09-15 14:09:05
 */

using fat.rawdata;

namespace FAT.Merge
{
    //用于将棋子上挂载的活动积分token数量翻倍的组件 - 目前认为全局只会有一个持有本组件的棋子生效
    //目前仅支持EventType.MicMilestone活动，后续有新活动需要支持则再开发 
    public class ItemTokenMultiComponent : ItemComponentBase
    {
        public bool isCounting => item.world.tokenMulti.activeTokenMultiId == item.id;
        public int countdown => item.world.tokenMulti.countdown;
        public ComMergeTokenMultiplier config => mConfig;
        private ComMergeTokenMultiplier mConfig = null;

        public static bool Validate(ItemComConfig config)
        {
            return config?.tokenMultiConfig != null;
        }

        protected override void OnPostAttach()
        {
            base.OnPostAttach();
            mConfig = Env.Instance.GetItemComConfig(item.tid).tokenMultiConfig;
        }
    }
}