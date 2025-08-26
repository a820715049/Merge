/*
 * @Author: tang.yan
 * @Description: 无限礼包三格版数据类 
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/dzsfo3rsrqly3u5s
 * @Date: 2024-07-04 15:07:03
 */
using System;
using System.Collections.Generic;
using Config;
using fat.rawdata;
using fat.gamekitdata;
using EL;
using static fat.conf.Data;
using static FAT.RecordStateHelper;

namespace FAT {
    public class PackEndlessThree : GiftPack {
        
        //礼包数据结构
        public class EndlessThreePkgData
        {
            public int BelongPkgId;     //所属礼包id EndlessPkgRewardConfig
            public int IapId;           //IAPProduct.id 默认0 代表免费
            public int PackId;          //IAPPack.id    付费礼包内容
            public List<RewardConfig> PayRewardInfo;
            public int FreeId;          //IAPFree.id    免费奖励内容 没有时为0
            public List<RewardConfig> FreeRewardInfo;
            public int TokenRewardNum;  //本档位在领取时会同步发放的token奖励数量 默认0代表无奖励

            public EndlessThreePkgData(int pkgId, int iapId, int packId, int freeId, int tokenRewardNum)
            {
                BelongPkgId = pkgId;
                IapId = iapId;
                PackId = packId;
                FreeId = freeId;
                TokenRewardNum = tokenRewardNum;
            }
            
            public void RefreshReward(bool albumActive)
            {
                //付费奖励
                if (IapId > 0 && PackId > 0 && Game.Manager.iap.FindIAPPack(PackId, out var iapPackConf))
                {
                    if (PayRewardInfo == null)
                    {
                        PayRewardInfo = new List<RewardConfig>();
                    }
                    else
                    {
                        PayRewardInfo.Clear();
                    }
                    var payRewardConfig = albumActive ? iapPackConf.RewardAlbum : iapPackConf.Reward;
                    foreach (var config in payRewardConfig)
                    {
                        PayRewardInfo.Add(config.ConvertToRewardConfig());
                    }
                }
                //免费奖励
                else if (FreeId > 0 && Game.Manager.configMan.TryGetIapFreeConfig(FreeId, out var freeConf))
                {
                    if (FreeRewardInfo == null)
                    {
                        FreeRewardInfo = new List<RewardConfig>();
                    }
                    else
                    {
                        FreeRewardInfo.Clear();
                    }
                    var strList = albumActive ? freeConf.AlbumReward : freeConf.FreeReward;
                    foreach (var rewardStr in strList)
                    {
                        FreeRewardInfo.Add(rewardStr.ConvertToRewardConfig());
                    }
                }
            }
        }
        
        //配置数据
        private EndlessThreePack _conf;
        //当前rTag对应的礼包数据  顺序决定界面显示顺序
        private List<EndlessThreePkgData> _curPkgDataList = new List<EndlessThreePkgData>();
        //开始循环的index ，总共的奖励格子数  都是默认从0开始计数
        private int _startLoopIndex, _totalIndex;
        //当前可领取的奖励index  本活动借用底层的buycount字段，用于记录礼包奖励的购买/领取次数
        private int _curRecIndex;
        //基于用户分层确定的IAPPack.id和IAPFree.id对应的List  会按照礼包组的顺序进行记录  没有相关配置时默认以0占位
        private List<int> _gradeIdList = new List<int>();   
        //积分进度条相关
        private List<int> _tokenConfIdList = new List<int>();   //EndlessThreePackToken.id对应List(记录用户分层后的实际id)
        private int _progressConfId;//经过用户分层后的进度条配置id EndlessThreePackProgress.id
        private int _progressPhase; //当前进度条所处阶段 从0开始 根据阶段值读配置获取当前的最大进度以及达成后可获得的奖励
        private int _progressNum;   //当前进度条的进度值
        //购买成功Action
        private Action<IList<RewardCommitData>> _purchaseSuccCb = null;
        
