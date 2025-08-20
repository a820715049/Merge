/*
 * @Author: qun.chao
 * @Date: 2024-12-20 10:23:47
 * @LastEditors: ange.shentu
 * @LastEditTime: 2025/07/04 15:25:10
 * @Document:https://centurygames.feishu.cn/wiki/RyuZwgDbZiE3PwkXvz6cmPe1ntb
 */
namespace FAT.Merge
{
    public enum EnergyBoostState
    {
        X1 = 0,
        X2 = 1,
        X4 = 2,
    }

    public static class EnergyBoostUtility
    {
        //隔离枚举，这个枚举仅用于Utility类内部处理逻辑
        private enum BetState
        {
            X1 = 0,
            X2 = 1,
            X4 = 2,
        }
        #region 公共方法
        public static bool Is4X()
        {
            return Env.Instance.GetEnergyBoostState() == EnergyBoostState.X4;
        }

        public static bool AnyEnergyBoostFeatureReady()
        {
            return FeatureReady(BetState.X2) || FeatureReady(BetState.X4);
        }

        public static ReasonString GetEnergyProduceReason(int curState)
        {
            return (BetState)curState switch
            {
                BetState.X2 => ReasonString.produce_2x,
                BetState.X4 => ReasonString.produce_4x,
                _ => ReasonString.produce,
            };
        }

        public static bool IsBoost(int curState) => curState != (int)BetState.X1;

        public static int GetEnergyRate(int state)
        {
            var cfg = Game.Manager.configMan.GetEnergyBoostConfig(state);
            if (cfg != null)
            {
                return cfg.BoostRate;
            }
            return 1;
        }

        public static int GetBoostLevel(int state)
        {
            var cfg = Game.Manager.configMan.GetEnergyBoostConfig(state);
            if (cfg != null)
            {
                return cfg.BoostLevel;
            }
            return 0;
        }

        public static (string nameKey, string descKey) GetBoardDetailKeyForBoostState()
        {
            var state = Env.Instance.GetEnergyBoostState();
            return state switch
            {
                EnergyBoostState.X2 => ("#SysComDesc35", "#SysComDesc222"),
                EnergyBoostState.X4 => ("#SysComDesc35", "#SysComDesc757"),
                _ => ("#SysComDesc221", "#SysComDesc223"),
            };
        }

        public static string GetEnergyBoostTipText()
        {
            var state = Env.Instance.GetEnergyBoostState();
            var bet = GetEnergyRate((int)state);
            return state switch
            {
                EnergyBoostState.X2 => EL.I18N.FormatText("#SysComDesc786", bet),
                EnergyBoostState.X4 => EL.I18N.FormatText("#SysComDesc785", bet),
                _ => EL.I18N.Text("#SysComDesc221"),
            };
        }

        public static bool OnLoginAdjustBetState(int curState, out int nextState)
        {
            nextState = curState;
            do
            {
                if (FeatureReady((BetState)nextState))
                    return nextState != curState;
                --nextState;
            }
            while (System.Enum.IsDefined(typeof(BetState), nextState));
            nextState = (int)BetState.X1;
            return true;
        }

        public static bool CanSwitchToState(EnergyBoostState state)
        {
            var bt = (BetState)state;
            return FeatureReady(bt);
        }

        public static int SwitchBetState(int curState) => (int)SwitchBetState((BetState)curState);
        #endregion

        // 兼容跳过低档直接开启高档时的切换逻辑
        private static BetState SwitchBetState(BetState fromState)
        {
            do
            {
                var nextState = fromState + 1;
                // 不合法 切换到关闭状态
                if (!System.Enum.IsDefined(typeof(BetState), nextState))
                    return BetState.X1;
                // 状态可用
                if (FeatureReady(nextState))
                    return nextState;
                // 状态不可用 继续尝试下一个
                ++fromState;
            }
            while (true);
        }

        /// <summary>
        /// 这里抽了一部分FeatureUnlockManager中IsEntryMatchRequire的逻辑（Guide、Level）
        /// 目的是方便AB测试配置，在EnergyBooster单表中就可以完成AB配置
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static bool FeatureReady(BetState state)
        {
            if (state == BetState.X1)
            {
                return true;
            }
            var cfg = Game.Manager.configMan.GetEnergyBoostConfig((int)state);
            if (cfg != null)
            {
                //体力判断
                if (cfg.RequireEnergyNum > 0)
                {
                    if (!Env.Instance.CanUseEnergy(cfg.RequireEnergyNum))
                    {
                        return false;
                    }
                }

                //活动开启开关判断
                // if (cfg.IsPassive)
                // {
                //     //TODO: 暂时预留的判断
                //     //后续如果X8、X16等状态需要通过依赖活动状态来做开关，需要增加一个接口类给控制能量加倍的活动来继承
                //     //36版本内所有IsPassive均为false，所以这里注释，减少干扰
                //     //遍历活动，寻找接口类，有true就直接break.
                // }

                if (cfg.Guide > 0)
                {
                    if (Game.Manager.guideMan.IsGuideFinished(cfg.Guide))
                    {
                        // guide达成则无视其他条件直接解锁（这里仅跳过Level）
                        return true;
                    }
                    // 这里和IsEntryMatchRequire的逻辑保持一致，guide不判断false的情况
                }

                //等级判断
                var level = Game.Manager.mergeLevelMan.displayLevel;
                if (cfg.ActiveLv > 0)
                {
                    if (level < cfg.ActiveLv)
                    {
                        return false;
                    }
                }
                return true;
            }
            //没有找到对应配置，返回false
            return false;
        }
    }
}
