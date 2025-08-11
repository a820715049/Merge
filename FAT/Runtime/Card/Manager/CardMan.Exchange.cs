/*
 * @Author: tang.yan
 * @Description: 集卡系统管理器-卡片星星兑换相关逻辑 
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/eopuffcrlitdttbt
 * @Date: 2024-10-18 10:10:35
 */

using System;
using System.Collections.Generic;
using fat.rawdata;
using EL;
using UnityEngine;

namespace FAT
{
    //卡片星星兑换相关逻辑
    public partial class CardMan
    {
        //下次可以显示卡片星星兑换入口红点的时间 会存档
        private long _nextShowExchangeRPTs;
        
        //检查当前是否可以开启卡片星星兑换功能  未开启时不显示入口 星星库存也不会增加
        public bool IsOpenStarExchange()
        {
            return GetCardRoundData()?.IsOpenStarExchange() ?? false;
        }

        //获取当前星星库存 (固定库存+虚拟库存)
        public int GetTotalStarNum()
        {
            var roundData = GetCardRoundData();
            if (roundData == null) return 0;
            var totalVirtualStarNum = roundData.TryGetCardAlbumData()?.GetTotalVirtualStarNum() ?? 0;
            return roundData.GetTotalFixedStarNum() + totalVirtualStarNum;
        }

        //获取当前星星的固定库存
        public int GetTotalFixedStarNum()
        {
            return GetCardRoundData()?.GetTotalFixedStarNum() ?? 0;
        }
        
        //获取本轮所有重复卡片转换后对应的星星数量(虚拟库存)
        public int GetTotalVirtualStarNum()
        {
            return GetCardRoundData()?.TryGetCardAlbumData()?.GetTotalVirtualStarNum() ?? 0;
        }

        public void OpenUICardExchangeReward()
        {
            var curTs = Game.Instance.GetTimestampSeconds();
            if (curTs >= _nextShowExchangeRPTs && CanShowStarExchangeRP())
            {
                var offsetHour = Game.Manager.configMan.globalConfig.CardShopRedRefreshUtc;
                //刷新下次重置时间
                _nextShowExchangeRPTs = ((curTs - offsetHour * 3600) / Constant.kSecondsPerDay + 1) * Constant.kSecondsPerDay + offsetHour * 3600;
            }
            //打开界面
            UIManager.Instance.OpenWindow(UIConfig.UICardExchangeReward);
        }

        //判断是否显示星星兑换入口红点 只在需要时调用 别频繁调
        //当前没开启星星兑换或者目前星星数量不够兑换奖励时 不显示红点
        public bool CanShowStarExchangeRP()
        {
            if (!IsOpenStarExchange())
                return false;
            var curTs = Game.Instance.GetTimestampSeconds();
            if (curTs < _nextShowExchangeRPTs)
                return false;
            var totalNum = GetTotalStarNum();
            var canShow = false;
            var starExchangeDataList = GetCardRoundData().GetAllStarExchangeData();
            foreach (var data in starExchangeDataList)
            {
                var conf = data.GetConfig();
                if (conf != null && conf.CostStar <= totalNum && !data.IsInCd())
                {
                    canShow = true;
                    break;
                }
            }
            return canShow;
        }

        public bool CheckCanExchange(int exchangeId)
        {
            if (exchangeId <= 0) 
                return false;
            var totalNum = GetTotalStarNum();
            var data = GetCardRoundData()?.GetStarExchangeData(exchangeId);
            return data?.GetConfig()?.CostStar <= totalNum;
        }
        
        //在兑换界面尝试消耗固定库存的星星直接兑换奖励  返回true时表示兑换成功
        public bool TryConsumeStarForReward(int exchangeId)
        {
            if (exchangeId <= 0) 
                return false;
            var roundData = GetCardRoundData();
            if (roundData == null) return false;
            var totalStarNum = GetTotalStarNum();
            var fixedStarNum = roundData.GetTotalFixedStarNum();
            var exchangeData = roundData.GetStarExchangeData(exchangeId);
            if (exchangeData == null || exchangeData.IsInCd()) return false;
            var conf = exchangeData.GetConfig();
            if (conf == null) return false;
            var costStar = conf.CostStar;
            //判断当前星星固定库存是否≥兑换所需星星数量, 是则直接兑换成功，扣除对应数量的星星，弹出发奖界面。
            if (fixedStarNum >= costStar)
            {
                //默认配的是随机宝箱 数量固定为1
                var rewardMan = Game.Manager.rewardMan;
                rewardMan.CommitReward(rewardMan.BeginReward(conf.Reward, 1, ReasonString.star_exchange));
                //更新宝箱cd和固定库存数量
                roundData.OnCardStarExchangeSuccess(exchangeId, costStar, costStar, 0);
                //立即存档
                Game.Manager.archiveMan.SendImmediately(true);
                //消耗星星兑换奖励时打点
                TrackCardStarExchange(exchangeId, costStar, roundData.GetTotalUsedStarNum(), 0);
                return true;
            }
            //否则判断星星总数(固定+虚拟)是否够兑换的  是则打开选卡兑换界面
            if (totalStarNum >= costStar)
            {
                UIManager.Instance.OpenWindow(UIConfig.UICardExchangeSelect, exchangeId);
            }
            return false;
        }