        public override UIResAlt Res { get; } = new(UIConfig.UIEndlessPack);
        public override int PackId { get => _GetCurPackId(); set => _SetCurPackId(value); }
        public override int ThemeId => _conf.EventTheme;
        public override int StockTotal => -1;    //限购次数默认设成-1次 代表可以无限购买
        public override bool Valid => _curRecIndex >= 0 && _curRecIndex <= _totalIndex;

        public PackEndlessThree() { }


        public PackEndlessThree(ActivityLite lite_)
        {
            Lite = lite_;
            _conf = GetEndlessThreePack(lite_.Param);
            RefreshTheme(popupCheck_:true);
        }

        public override void SetupFresh()
        {
            _InitInfoWithUserGrade();
            _InitConfigList();
            _UpdateCurRecIndex();
            _purchaseSuccCb = null;
        }
        
        public override void SaveSetup(ActivityInstance data_) {
            base.SaveSetup(data_);
            var any = data_.AnyState;
            int startIndex = 3;
            foreach (var gradeId in _gradeIdList)
            {
                any.Add(ToRecord(startIndex++, gradeId));
            }
            foreach (var tokenId in _tokenConfIdList)
            {
                any.Add(ToRecord(startIndex++, tokenId));
            }
            any.Add(ToRecord(startIndex++, _progressConfId));
            any.Add(ToRecord(startIndex++, _progressPhase));
            any.Add(ToRecord(startIndex, _progressNum));
        }

        public override void LoadSetup(ActivityInstance data_) {
            base.LoadSetup(data_);
            var any = data_.AnyState;
            int startIndex = 3;
            if (any.Count > startIndex)
            {
                var totalCount = _conf?.Detailid.Count ?? 0;
                var curMinIndex = startIndex;
                var curMaxIndex = startIndex + 3 * totalCount;
                for (int i = curMinIndex; i < curMaxIndex; i++)
                {
                    var gradeId = ReadInt(i, any);
                    _gradeIdList.Add(gradeId);
                }
                curMinIndex = startIndex + 3 * totalCount;
                curMaxIndex = startIndex + 4 * totalCount;
                for (int i = curMinIndex; i < curMaxIndex; i++)
                {
                    var tokenConfId = ReadInt(i, any);
                    _tokenConfIdList.Add(tokenConfId);
                }
                _progressConfId = ReadInt(curMaxIndex++, any);
                _progressPhase = ReadInt(curMaxIndex++, any);
                _progressNum = ReadInt(curMaxIndex, any);
            }
            _InitConfigList();
            //在加载到存档后 更新当前index
            _UpdateCurRecIndex();
        }

        //获取当前可以在界面上显示的礼包数据对应的index list 默认取前3个 会有循环
        public void FillCurShowPkgIndexList(List<int> indexList, out bool isLast)
        {
            isLast = false;
            //获取前先更新下index 用于应对可能存在的补单逻辑 目前补单逻辑在执行完后是不会告诉礼包数据的
            _UpdateCurRecIndex();
            if (indexList == null || _conf == null)
                return;
            //礼包循环次数 默认-1代表无限循环 0代表不循环 大于0代表循环1+指定次数
            var cycleNum = _conf.CycleNum;
            if (cycleNum <= -1)
            {
                _FillListWithCycle(indexList);
                isLast = false;
            }
            else if (cycleNum == 0)
            {
                isLast = _FillListWithCheckLast(indexList);
            }
            else
            {
                var curCycleNum = _CalCurCycleNum();
                if (curCycleNum < cycleNum)
                {
                    _FillListWithCycle(indexList);
                    isLast = false;
                }
                else if (curCycleNum == cycleNum)
                {
                    isLast = _FillListWithCheckLast(indexList);
                }
                else
                {
                    _FillListAtLast(indexList);
                    isLast = true;
                }
            }
        }

        public EndlessThreePkgData GetRewardDataByIndex(int index)
        {
            if (_curPkgDataList.TryGetByIndex(index, out var data))
            {
                return data;
            }
            else
            {
                return null;
            }
        }
        
        //检查传入的index对应的奖励是否可以领取
        public bool CheckCanGetReward(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex > _totalIndex || targetIndex != _curRecIndex)
                return false;
            return true;
        }
        
