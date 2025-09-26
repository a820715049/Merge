/*
 * @Author: tang.yan
 * @Description: SpecialRewardMan 虚空奖励箱 & 优先队列（partial 实现）
 * @Doc: https://centurygames.feishu.cn/wiki/RH92wZzAFif3CQkL1gWcGpoynDb?fromScene=spaceOverview
 * @Date: 2025-08-26 17:08:28
 */

using System.Collections.Generic;
using EL;
using fat.gamekitdata;

namespace FAT
{
    public partial class SpecialRewardMan : IUserDataHolder
    {
        public void SetData(LocalSaveData archive)
        {
            _virtualRewardList.Clear();
            var rewardList = archive.ClientData.PlayerGameData.SpecialRewardDataList;
            if (rewardList != null)
            {
                foreach (var rewardData in rewardList)
                {
                    var data = new VirtualRewardData()
                    {
                        Id = rewardData.Id,
                        Count = rewardData.Count,
                        Reason = new ReasonString(rewardData.Reason),
                        IsWaitCommit = false,   //读存档时默认为false
                    };
                    _virtualRewardList.Add(data);
                }
            }
        }
        
        public void FillData(LocalSaveData archive)
        {
            var rewardList = new List<SpecialRewardData>();
            foreach (var rewardData in _virtualRewardList)
            {
                var r = new SpecialRewardData
                {
                    Id = rewardData.Id,
                    Count = rewardData.Count,
                    Reason = rewardData.Reason
                };
                rewardList.Add(r);
            }
            archive.ClientData.PlayerGameData.SpecialRewardDataList.AddRange(rewardList);
        }
        
        // ===== 数据结构 =====

        private sealed class VirtualRewardData
        {
            public int Id;
            public int Count;               // 命中后 --；为 0 时删条
            public ReasonString Reason;     // id+reason 精确匹配
            //记录当前奖励是否在等待展示，运行时状态，不进存档，只在当次session有效，后续session默认为false 会在合适时机统一解析
            public bool IsWaitCommit = false;
        }

        // 方案A：吃完队头剩余再处理下一个，所以用 class 便于 Peek 后就地 Remain--
        private sealed class PriorityReq
        {
            public int Id;
            public ReasonString Reason;
            public int Remain;              // “立刻优先开这么多”的剩余次数
        }

        // ===== 字段 =====

        private readonly List<VirtualRewardData> _virtualRewardList = new(); // 虚空奖励箱（允许同 (Id,Reason) 多条）
        private readonly Queue<PriorityReq> _priorityQueue = new();          // 优先队列：谁先 Commit 谁先开（不合并）
        private bool _isPumping = false;                                      // 是否正在开/展示（串行防重入）

        // ===== 对外：Begin —— 只入虚空箱，不触发 Pump =====

        /// <summary>
        /// Begin 时机：只把 (id, count, reason) 记入虚空奖励箱。
        /// 注意：按要求，这里不触发 TryPump()。
        /// </summary>
        public void TryAddSpecialReward(int id, int count, ReasonString reason = null)
        {
            if (count <= 0)
            {
                DebugEx.FormatWarning("[SpecialRewardMan.TryAddSpecialReward] invalid count: {0} for id={1}", count, id);
                return;
            }

            var entry = new VirtualRewardData
            {
                Id = id,
                Count = count,
                Reason = reason,
                IsWaitCommit = true,
            };
            _virtualRewardList.Add(entry);

            DebugEx.FormatInfo("[SpecialRewardMan.TryAddSpecialReward] : id={0}, count={1}, reason={2}", id, count, ReasonStr(reason));
        }

        // ===== 对外：Commit —— 记录优先；未 pumping 且 UI 空闲时触发一次 Pump =====

        /// <summary>
        /// Commit 时机：记录“优先开箱”诉求（谁先 Commit 谁先开；按 id+reason 精确匹配）。
        /// 若当前不在 pumping 且 UI 空闲，则尝试 TryPump()；若正在 pumping，则仅入队，不触发。
        /// </summary>
        public void TryOpenSpecialReward(int id, int count, ReasonString reason)
        {
            if (count <= 0)
            {
                DebugEx.FormatWarning("[SpecialRewardMan.TryOpenSpecialReward] invalid count: {0} for id={1}", count, id);
                return;
            }

            _priorityQueue.Enqueue(new PriorityReq { Id = id, Reason = reason, Remain = count });
            DebugEx.FormatInfo("[SpecialRewardMan.TryOpenSpecialReward] : id={0}, reason={1}, remain={2}, CanPump={3}",
                id, ReasonStr(reason), count, !_isPumping);
            
            _TryPump();
        }

        // ===== 驱动：尝试启动一次 Pump（若未 pumping 且 UI 空闲）=====

        //进游戏时尝试弹出在之前session中没有来的及解析的特殊奖励（如玩家杀端）
        public void TryPumpWhenLogin()
        {
            if (HasOldRewardData())
            {
                UIManager.Instance.RegisterIdleAction("try_pump_when_login", 999999, _TryPump); //比小丑万能卡优先级高
            }
        }

        //目前是否有非本次session的奖励
        public bool HasOldRewardData()
        {
            foreach (var r in _virtualRewardList)
            {
                if (!r.IsWaitCommit)    //false时表示非本次session奖励
                    return true;
            }
            return false;
        }
        