        //为卡片星星兑换UI相关逻辑提供的数据类
        public class UICardStarExchangeData
        {
            public int CardId;          //卡片ObjCard.id 
            public int RepeatOwnCount;  //重复拥有的数量(拥有总数-1)
            public int CardStar;        //卡片星级
            public int SelectNum;       //目前已选数量

            public ObjBasic GetObjBasicConfig()
            {
                return Game.Manager.objectMan.GetBasicConfig(CardId);
            }

            public ObjCard GetConfig()
            {
                return Game.Manager.objectMan.GetCardConfig(CardId);
            }
        }
        
        //传入自建容器 会填充系统默认的最优卡片排列顺序
        public void FillBestSelectCardList(List<UICardStarExchangeData> container, int exchangeId)
        {
            if (container == null)
                return;
            var roundData = GetCardRoundData();
            if (roundData == null || !IsOpenStarExchange())
                return;
            var allCardData = roundData.TryGetCardAlbumData()?.GetAllCardDataMap()?.Values;
            if (allCardData == null || allCardData.Count < 1)
                return;
            container.Clear();  //填充数据前先清理
            foreach (var cardData in allCardData)
            {
                var conf = cardData.GetConfig();
                if (conf == null) continue;
                var repeatOwnCount = cardData.OwnCount - 1;
                if (repeatOwnCount <= 0) continue;
                var data = new UICardStarExchangeData
                {
                    CardId = cardData.CardId,
                    RepeatOwnCount = repeatOwnCount,
                    CardStar = conf.Star,
                    SelectNum = 0
                };
                container.Add(data);
            }
            container.Sort(_BestSelectCardSort);
            ChooseBestSelectCard(container, exchangeId);
        }

        //玩家主动点击一键选卡按钮 会清空container中的选卡情况 重新按系统默认方式选择
        public void ChooseBestSelectCard(List<UICardStarExchangeData> container, int exchangeId)
        {
            var costStar = GetCardRoundData()?.GetStarExchangeData(exchangeId)?.GetConfig()?.CostStar ?? 0;
            var totalFixedStar = GetTotalFixedStarNum();
            if (costStar > 0 && costStar > totalFixedStar)
            {
                var targetStarNum = costStar - totalFixedStar;
                _SelectBestCardDirectly(container, targetStarNum);
            }
        }
        
        //在选卡兑换界面尝试消耗 固定库存的星星以及部分卡片 来兑换奖励  返回true时表示兑换成功
        //container：通过FillBestSelectCardList获得的所有卡片容器 里面存有界面操作信息
        //exchangeId：目前要兑换的奖励条目id
        public bool TryConsumeCardForReward(List<UICardStarExchangeData> container, int exchangeId)
        {
            if (container == null || exchangeId <= 0) return false;
            var roundData = GetCardRoundData();
            if (roundData == null) return false;
            var exchangeData = roundData.GetStarExchangeData(exchangeId);
            if (exchangeData == null || exchangeData.IsInCd()) return false;
            var conf = exchangeData.GetConfig();
            if (conf == null) return false;
            var costStar = conf.CostStar;
            var totalFixedStar = roundData.GetTotalFixedStarNum();
            var targetStarNum = costStar - totalFixedStar;
            //不管是系统默认方案还是玩家自选方案，都检查下是否有溢出并剔除多余卡片，按最后结果进行结算
            if (_SelectBestCardBaseOnContainer(container, targetStarNum, out var overflow))
            {
                //默认配的是随机宝箱 数量固定为1
                var rewardMan = Game.Manager.rewardMan;
                rewardMan.CommitReward(rewardMan.BeginReward(conf.Reward, 1, ReasonString.star_exchange));
                //更新宝箱cd和固定库存数量
                roundData.OnCardStarExchangeSuccess(exchangeId, costStar, totalFixedStar, overflow);
                //更新卡片数量
                foreach (var card in container)
                {
                    var selectNum = card.SelectNum;
                    if (selectNum <= 0) continue;
                    var cardData = GetCardData(card.CardId);
                    if (cardData == null) continue;
                    //数据层减少卡片数量并打点
                    cardData.ReduceCardCount(selectNum, 2, exchangeId);
                    card.RepeatOwnCount -= selectNum;
                    //卡片已选数量清0
                    card.SelectNum = 0;
                }
                //立即存档
                Game.Manager.archiveMan.SendImmediately(true);
                //消耗星星兑换奖励时打点
                TrackCardStarExchange(exchangeId, totalFixedStar, roundData.GetTotalUsedStarNum(), overflow);
                MessageCenter.Get<MSG.CARD_STAR_EXCHANGE>().Dispatch();
                return true;
            }
            return false;
        }