        public bool CheckHasGetReward(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex > _totalIndex || targetIndex >= _curRecIndex)
                return false;
            return true;
        }
        
        //尝试领取格子上的免费/付费奖励 默认只能领取第一个格子 后面的格子都为上锁状态
        public void TryGetReward(int targetIndex)
        {
            if (!CheckCanGetReward(targetIndex))
                return;
            EndlessThreePkgData rewardData = _curPkgDataList[targetIndex];
            if (rewardData == null)
                return;
            int iapId = rewardData.IapId;
            //免费奖励
            if (iapId == 0)
            {
                using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
                {
                    foreach (var reward in rewardData.FreeRewardInfo)
                    {
                        //构造基础奖励
                        rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                    }
                    //发放可能会有的token奖励
                    var tokenReward = _TryGetTokenReward(rewardData.TokenRewardNum);
                    //购买次数+1 这里认为免费领取也算购买 便于逻辑计算index
                    BuyCount++;
                    //领奖后续处理
                    _CollectRewardFinish(rewards, targetIndex, true, tokenReward);
                }
            }
            //付费奖励
            else if (iapId > 0)
            {
                _purchaseSuccCb = (rewards) =>
                {
                    //发放可能会有的token奖励
                    var tokenReward = _TryGetTokenReward(rewardData.TokenRewardNum);
                    //buyCount会在底层自己+1
                    //领奖后续处理
                    _CollectRewardFinish(rewards, targetIndex, false, tokenReward);
                };
                Game.Manager.activity.giftpack.Purchase(this, null, pack =>
                {
                    //失败时通知界面关闭
                    MessageCenter.Get<MSG.GAME_ENDLESS_THREE_PGK_REC_FAIL>().Dispatch();
                });
            }
        }
        
        //正常购买成功或补单成功后的回调
        public override void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_)
        {
            base.PurchaseSuccess(packId_, rewards_, late_);
            //如果发生了补单，底层会帮忙发放付费档的奖励
            if (late_)
            {
                //检查当前的付费档packId是否和补单的packId一样
                //如果一样 则按正常情况处理
                if (Goods.packId == packId_)
                {
                    //如果回调不为空 说明是在线时触发补单 此时直接走回调
                    if (_purchaseSuccCb != null)
                    {
                        _purchaseSuccCb.Invoke(rewards_);
                        _purchaseSuccCb = null;
                    }
                    //如果回调为空 说明是杀进程后再进游戏触发补单
                    //这种情况下 付费档的奖励会在底层给到 但是档位上可能存在的进度值不会给发 这里发一下
                    //另外 由于回调缺失 导致有些方法没走到 这里调用一下
                    else
                    {
                        //收集packId_对应的可能有的进度值奖励
                        _CollectTokenRewardByPackId(packId_);
                        //更新index， buyCount会在底层自己+1
                        _UpdateCurRecIndex();
                        //打点
                        DataTracker.endless_three_reward.Track(this, _curRecIndex, false);
                    }
                }
                //如果不一样 则说明发生了多次补单 此时需要补这一轮的所有免费奖励 同时BuyCount-- 保持进度不受影响
                else
                {
                    _CollectAllFreeRewardByPackId(packId_);
                    BuyCount--;
                }
                return;
            }
            //未发生补单时，正常发奖
            _purchaseSuccCb?.Invoke(rewards_);
            _purchaseSuccCb = null;
        }
        
        //补单发生时 根据packId找到对应奖励数据，发放其可能存在的token奖励
        private void _CollectTokenRewardByPackId(int packId)
        {
            var pkgDataCount = _curPkgDataList.Count;
            if (packId <= 0 || pkgDataCount <= 0)
                return;
            for (int i = 0; i < pkgDataCount; i++)
            {
                var pkgData = _curPkgDataList[i];
                if (pkgData.IapId <= 0 || pkgData.PackId <= 0) 
                    continue;
                if (pkgData.PackId == packId)
                {
                    //发放可能会有的token奖励
                    var tokenReward = _TryGetTokenReward(pkgData.TokenRewardNum);
                    if (tokenReward != null) 
                        Game.Manager.rewardMan.CommitReward(tokenReward);
                    break;
                }
            }
        }
        
        //补单发生时 根据packId找到其所在的轮次 发放这一轮中所有的免费奖励
        private void _CollectAllFreeRewardByPackId(int packId)
        {
            var pkgDataCount = _curPkgDataList.Count;
            if (packId <= 0 || pkgDataCount <= 0)
                return;
            var payIndex = -1;  //传入的packId对应的礼包index
            var nextPayIndex = -1;  //结合当前index找到的下一个需要付费的index
            for (int i = 0; i < pkgDataCount; i++)
            {
                var pkgData = _curPkgDataList[i];
                if (pkgData.IapId <= 0 || pkgData.PackId <= 0) 
                    continue;
                if (payIndex < 0 && pkgData.PackId == packId)
                    payIndex = i;
                if (nextPayIndex < 0 && payIndex >= 0 && i > payIndex)
                    nextPayIndex = i;
                //都找到后break
                if (payIndex >= 0 && nextPayIndex >= 0)
                    break;
            }
            //循环完后若没有找到下一个需要付费的index 说明当前所有的付费档都买完了 后面剩下的都是可领取的免费档
            if (nextPayIndex < 0)
            {
                nextPayIndex = pkgDataCount;
            }
            //都没找到则返回
            if (payIndex < 0 || nextPayIndex < 0)
                return;
            var rewardMan = Game.Manager.rewardMan;
            for (int i = 0; i < pkgDataCount; i++)
            {
                if (i > payIndex && i < nextPayIndex)
                {
                    var pkgData = _curPkgDataList[i];
                    //只领取免费奖励
                    if (pkgData.IapId == 0)
                    {
                        foreach (var reward in pkgData.FreeRewardInfo)
                        {
                            rewardMan.CommitReward(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                        }
                        //发放可能会有的token奖励
                        var tokenReward = _TryGetTokenReward(pkgData.TokenRewardNum);
                        if (tokenReward != null) rewardMan.CommitReward(tokenReward);
                        //打点
                        DataTracker.endless_three_reward.Track(this, i + 1, true);
                    }
                }
            }
        }

        private void _CollectRewardFinish(IList<RewardCommitData> rewards, int targetIndex, bool isFree, RewardCommitData tokenReward)
        {
            //更新index
            _UpdateCurRecIndex();
            //发奖励 刷界面
            MessageCenter.Get<MSG.GAME_ENDLESS_THREE_PGK_REC_SUCC>().Dispatch(targetIndex, rewards, tokenReward);
            //打点
            DataTracker.endless_three_reward.Track(this, _curRecIndex, isFree);
            //检查活动是否结束
            _CheckWillEnd();
        }
        
        //若当前礼包不循环或循环次数有限 则在每次领完奖励后检查一下活动是否可以结束
        private void _CheckWillEnd()
        {
            if (_conf == null)
                return;
            var cycleNum = _conf.CycleNum;
            if (cycleNum <= -1)
            {
                //无限循环 只有活动到结束时间时才会结束
            }
            else if (cycleNum == 0)
            {
                //只进行一轮 不循环 
                if (_curRecIndex > _totalIndex)
                {
                    Game.Manager.activity.EndImmediate(this,false);
                }
            }
            else
            {
                var curCycleNum = _CalCurCycleNum();
                //当前进行的循环次数已超过指定循环次数 则活动结束
                if (curCycleNum > cycleNum)
                {
                    Game.Manager.activity.EndImmediate(this,false);
                }
            }
        }
        
        //当活动结束时 如果有未领取的免费奖励 则自动领取
        public override void WhenEnd()
        {
            //活动结束时 根据当前进度和配置的可循环次数检查是否有玩家付费了但没领取的免费奖励， 如果有则帮忙领取
            _TryHelpCollectReward();
            //重置数据
            _curRecIndex = 0;
            _startLoopIndex = 0; 
            _totalIndex = 0;
            _curPkgDataList.Clear();
            _purchaseSuccCb = null;
        }
        
        private void _InitInfoWithUserGrade()
        {
            if (_conf == null)
                return;
            _gradeIdList.Clear();
            _tokenConfIdList.Clear();
            var userGradeMan = Game.Manager.userGradeMan;
            foreach (var pkgId in _conf.Detailid)
            {
                var pkgConf = GetOneEndlessThreePackDetailByFilter(x => x.Detailid == pkgId);
                if (pkgConf != null)
                {
                    var iapPackId = pkgConf.PackGrpId > 0 ? userGradeMan.GetTargetConfigDataId(pkgConf.PackGrpId) : 0;
                    _gradeIdList.Add(iapPackId);
                    var iapFreeId1 = pkgConf.FreeOneGrpId > 0 ? userGradeMan.GetTargetConfigDataId(pkgConf.FreeOneGrpId) : 0;
                    _gradeIdList.Add(iapFreeId1);
                    var iapFreeId2 = pkgConf.FreeTwoGrpId > 0 ? userGradeMan.GetTargetConfigDataId(pkgConf.FreeTwoGrpId) : 0;
                    _gradeIdList.Add(iapFreeId2);
                    var tokenConfId = userGradeMan.GetTargetConfigDataId(pkgConf.TokenReward);
                    _tokenConfIdList.Add(tokenConfId);
                }
            }
            _progressConfId = userGradeMan.GetTargetConfigDataId(_conf.Progress);
        }
        
        private void _InitConfigList()
        {
            _curPkgDataList.Clear();
            _startLoopIndex = 0;
            _totalIndex = 0;
            if (_conf == null)
                return;
            int startLoopPkgId = _conf.StartLoopPkgId;  //开始循环的detail id, 不配默认从礼包组第一个开始循环
            int index = 0;
            var tokenConfIndex = 0;
            foreach (var pkgId in _conf.Detailid)
            {
                var pkgConf = GetOneEndlessThreePackDetailByFilter(x => x.Detailid == pkgId);
                if (pkgConf != null)
                {
                    //确定循环起始index
                    if (pkgId == startLoopPkgId)
                    {
                        _startLoopIndex = _curPkgDataList.Count;
                    }
                    
                    //拿到当前循环组对应的所有token奖励信息
                    var tokenList = 
                        _tokenConfIdList.TryGetByIndex(tokenConfIndex, out var tokenConfId) 
                            ? (GetEndlessThreePackToken(tokenConfId)?.TokenList) 
                            : null;
                    var tokenIndex = 0;
                    var tokenRewardNum = 0;
                    //当前礼包配置了内购商品
                    if (pkgConf.PackGrpId > 0 && _gradeIdList.TryGetByIndex(index, out var packId))
                    {
                        //构造付费奖励
                        if (packId > 0 && Game.Manager.iap.FindIAPPack(packId, out var iapPackConf) && iapPackConf.IapId > 0)
                        {
                            tokenList?.TryGetByIndex(tokenIndex, out tokenRewardNum);
                            var payData = new EndlessThreePkgData(pkgConf.Detailid, iapPackConf.IapId, packId, 0, tokenRewardNum);
                            _curPkgDataList.Add(payData);
                        }
                        //配了内购商品但构造奖励时发现没有配置，则传-1代表非法
                        else
                        {
                            tokenList?.TryGetByIndex(tokenIndex, out tokenRewardNum);
                            var payData = new EndlessThreePkgData(pkgConf.Detailid, -1, -1, 0, tokenRewardNum);
                            _curPkgDataList.Add(payData);
                        }
                        tokenIndex++;
                    }
                    index++;

                    //当前礼包配置了赠品1
                    if (pkgConf.FreeOneGrpId > 0 && _gradeIdList.TryGetByIndex(index, out var freeId1))
                    {
                        //构造免费奖励
                        if (freeId1 > 0)
                        {
                            tokenList?.TryGetByIndex(tokenIndex, out tokenRewardNum);
                            var freeData = new EndlessThreePkgData(pkgConf.Detailid, 0, 0, freeId1, tokenRewardNum);
                            _curPkgDataList.Add(freeData);
                        }
                        else
                        {
                            //配了免费奖励但构造时发现没有配置，则跳过不构造数据类 
                        }
                        tokenIndex++;
                    }
                    index++;
                    
                    //当前礼包配置了赠品2
                    if (pkgConf.FreeTwoGrpId > 0 && _gradeIdList.TryGetByIndex(index, out var freeId2))
                    {
                        //构造免费奖励
                        if (freeId2 > 0)
                        {
                            tokenList?.TryGetByIndex(tokenIndex, out tokenRewardNum);
                            var freeData = new EndlessThreePkgData(pkgConf.Detailid, 0, 0, freeId2, tokenRewardNum);
                            _curPkgDataList.Add(freeData);
                        }
                        else
                        {
                            //配了免费奖励但构造时发现没有配置，则跳过不构造数据类 
                        }
                    }
                    index++;
                }
                tokenConfIndex++;
            }
            _totalIndex = _curPkgDataList.Count - 1;
        }
        
        //更新当前index 超过上限后重新从循环index开始
        private void _UpdateCurRecIndex()
        {
            if (_curPkgDataList.Count < 1)
                return;
            //购买次数小于总Index时 直接赋值
            if (BuyCount <= _totalIndex)
            {
                _curRecIndex = BuyCount;
            }
            else
            {
                //礼包循环次数
                int cycleNum = _conf?.CycleNum ?? 0;
                //0代表不循环
                if (cycleNum == 0)
                {
                    _curRecIndex = BuyCount;    //这么设置的话 Valid值会变为false 此时活动应立即结束了
                }
                //默认-1代表无限循环 大于0代表只能循环1+指定次数  这两个其实都是循环 所以用一样的逻辑
                //不同的是 后续的检查逻辑如果发现循环次数到达指定次数了 会直接结束活动
                else
                {
                    //大于总Index时 取余 然后还要考虑开始循环的index
                    int cycleLength = _totalIndex - _startLoopIndex + 1;    //参与循环的index长度
                    int tempIndex = cycleLength != 0 ? (BuyCount - _startLoopIndex) % cycleLength : 0;  //确定在循环范围内的当前位置
                    _curRecIndex = _startLoopIndex + tempIndex;
                }
            }
            //刷新index后 再设置下当前的礼包信息Content
            RefreshContent();
        }

        private int _GetCurPackId()
        {
            if (_curRecIndex < 0 || _curRecIndex > _totalIndex || _curPkgDataList.Count < 1)
                return 0;
            EndlessThreePkgData rewardData = _curPkgDataList[_curRecIndex];
            return rewardData?.PackId ?? 0;
        }

        private void _SetCurPackId(int value_) {
            if (_curRecIndex < 0 || _curRecIndex > _totalIndex || _curPkgDataList.Count < 1)
                return;
            EndlessThreePkgData rewardData = _curPkgDataList[_curRecIndex];
            if (rewardData != null) rewardData.PackId = value_;
        }
        
        //无限礼包允许Content为空(免费档)的情况
        public override void RefreshContent() {
            Content = GetIAPPack(PackId);
            RefreshPack();
        }

        public override void RefreshPack() {
            base.RefreshPack();
            var albumActive = Game.Manager.activity.IsActive(fat.rawdata.EventType.CardAlbum);
            //刷新奖励
            foreach (var endlessPkgData in _curPkgDataList)
            {
                endlessPkgData.RefreshReward(albumActive);
            }
        }
        
        public override BonusReward MatchPack(int packId_)
        {
            //如果想要的packId和当前序号对应的goods.packId不一致，说明发生了补单，这时new一个返回去，避免补单对应的奖励发不出去
            if (Goods.packId != packId_)
            {
                var iapPack = GetIAPPack(packId_);
                if (iapPack != null)
                {
                    var reward = new BonusReward();
                    reward.Refresh(iapPack);
                    DebugEx.FormatInfo("Endless Delivery, needPackId = {0}, curPackId = {1} ", packId_, Goods.packId);
                    return reward;
                }
            }
            return Goods;
        }

        //填充list 超过上限时直接从循环位置开始继续填充
        private void _FillListWithCycle(ICollection<int> indexList)
        {
            //默认_curRecIndex为第一个
            int startIndex = _curRecIndex;
            indexList.Add(startIndex);
            //之后再依次找3个  界面上最多显示3个 但为了tween动画表现流畅 会在显示区域外多一个module提前刷新好 因而总共需要4个数据
            for (int i = 1; i <= 3; i++)
            {
                int curIndex = startIndex + i;
                int addIndex = curIndex <= _totalIndex ? curIndex : _startLoopIndex + curIndex - _totalIndex - 1;
                indexList.Add(addIndex);
            }
        }

        //填充list 检查目前是否到可填充末尾 若到达末尾则默认填充最后3个
        private bool _FillListWithCheckLast(ICollection<int> indexList)
        {
            bool isLast = false;
            var totalCount = _curPkgDataList.Count;
            //界面最多显示3个 但为了tween动画表现流畅 会在显示区域外多一个module提前刷新好 因而总共需要4个数据
            if (_curRecIndex <= totalCount - 3)
            {
                for (int i = _curRecIndex; i < _curRecIndex + 4 && i < totalCount; i++)
                {
                    indexList.Add(i);
                }
                isLast = false;
            }
            else
            {
                _FillListAtLast(indexList);
                isLast = true;
            }
            return isLast;
        }

        //填充list 只填充最后3个
        private void _FillListAtLast(ICollection<int> indexList)
        {
            var totalCount = _curPkgDataList.Count;
            for (int i = totalCount - 3; i < totalCount && i >= 0; i++)
            {
                indexList.Add(i);
            }
        }

        //根据当前信息计算当前的循环次数
        private int _CalCurCycleNum()
        {
            var cycleLength = _totalIndex - _startLoopIndex + 1;
            var curCycleNum = 0;
            var offsetIndex = BuyCount - _totalIndex;
            if (offsetIndex > 0)
            {
                if (offsetIndex % cycleLength != 0)
                    curCycleNum = offsetIndex / cycleLength + 1;
                else
                    curCycleNum = offsetIndex / cycleLength;
            }
            return curCycleNum;
        }
        
        private void _TryHelpCollectReward()
        {
            if (_conf == null || _curPkgDataList == null || _curPkgDataList.Count < 1)
                return;
            bool canHelp = false;   //检查目前情况是否可以帮助领取
            var cycleNum = _conf.CycleNum;
            if (cycleNum <= -1)
            {
                //无限循环时默认始终可以帮忙领取
                canHelp = true;
            }
            else if (cycleNum == 0)
            {
                //只进行一轮不循环时 若当前index超过总的index 则不帮忙
                if (_curRecIndex <= _totalIndex)
                {
                    canHelp = true;
                }
            }
            else
            {
                var curCycleNum = _CalCurCycleNum();
                //循环指定轮次时，若当前进行的循环次数已超过指定循环次数 则不帮忙
                if (curCycleNum <= cycleNum)
                {
                    canHelp = true;
                }
            }
            //目前情况是否不可以帮助领取 则返回
            if (!canHelp)
                return;

            int firstPayIndex = -1;     //第一个需要付费的index 从0开始
            int curNextPayIndex = -1;   //结合当前index找到的下一个需要付费的index 从0开始
            for (int i = 0; i < _curPkgDataList.Count; i++)
            {
                var pkgData = _curPkgDataList[i];
                if (pkgData.IapId > 0)
                {
                    if (firstPayIndex < 0)
                        firstPayIndex = i;
                    if (curNextPayIndex < 0 && _curRecIndex <= i)
                    {
                        curNextPayIndex = i;
                    }
                }
            }
            //循环完后若没有找到下一个需要付费的index 说明当前所有的付费档都买完了 后面剩下的都是可领取的免费档
            if (curNextPayIndex < 0)
            {
                curNextPayIndex = _curPkgDataList.Count;
            }
            if (_curRecIndex > firstPayIndex)
            {
                var index = _curRecIndex;
                var rewardMan = Game.Manager.rewardMan;
                for (int i = 0; i < _curPkgDataList.Count; i++)
                {
                    if (i >= _curRecIndex && i < curNextPayIndex)
                    {
                        var pkgData = _curPkgDataList[i];
                        //只领取免费奖励
                        if (pkgData.IapId == 0)
                        {
                            foreach (var reward in pkgData.FreeRewardInfo)
                            {
                                rewardMan.CommitReward(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                            }
                            //发放可能会有的token奖励
                            var tokenReward = _TryGetTokenReward(pkgData.TokenRewardNum);
                            if (tokenReward != null) rewardMan.CommitReward(tokenReward);
                            index++;
                            //打点
                            DataTracker.endless_three_reward.Track(this, index, true);
                        }
                    }
                }
            }
        }
        
        #region 进度条相关逻辑

        public EndlessThreePackProgress GetCurProgressConf()
        {
            return GetEndlessThreePackProgress(_progressConfId);
        }

        public int GetCurTokenId()
        {
            return _conf?.TokenId ?? 0;
        }

        public int GetCurProgressPhase()
        {
            return _progressPhase;
        }

        public int GetCurProgressNum()
        {
            return _progressNum;
        }

        //检查所有进度条是否都完成
        public bool CheckProgressFinish()
        {
            var progressConf = GetCurProgressConf();
            //所有进度值都完成
            return progressConf == null || _progressPhase >= progressConf.ProgressNode.Count || _progressPhase >= progressConf.ProgressReward.Count;
        }

        //外部调用增加当前进度值
        public void TryAddProgressNum(int tokenId, int tokenNum)
        {
            if (tokenId <= 0 || tokenId != GetCurTokenId() || tokenNum <= 0)
                return;
            var progressConf = GetCurProgressConf();
            //所有进度值都完成
            if (progressConf == null || _progressPhase >= progressConf.ProgressNode.Count || _progressPhase >= progressConf.ProgressReward.Count)
                return;
            //增加进度值
            var finalProgressNum = _progressNum + tokenNum;
            //检测是否达到本阶段最大值
            var curProgressMax = progressConf.ProgressNode[_progressPhase];
            if (finalProgressNum < curProgressMax)
            {
                _progressNum = finalProgressNum;
                MessageCenter.Get<MSG.GAME_ENDLESS_THREE_PROG_CHANGE>().Dispatch(finalProgressNum, null, -1);
                return;
            }
            //若达到则发阶段奖励且阶段值+1
            //这里默认单次加的进度值不会一下完成多段进度
            finalProgressNum -= curProgressMax; //此时finalProgressNum代表多余的进度值
            var reward = progressConf.ProgressReward[_progressPhase].ConvertToRewardConfig();
            if (reward != null)
            {
                var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.endless_three_progress);
                var t = Game.TimestampNow();
                //处理极端情况 活动结束时有未领取的免费奖励，且免费奖励上带有token奖励，且领取了该token奖励后正好可以完成当前进度条并获得奖励
                if (t >= endTS || t < startTS)
                {
                    Game.Manager.rewardMan.CommitReward(commit);    //此时直接commit奖励 没有相关界面表现
                }
                else
                {
                    if (UIManager.Instance.IsOpen(Res.ActiveR))
                    {
                        //进度条动画  进度条满时发奖流程  发完奖后若还有多余的进度值，剩余的进度值也要有动画
                        MessageCenter.Get<MSG.GAME_ENDLESS_THREE_PROG_CHANGE>().Dispatch(finalProgressNum, commit, curProgressMax);
                    }
                    else
                    {
                        //若各种原因导致加积分发奖时界面没有打开（如补单补了积分），此时直接commit奖励，没有相关界面表现
                        Game.Manager.rewardMan.CommitReward(commit);
                    }
                }
                //领取进度条奖励时打点
                var isFinal = _progressPhase == progressConf.ProgressNode.Count - 1;
                DataTracker.endless_three_progreward.Track(this, _progressPhase + 1, isFinal);
            }
            _progressPhase++;
            _progressNum = finalProgressNum;
        }
        
        //领取指定档位中的token奖励
        private RewardCommitData _TryGetTokenReward(int tokenRewardNum)
        {
            if (tokenRewardNum <= 0 || _conf == null)
                return null;
            var reward = Game.Manager.rewardMan.BeginReward(GetCurTokenId(), tokenRewardNum, ReasonString.purchase);
            return reward;
        }

        #endregion
    }
}