        private void _TryPump()
        {
            if (_isPumping)
            {
                return;
            }

            if (!TrySelectNext(out int id, out ReasonString reason, out int idx))
            {
                return; // 无优先、无 FIFO
            }
            DebugEx.FormatInfo("[SpecialRewardMan.TryPump] : id={0}, reason={1}, index={2}, totalCount={3}", id, ReasonStr(reason), idx, _virtualRewardList.Count);
            
            _isPumping = true;
            // 一次性数据落地 + Ready + 拉起展示（每次只开 1 枚）
            var isSuccess = ResolveAndReadyOne(id, reason);
            //无论成功失败 数据层都对命中条目扣减 1；为0时删条
            if (idx >= 0 && idx < _virtualRewardList.Count)
            {
                var entry = _virtualRewardList[idx]; // 读→改→写回（List 索引器非 ref return）
                entry.Count -= 1;
                if (entry.Count <= 0)
                {
                    _virtualRewardList.RemoveAt(idx);
                }
                else
                {
                    _virtualRewardList[idx] = entry;
                }
            }
            //如果失败了，直接按照对应奖励界面关闭处理，逻辑上形成闭环
            if (!isSuccess)
            {
                var type = Game.Manager.objectMan.DeduceTypeForId(id);
                Game.Manager.specialRewardMan.OnSpecialRewardUIClosed(type, id);
            }
        }

        // ===== 回调：奖励 UI 关闭 / 一次开箱流程结束 =====

        /// <summary>
        /// 奖励 UI 关闭/本轮展示完成。
        /// - 置 _isPumping=false
        /// - 若仍有优先请求 → 继续；
        /// - 否则结束。
        /// </summary>
        public void OnSpecialRewardUIClosed(ObjConfigType type, int rewardId)
        {
            //非运行状态下 不做处理
            if (!Game.Instance.isRunning)
                return;
            //将传过来的id对应的数据设置为finish
            _TryFinishSpecialReward(type, rewardId);
            //尝试pump
            _isPumping = false;
            _TryPump();
            //在TryPump之后，如果没有处于_isPumping状态，则检查是否所有的奖励都已发完并发事件
            if (!_isPumping)
            {
                CheckSpecialRewardFinish();
            }
        }

        // ===== 内部：门禁/挑选/执行 =====

        /// <summary>
        /// 选择下一枚要开的箱：
        /// 1) 若有优先：取队头 (id,reason)，在虚空队列中找 seq 最早的同键；
        ///    - 找到：消耗队头一次（remain--，remain==0 时 Dequeue）并返回该条索引；
        ///    - 找不到：无货即丢弃该优先（Dequeue），继续尝试下一条优先；
        /// 2) 若无优先：按 FIFO 取虚空队列中 seq 最早的一条。
        /// </summary>
        private bool TrySelectNext(out int id, out ReasonString reason, out int vrIndex)
        {
            // 先尝试优先（方案A：吃完队头剩余再处理下一个）
            while (_priorityQueue.Count > 0)
            {
                var req = _priorityQueue.Peek();

                int bestIdx = -1;
                for (int i = 0; i < _virtualRewardList.Count; i++)
                {
                    var e = _virtualRewardList[i];
                    if (e.IsWaitCommit && e.Id == req.Id && EqualityComparer<ReasonString>.Default.Equals(e.Reason, req.Reason) && e.Count > 0) // null-safe
                    { 
                        bestIdx = i; 
                        break; 
                    }
                }
                if (bestIdx >= 0)
                {
                    req.Remain -= 1;
                    if (req.Remain <= 0)
                        _priorityQueue.Dequeue(); // 吃完队头后再处理下一个

                    id = _virtualRewardList[bestIdx].Id;
                    reason = _virtualRewardList[bestIdx].Reason;
                    vrIndex = bestIdx;
                    return true;
                }
                else
                {
                    // 无货即丢弃该优先
                    _priorityQueue.Dequeue();
                    DebugEx.FormatWarning("[SpecialRewardMan.TrySelectNext] priority dropped (no stock): id={0}, reason={1}", req.Id, req.Reason);
                }
            }

            // 无优先：FIFO
            if (_virtualRewardList.Count == 0)
            {
                id = 0;
                reason = null;
                vrIndex = -1;
                return false;
            }
            //取第一个IsWaitCommit为false的元素
            int fifoIdx = -1;
            for (int i = 0; i < _virtualRewardList.Count; i++)
            {
                var e = _virtualRewardList[i];
                if (!e.IsWaitCommit)
                {
                    fifoIdx = i;
                    break;
                }
            }
            //如果找不到 则不弹
            if (fifoIdx < 0)
            {
                id = 0;
                reason = null;
                vrIndex = -1;
                return false;
            }
            id = _virtualRewardList[fifoIdx].Id;
            reason = _virtualRewardList[fifoIdx].Reason;
            vrIndex = fifoIdx;
            return true;
        }

        /// <summary>
        /// 一次性数据落地 + Ready + 触发展示（每次只开 1 枚）
        /// </summary>
        private bool ResolveAndReadyOne(int id, ReasonString reason = null)
        {
            //基于一次性只处理一个奖励的流程，使的下面的调用更加闭环
            var type = Game.Manager.objectMan.DeduceTypeForId(id);
            //此时才真正解析奖励
            var isSuccess = _TryBeginSpecialReward(type, id, 1, reason);
            //数据层面设置允许表现
            _TryCommitSpecialReward(type, id, 1);
            //如果解析奖励成功 执行表现
            if (isSuccess)
            {
                _TryDisplaySpecialReward();
            }
            return isSuccess;
        }
        
        private static string ReasonStr(ReasonString reason)
            => reason == null ? "(null)" : reason.ToString();
    }
}
