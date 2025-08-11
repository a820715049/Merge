/*
 * @Author: qun.chao
 * @Date: 2023-11-30 11:58:24
 */
using System.Collections.Generic;
using fat.rawdata;

namespace FAT
{
    public class NpcMan : IGameModule, IPostSetUserDataListener
    {
        public Dictionary<int, NpcConfig> OrderNpcPool => mCanUseInOrderMap;

        private IDictionary<int, NpcConfig> mNpcConfigMap;
        // 等级变化时刷新可用于订单的npc
        private Dictionary<int, NpcConfig> mCanUseInOrderMap = new Dictionary<int, NpcConfig>();

        void IGameModule.Reset()
        {
            mCanUseInOrderMap.Clear();
        }

        void IGameModule.LoadConfig()
        {
            mNpcConfigMap = Game.Manager.configMan.GetNpcConfigMap();
        }

        void IGameModule.Startup()
        {
        }

        void IPostSetUserDataListener.OnPostSetUserData()
        {
            _RefreshOrderNpc();
        }

        public NpcConfig GetNpcConfig(int id)
        {
            mNpcConfigMap.TryGetValue(id, out var npc);
            return npc;
        }

        public void OnMergeLevelChange()
        {
            _RefreshOrderNpc();
        }

        private void _RefreshOrderNpc()
        {
            mCanUseInOrderMap.Clear();
            var level = Game.Manager.mergeLevelMan.level;
            foreach (var npc in mNpcConfigMap)
            {
                if (level >= npc.Value.OrderLevel)
                {
                    mCanUseInOrderMap.Add(npc.Key, npc.Value);
                }
            }
        }
    }
}