        //根据传入的目标星星数量targetStarNum，在划定范围container内，找出最合适的卡片选择方案，并且选卡结果星星总数尽量趋近于targetStarNum
        //注意：本方法会无视container中原有的卡片选择情况 直接给出默认最佳选择方案
        //needUIShow 最终结果是否要在UI上面显示 如果需要的话 会在最后多走一遍排序逻辑
        private void _SelectBestCardDirectly(List<UICardStarExchangeData> container, int targetStarNum, bool needUIShow = true)
        {
            if (container == null || targetStarNum <= 0)
                return;
            if (!IsOpenStarExchange())
                return;
            //记录目前已选择的卡片对应的星星数量
            int totalStars = 0;
            // 在container中依次选择卡片
            _ExecuteBestAdd(container, targetStarNum, ref totalStars);
            
            int overflow = totalStars - targetStarNum;
            // 如果星星数量大于兑换所需，则开始剔除
            if (overflow > 0)
            {
                _ExecuteBestRemove(container, overflow, ref totalStars);
                //剔除逻辑执行完毕后 再计算一下当前的溢出值 作为最后的星星结余转为固定值
                overflow = totalStars - targetStarNum;
            }
            //如果container结果需要在ui上展示 则按ui显示规则重新排序一下
            if (needUIShow)
                container.Sort(_BestSelectCardSort);
            //打印结果
            _DebugResult(container, targetStarNum,  totalStars,  overflow);
        }

        //根据传入的目标星星数量targetStarNum，以及container内选卡的情况，找出最合适的卡片选择方案，并且选卡结果星星总数尽量趋近于targetStarNum
        //注意：本方法会基于container中原有的卡片选择情况 只在他原本选择的卡片范围内做出挑选，尽量给出最佳方案
        private bool _SelectBestCardBaseOnContainer(List<UICardStarExchangeData> container, int targetStarNum, out int overflow)
        {
            overflow = 0;
            if (container == null || targetStarNum <= 0)
                return false;
            //累计目前container中的卡片星星数量
            int totalStars = 0;
            foreach (var card in container)
            {
                if (card.SelectNum > 0)
                    totalStars += card.SelectNum * card.CardStar;
            }
            //如果目前已选的卡片星星数量小于或等于targetStarNum 则均不处理
            if (totalStars < targetStarNum)
                return false;
            if (totalStars == targetStarNum)
                return true;
            //如果大于targetStarNum 则说明有溢出 尝试减小溢出
            overflow = totalStars - targetStarNum;
            // 如果星星数量大于兑换所需，则开始剔除
            _ExecuteBestRemove(container, overflow, ref totalStars);
            //剔除逻辑执行完毕后 再计算一下当前的溢出值 作为最后的星星结余转为固定值
            overflow = totalStars - targetStarNum;
            //打印结果
            _DebugResult(container, targetStarNum,  totalStars,  overflow);
            return true;
        }

        //最佳选择卡片的排序方式：1.星级低的在前 2.重复拥有数量多的在前 3.ObjCard.id小的在前
        private int _BestSelectCardSort(UICardStarExchangeData x, UICardStarExchangeData y)
        {
            // 1.先按星级升序排序，星级低的在前
            var starComparison = x.CardStar.CompareTo(y.CardStar);
            if (starComparison != 0) return starComparison;
            // 2.再按重复拥有的数量降序排序，重复拥有数量多的在前
            var quantityComparison = y.RepeatOwnCount.CompareTo(x.RepeatOwnCount);
            if (quantityComparison != 0) return quantityComparison;
            // 3.最后按ObjCard.id升序排序，ID小的在前
            return x.CardId.CompareTo(y.CardId);
        }
        
