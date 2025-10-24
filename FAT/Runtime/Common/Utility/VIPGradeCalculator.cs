/*
 * @Description: VIP等级计算工具类，根据累计付费金额和配置表计算VIP等级
 */
using System;
using EL;

namespace FAT
{
    public static class VIPGradeCalculator
    {
        /// <summary>
        /// 根据累计付费金额计算VIP等级
        /// </summary>
        /// <param name="totalPaymentCents">累计付费金额（单位：美分）</param>
        /// <returns>VIP等级，如果配置错误返回0</returns>
        public static int CalculateVIPLevel(int totalPaymentCents)
        {
            try
            {
                // 将美分转换为美元
                float totalPaymentUSD = totalPaymentCents / 100.0f;
                
                // 获取VIP配置表数据
                var vipConfigMap = fat.conf.CustCmpltVIPVisitor.All();
                if (vipConfigMap == null || vipConfigMap.Count == 0)
                {
                    DebugEx.Error("VIPGradeCalculator: VIP配置表为空");
                    return 0;
                }
                
                int resultLevel = 0;
                
                // 遍历配置表找到合适的VIP等级
                foreach (var config in vipConfigMap.Values)
                {
                    // 检查付费金额是否在当前配置的区间内
                    // 区间为左开右闭：(valueLeft, valueRight]
                    bool inRange;
                    
                    if (config.ValueRight == -1)
                    {
                        // 右端点为-1表示+∞
                        inRange = totalPaymentUSD > config.ValueLeft;
                    }
                    else
                    {
                        inRange = totalPaymentUSD > config.ValueLeft && totalPaymentUSD <= config.ValueRight;
                    }
                    
                    if (inRange)
                    {
                        // 找到匹配的等级，取最高等级（防止配置重叠）
                        resultLevel = Math.Max(resultLevel, config.Level);
                    }
                }
                
                DebugEx.Info($"VIPGradeCalculator: 付费金额 ${totalPaymentUSD:F2} USD ({totalPaymentCents}分) -> VIP等级 {resultLevel}");
                return resultLevel;
            }
            catch (Exception ex)
            {
                DebugEx.Error($"VIPGradeCalculator: 计算VIP等级时发生异常: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// 获取用户当前VIP等级
        /// </summary>
        /// <returns>当前用户的VIP等级</returns>
        public static int GetCurrentUserVIPLevel()
        {
            try
            {
                int totalPayment = Game.Manager.iap.TotalIAPServer;
                return CalculateVIPLevel(totalPayment);
            }
            catch (Exception ex)
            {
                DebugEx.Error($"VIPGradeCalculator: 获取当前用户VIP等级时发生异常: {ex.Message}");
                return 0;
            }
        }
    }
}
