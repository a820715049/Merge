/*
 * @Author: tang.yan
 * @Description: 无限礼包数据类 
 * @Date: 2024-01-02 16:01:45
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
    public class PackEndless : GiftPack {
        
        //礼包数据结构
        public class EndlessPkgData
        {
            public int BelongPkgId;     //所属礼包id EndlessPkgRewardConfig
            public int IapId;           //IAPProduct.id 默认0 代表免费
            public int PackId;          //IAPPack.id 
            public List<RewardConfig> PayRewardInfo;
            public RewardConfig FreeRewardInfo;
            public int TokenRewardNum;  //本档位在领取时会同步发放的token奖励数量 默认0代表无奖励
            private string _freeRewardStr;
            private string _freeAlbumRewardStr;

            public EndlessPkgData(int pkgId, int iapId, int packId, int tokenRewardNum, string freeRewardStr = "", string freeAlbumRewardStr = "")
            {
                BelongPkgId = pkgId;
                IapId = iapId;
                PackId = packId;
                TokenRewardNum = tokenRewardNum;
                _freeRewardStr = freeRewardStr;
                _freeAlbumRewardStr = freeAlbumRewardStr;
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
                else
                {
                    FreeRewardInfo = albumActive ? _freeAlbumRewardStr?.ConvertToRewardConfig() : _freeRewardStr?.ConvertToRewardConfig();
                }
            }
        }
        
        //配置数据
        private EndlessPack endlessPackConf;
        //当前rTag对应的礼包数据  顺序决定界面显示顺序
        private List<EndlessPkgData> _curPkgDataList = new List<EndlessPkgData>();
        //开始循环的index ，总共的奖励格子数  都是默认从0开始计数
        private int _startLoopIndex, _totalIndex;
        //当前可领取的奖励index  本活动借用底层的buycount字段，用于记录礼包奖励的购买/领取次数
        private int _curRecIndex;
        private List<int> _packIdList = new List<int>();    //IAPPack.id对应List(记录用户分层后的实际id)
        private List<int> _freeIdList = new List<int>();    //IAPFree.id对应List(记录用户分层后的实际id)
        //积分进度条相关
        private List<int> _tokenConfIdList = new List<int>();   //EndlessPackToken.id对应List(记录用户分层后的实际id)
        private int _progressConfId;//经过用户分层后的进度条配置id EndlessPackProgress.id
        private int _progressPhase; //当前进度条所处阶段 从0开始 根据阶段值读配置获取当前的最大进度以及达成后可获得的奖励
        private int _progressNum;   //当前进度条的进度值
        //购买成功Action
        private Action<IList<RewardCommitData>> _purchaseSuccCb = null;
        
        public override UIResAlt Res { get; } = new(UIConfig.UIEndlessPack);
        public override int PackId { get => _GetCurPackId(); set => _SetCurPackId(value); }
        public override int ThemeId => endlessPackConf.EventTheme;
        public override int StockTotal => -1;    //限购次数默认设成-1次 代表可以无限购买
        public override bool Valid => _curRecIndex >= 0 && _curRecIndex <= _totalIndex;

        public PackEndless() { }


        public PackEndless(ActivityLite lite_)
        {
            Lite = lite_;
            endlessPackConf = GetEndlessPack(lite_.Param);
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
            foreach (var packId in _packIdList)
            {
                any.Add(ToRecord(startIndex++, packId));
            }
            foreach (var freeId in _freeIdList)
            {
                any.Add(ToRecord(startIndex++, freeId));
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
                var totalCount = endlessPackConf?.Detailid.Count ?? 0;
                var curMinIndex = startIndex;
                var curMaxIndex = startIndex + totalCount;
                for (int i = curMinIndex; i < curMaxIndex; i++)
                {
                    var packId = ReadInt(i, any);
                    _packIdList.Add(packId);
                }
                curMinIndex = startIndex + totalCount;
                curMaxIndex = startIndex + 2 * totalCount;
                for (int i = curMinIndex; i < curMaxIndex; i++)
                {
                    var freeId = ReadInt(i, any);
                    _freeIdList.Add(freeId);
                }
                curMinIndex = startIndex + 2 * totalCount;
                curMaxIndex = startIndex + 3 * totalCount;
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

        private void _InitInfoWithUserGrade()
        {
            if (endlessPackConf == null)
                return;
            _packIdList.Clear();
            _freeIdList.Clear();
            _tokenConfIdList.Clear();
            var userGradeMan = Game.Manager.userGradeMan;
            foreach (var pkgId in endlessPackConf.Detailid)
            {
                var pkgConf = GetOneEndlessPackDetailByFilter(x => x.Detailid == pkgId);
                if (pkgConf != null)
                {
                    var iapPackId = userGradeMan.GetTargetConfigDataId(pkgConf.PackGrpId);
                    _packIdList.Add(iapPackId);
                    var iapFreeId = userGradeMan.GetTargetConfigDataId(pkgConf.FreeGrpId);
                    _freeIdList.Add(iapFreeId);
                    var tokenConfId = userGradeMan.GetTargetConfigDataId(pkgConf.TokenReward);
                    _tokenConfIdList.Add(tokenConfId);
                }
            }
            _progressConfId = userGradeMan.GetTargetConfigDataId(endlessPackConf.Progress);
        }

        //获取当前可以在界面上显示的礼包数据对应的index list 默认取前6个 会有循环
        public void FillCurShowPkgIndexList(List<int> indexList = null)
        {
            //获取前先更新下index 用于应对可能存在的补单逻辑 目前补单逻辑在执行完后是不会告诉礼包数据的
            _UpdateCurRecIndex();
            if (indexList == null)
                return;
            //默认_curRecIndex为第一个
            int startIndex = _curRecIndex;
            indexList.Add(startIndex);
            //之后再依次找5个
            for (int i = 1; i <= 5; i++)
            {
                int curIndex = startIndex + i;
                int addIndex = curIndex <= _totalIndex ? curIndex : _startLoopIndex + curIndex - _totalIndex - 1;
                indexList.Add(addIndex);
            }
        }
        
        public EndlessPkgData GetRewardDataByIndex(int index)
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
        
        //尝试领取格子上的免费/付费奖励 默认只能领取第一个格子 后面的格子都为上锁状态
        public void TryGetReward(int targetIndex)
        {
            if (!CheckCanGetReward(targetIndex))
                return;
            EndlessPkgData rewardData = _curPkgDataList[targetIndex];
            if (rewardData == null)
                return;
            int iapId = rewardData.IapId;
            //免费奖励
            if (iapId == 0)
            {
                using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
                {
                    //构造基础奖励
                    rewards.Add(Game.Manager.rewardMan.BeginReward(rewardData.FreeRewardInfo.Id, rewardData.FreeRewardInfo.Count, ReasonString.purchase));
                    //发放可能会有的token奖励
                    var tokenReward = _TryGetTokenReward(rewardData.TokenRewardNum);
                    //购买次数+1 这里认为免费领取也算购买 便于逻辑计算index
                    BuyCount++;
                    //更新index
                    _UpdateCurRecIndex();
                    //发奖励 刷界面
                    MessageCenter.Get<MSG.GAME_ENDLESS_PGK_REC_SUCC>().Dispatch(targetIndex, rewards, tokenReward);
                    //打点
                    DataTracker.endless_reward.Track(this, _curRecIndex, true);
                }
            }
            //付费奖励
            else if (iapId > 0)
            {
                _purchaseSuccCb = (rewards) =>
                {
                    //发放可能会有的token奖励
                    var tokenReward = _TryGetTokenReward(rewardData.TokenRewardNum);
                    //更新index， buyCount会在底层自己+1
                    _UpdateCurRecIndex();
                    //发奖励 刷界面
                    MessageCenter.Get<MSG.GAME_ENDLESS_PGK_REC_SUCC>().Dispatch(targetIndex, rewards, tokenReward);
                    //打点
                    DataTracker.endless_reward.Track(this, _curRecIndex, false);
                };
                Game.Manager.activity.giftpack.Purchase(this, null, pack =>
                {
                    //失败时通知界面关闭
                    MessageCenter.Get<MSG.GAME_ENDLESS_PGK_REC_FAIL>().Dispatch();
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
                        DataTracker.endless_reward.Track(this, _curRecIndex, false);
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
                        rewardMan.CommitReward(rewardMan.BeginReward(pkgData.FreeRewardInfo.Id, pkgData.FreeRewardInfo.Count, ReasonString.purchase));
                        //发放可能会有的token奖励
                        var tokenReward = _TryGetTokenReward(pkgData.TokenRewardNum);
                        if (tokenReward != null) rewardMan.CommitReward(tokenReward);
                        //打点
                        DataTracker.endless_reward.Track(this, i + 1, true);
                    }
                }
            }
        }

        //当活动结束时 如果有未领取的免费奖励 则自动领取
        public override void WhenEnd()
        {
            //活动结束时 如果玩家付费了 但有没领取的免费奖励 帮忙领取
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
                            rewardMan.CommitReward(rewardMan.BeginReward(pkgData.FreeRewardInfo.Id, pkgData.FreeRewardInfo.Count, ReasonString.purchase));
                            //发放可能会有的token奖励
                            var tokenReward = _TryGetTokenReward(pkgData.TokenRewardNum);
                            if (tokenReward != null) rewardMan.CommitReward(tokenReward);
                            index++;
                            //打点
                            DataTracker.endless_reward.Track(this, index, true);
                        }
                    }
                }
            }
            //重置数据
            _curRecIndex = 0;
            _startLoopIndex = 0; 
            _totalIndex = 0;
            _curPkgDataList.Clear();
            _purchaseSuccCb = null;
        }
        
        private void _InitConfigList()
        {
            _curPkgDataList.Clear();
            _startLoopIndex = 0;
            _totalIndex = 0;
            if (endlessPackConf == null)
                return;
            int startLoopPkgId = endlessPackConf.StartLoopPkgId;
            int index = 0;
            foreach (var pkgId in endlessPackConf.Detailid)
            {
                var pkgConf = GetOneEndlessPackDetailByFilter(x => x.Detailid == pkgId);
                if (pkgConf != null && _packIdList.TryGetByIndex(index, out var packId))
                {
                    //拿到当前循环组对应的所有token奖励信息
                    var tokenList = 
                        _tokenConfIdList.TryGetByIndex(index, out var tokenConfId) 
                        ? (GetEndlessPackToken(tokenConfId)?.TokenList) 
                        : null;
                    var tokenIndex = 0;
                    var tokenRewardNum = 0;
                    //构造付费奖励
                    if (packId > 0 && Game.Manager.iap.FindIAPPack(packId, out var iapPackConf))
                    {
                        tokenList?.TryGetByIndex(tokenIndex++, out tokenRewardNum);
                        EndlessPkgData payData = new EndlessPkgData(pkgConf.Detailid, iapPackConf.IapId, packId, tokenRewardNum);
                        _curPkgDataList.Add(payData);
                    }
                    //构造付费奖励时若发现没有配置，则传-1代表非法，index为0时是免费奖励 跳过检查
                    else if (index > 0) 
                    {
                        tokenList?.TryGetByIndex(tokenIndex++, out tokenRewardNum);
                        EndlessPkgData payData = new EndlessPkgData(pkgConf.Detailid, -1, -1, tokenRewardNum);
                        _curPkgDataList.Add(payData);
                    }

                    //确定循环起始index
                    if (pkgId == startLoopPkgId)
                    {
                        _startLoopIndex = _curPkgDataList.Count - 1;
                    }
                    //构造免费奖励
                    _freeIdList.TryGetByIndex(index, out var freeId);
                    var freeConf = GetIAPFree(freeId);
                    if (freeConf != null)
                    {
                        int albumRewardCount = freeConf.AlbumReward.Count;
                        for (int i = 0; i < freeConf.FreeReward.Count; i++)
                        {
                            string freeRewards = freeConf.FreeReward[i];
                            string freeAlbumRewards = i < albumRewardCount ? freeConf.AlbumReward[i] : "";
                            tokenList?.TryGetByIndex(tokenIndex++, out tokenRewardNum);
                            EndlessPkgData freeData = new EndlessPkgData(pkgConf.Detailid, 0, 0, tokenRewardNum, freeRewards, freeAlbumRewards);
                            _curPkgDataList.Add(freeData);
                        }
                    }
                }
                index++;
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
            //大于总Index时 取余 然后还要考虑开始循环的index
            else
            {
                int index = _totalIndex - _startLoopIndex + 1;
                int tempIndex = index != 0 ? (BuyCount - _startLoopIndex) % index : 0;
                _curRecIndex = _startLoopIndex + tempIndex;
            }
            //刷新index后 再设置下当前的礼包信息Content
            RefreshContent();
        }

        private int _GetCurPackId()
        {
            if (_curRecIndex < 0 || _curRecIndex > _totalIndex || _curPkgDataList.Count < 1)
                return 0;
            EndlessPkgData rewardData = _curPkgDataList[_curRecIndex];
            return rewardData?.PackId ?? 0;
        }

        private void _SetCurPackId(int value_) {
            if (_curRecIndex < 0 || _curRecIndex > _totalIndex || _curPkgDataList.Count < 1)
                return;
            EndlessPkgData rewardData = _curPkgDataList[_curRecIndex];
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

        #region 进度条相关逻辑

        public EndlessPackProgress GetCurProgressConf()
        {
            return GetEndlessPackProgress(_progressConfId);
        }

        public int GetCurTokenId()
        {
            return endlessPackConf?.TokenId ?? 0;
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
                MessageCenter.Get<MSG.GAME_ENDLESS_PROG_CHANGE>().Dispatch(finalProgressNum, null, -1);
                return;
            }
            //若达到则发阶段奖励且阶段值+1
            //这里默认单次加的进度值不会一下完成多段进度
            finalProgressNum -= curProgressMax; //此时finalProgressNum代表多余的进度值
            var reward = progressConf.ProgressReward[_progressPhase].ConvertToRewardConfig();
            if (reward != null)
            {
                var commit = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.endless_progress);
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
                        MessageCenter.Get<MSG.GAME_ENDLESS_PROG_CHANGE>().Dispatch(finalProgressNum, commit, curProgressMax);
                    }
                    else
                    {
                        //若各种原因导致加积分发奖时界面没有打开（如补单补了积分），此时直接commit奖励，没有相关界面表现
                        Game.Manager.rewardMan.CommitReward(commit);
                    }
                }
                //领取进度条奖励时打点
                var isFinal = _progressPhase == progressConf.ProgressNode.Count - 1;
                DataTracker.endless_progreward.Track(this, _progressPhase + 1, isFinal);
            }
            _progressPhase++;
            _progressNum = finalProgressNum;
        }
        
        //领取指定档位中的token奖励
        private RewardCommitData _TryGetTokenReward(int tokenRewardNum)
        {
            if (tokenRewardNum <= 0 || endlessPackConf == null)
                return null;
            var reward = Game.Manager.rewardMan.BeginReward(GetCurTokenId(), tokenRewardNum, ReasonString.purchase);
            return reward;
        }

        #endregion
    }
}