        //最佳剔除卡片的排序方式：1.星级高的在前 2.重复拥有数量少的在前 3.ObjCard.id小的在前
        private int _BestRemoveCardSort(UICardStarExchangeData x, UICardStarExchangeData y)
        {
            // 1.先按星级降序排序，星级高的在前
            int starComparison = y.CardStar.CompareTo(x.CardStar);
            if (starComparison != 0) return starComparison;
            // 2.再按重复拥有的数量升序排序，重复拥有数量少的在前
            int quantityComparison = x.RepeatOwnCount.CompareTo(y.RepeatOwnCount);
            if (quantityComparison != 0) return quantityComparison;
            // 3.最后按ObjCard.id升序排序，ID小的在前
            return x.CardId.CompareTo(y.CardId);
        }

        private void _ExecuteBestAdd(List<UICardStarExchangeData> container, int targetStarNum, ref int totalStars)
        {
            foreach (var card in container)
            {
                //记录之前先清0 即无视之前的选择方案
                card.SelectNum = 0;
                // 如果达到兑换所需星星数，则继续遍历 确保后面的数据SelectNum都被清零
                if (totalStars >= targetStarNum) 
                    continue;
                int starsFromCard = card.CardStar;      // 每张卡片可以转换的星星数
                int cardQuantity = card.RepeatOwnCount;  // 当前卡片重复拥有的数量
                // 如果当前选择卡片后星星数已经达到所需的总数，跳出循环
                for (int i = 0; i < cardQuantity; i++)
                {
                    totalStars += starsFromCard; // 累加星星
                    // 更新已选的卡片数量
                    card.SelectNum++;
                    // 如果达到兑换所需星星数，退出循环
                    if (totalStars >= targetStarNum) break;
                }
            }
        }

        private void _ExecuteBestRemove(List<UICardStarExchangeData> container, int overflow, ref int totalStars)
        {
            //按最佳剔除卡片的排序方式进行排序 之后执行剔除逻辑
            container.Sort(_BestRemoveCardSort);

            // 从选中的卡片中逐步剔除，直到溢出值尽量减小到0
            foreach (var card in container)
            {
                int starsPerCard = card.CardStar;      // 每张卡片可以转换的星星数
                int selectedQuantity = card.SelectNum; // 当前卡片当前已选的数量
                //卡片星级小于溢出值 说明可以尝试减小溢出值
                if (starsPerCard <= overflow)
                {
                    //遍历依次减少卡片的选择数量 直到当前溢出值溢出值为0或者小于卡片星级 说明不能再减少这张卡片了
                    for (int i = 0; i < selectedQuantity; i++)
                    {
                        overflow -= starsPerCard;
                        totalStars -= starsPerCard;
                        card.SelectNum--;
                        // 如果溢出值已经处理完，或者小于当前卡片星级，则停止剔除当前卡片
                        if (overflow == 0 || starsPerCard > overflow) break;
                    }
                }
                else
                {
                    // 因为当前顺序是卡片星级由高到低排序，如果当前卡片的星星值大于溢出值，则跳过此卡片
                    // 并继续尝试下一张卡片，看看能否进一步减少溢出值
                    continue;
                }
                // 一旦溢出为0，跳出循环
                if (overflow == 0) break;
            }
            // 如果遍历所有卡片后，无法进一步减少溢出值，就保留现有的溢出值
        }

        private void _DebugResult(List<UICardStarExchangeData> container, int targetStarNum, int totalStars, int overflow)
        {
#if UNITY_EDITOR
            // 输出最终选择的卡片和星星数量
            Debug.Log("最终选择的卡片组合：");
            foreach (var card in container)
            {
                if (card.SelectNum > 0) // 只输出已选数量大于0的卡片
                {
                    Debug.Log($"ID: {card.CardId}, RepeatOwnCount: {card.RepeatOwnCount}, CardStar: {card.CardStar}, SelectNum: {card.SelectNum}");
                }
            }
            Debug.Log($"目标星星数：{targetStarNum}, 总星星数: {totalStars}, 溢出数： {overflow}");
#endif
        }

        //消耗星星兑换奖励时打点
        public void TrackCardStarExchange(int exchangeId, int costFixedNum, int totalCostStarNum, int leftStarNum)
        {
            var activity = GetCardActivity();
            if (activity == null) return;
            var roundData = GetCardRoundData();
            var albumData = roundData?.TryGetCardAlbumData();
            if (albumData == null) return;
            DataTracker.TrackCardStarExchange(CurCardActId, activity.From, albumData.CardAlbumId, exchangeId, costFixedNum, totalCostStarNum, leftStarNum);
        }
    }
}


