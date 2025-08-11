/*
 * @Author: qun.chao
 * @Date: 2024-12-20 10:23:47
 */
using fat.rawdata;

namespace FAT.Merge
{
    public enum EnergyBoostState
    {
        X1,
        X2,
        X4,
    }

    public static class EnergyBoostUtility
    {
        private enum BetState
        {
            X1,
            X2,
            X4,
        }

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
            var cfg = Game.Manager.configMan.globalConfig;
            return (BetState)state switch
            {
                BetState.X2 => cfg?.BoostRate ?? 2,
                BetState.X4 => cfg?.BoostRate4X ?? 4,
                _ => 1,
            };
        }

        public static int GetBoostLevel(int state)
        {
            var cfg = Game.Manager.configMan.globalConfig;
            return (BetState)state switch
            {
                BetState.X2 => cfg?.BoostLevel ?? 1,
                BetState.X4 => cfg?.BoostLevel4X ?? 2,
                _ => 0,
            };
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
                if (IsStartupEnergyEnough((BetState)nextState))
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
            return IsStartupEnergyEnough(bt) && FeatureReady(bt);
        }

        public static int SwitchBetState(int curState) => (int)SwitchBetState((BetState)curState);
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
                if (IsStartupEnergyEnough(nextState) && FeatureReady(nextState))
                    return nextState;
                // 状态不可用 继续尝试下一个
                ++fromState;
            }
            while (true);
        }

        private static bool IsStartupEnergyEnough(BetState curState)
        {
            return curState switch
            {
                // 体力大于100才可切换到4x
                BetState.X4 => Env.Instance.CanUseEnergy(101),
                _ => true,
            };
        }

        private static bool FeatureReady(BetState state)
        {
            var feat = StateToFeature(state);
            if (feat == FeatureEntry.FeatureNone)
                return true;
            return Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(feat);
        }

        private static FeatureEntry StateToFeature(BetState state)
        {
            return state switch
            {
                BetState.X2 => FeatureEntry.FeatureErgBoost,
                BetState.X4 => FeatureEntry.FeatureErgBoost4X,
                _ => FeatureEntry.FeatureNone,
            };
        }
    }
}