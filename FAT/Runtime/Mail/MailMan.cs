/*
 * @Author: qun.chao
 * @Date: 2020-11-20 12:11:16
 */
using System.Collections;
using System.Collections.Generic;
using EL;
using System;
using fat.gamekitdata;
using fat.msg;

namespace FAT
{
    public class MailMan : IGameModule
    {
        public int MailCount => mMailList.Count;
        public Mail RecordClickLinkMail = null;
        private readonly List<Mail> mMailList = new();

        public void LoadConfig() { }

        public void Reset()
        {
            mMailList.Clear();
            RecordClickLinkMail = null;
            MessageCenter.Get<MSG.GAME_LOGIN_FIRST_SYNC>().AddListenerUnique(RequestMail);
        }

        public void Startup()
        {
            RequestMail();
        }

        /// <summary>
        /// 用于外部入口红点显示 可以在每次刷新时做准确判断 忽略过期邮件
        /// </summary>
        public bool HasNewMail()
        {
            var cur = Game.Instance.GetTimestampSeconds();
            foreach (var mail in mMailList)
            {
                if (mail.ExpireTime < cur)
                    continue;
                if (!mail.IsRead)
                    return true;
            }
            return false;
        }

        public bool HasReward()
        {
            foreach (var mail in mMailList)
            {
                if (!mail.IsClaimed && mail.Rewards.Count > 0)
                    return true;
            }
            return false;
        }

        public void RemoveExpiredMail()
        {
            _RemoveExpiredMail();
        }

        public void FillMailList(List<Mail> container)
        {
            container.AddRange(mMailList);
        }

        private void _RemoveExpiredMail()
        {
            bool removed = false;
            var cur = Game.Instance.GetTimestampSeconds();
            for (int i = mMailList.Count - 1; i >= 0; --i)
            {
                if (mMailList[i].ExpireTime < cur)
                {
                    DebugEx.FormatInfo("[MAIL] remove expired mail id {0}, expTime {1}, nowTime {2}", mMailList[i].Id, mMailList[i].ExpireTime, cur);
                    mMailList.RemoveAt(i);
                    removed = true;
                }
            }
            if (removed)
                _NotifyListChange();
        }

        private void _NotifyListChange()
        {
            MessageCenter.Get<MSG.GAME_MAIL_LIST_CHANGE>().Dispatch();
        }

        private void _NotifyStateChange()
        {
            MessageCenter.Get<MSG.GAME_MAIL_STATE_CHANGE>().Dispatch();
        }

        public void RequestMail()
        {
            if (!Game.Manager.networkMan.isLogin) return;
            //TODO
            Game.Instance.StartCoroutineGlobal(_RequestMail());
        }

        private IEnumerator _RequestMail()
        {
            var resp = Game.Manager.networkMan.FetchMail();
            yield return resp;

            if (!resp.isSuccess)
            {
                DebugEx.FormatInfo("[MAIL] NetHandlerer.FetchEmail ----> fetch mail fail : {0}", resp.error);
                yield break;
            }

            var body = resp.result as GetMailResp;

            mMailList.Clear();
            mMailList.AddRange(body.Mails);

            // // =============================
            // // add test data
            // long time = Game.Instance.GetTimestampSeconds();
            // for (int i = 0; i < 5; ++i)
            // {
            //     var email = new Gamekitdata.Mail
            //     {
            //         Id = 1000 + (ulong)i,
            //         Type = Gamekitdata.MailType.System,
            //         IsRead = false,
            //         ExpireTime = time + 100L * i,
            //     };
            //     mMailList.Add(email);
            // }
            // // =============================

            DebugEx.FormatInfo("[MAIL] fetch mail success with {0}", body.Mails.Count);

            _RemoveExpiredMail();

            _NotifyListChange();

            ReciveMailTrack();

        }

        public void SetMailRead(ulong mailId)
        {
            Game.Instance.StartCoroutineGlobal(_RequestReadMail(mailId));
        }

        private IEnumerator _RequestReadMail(ulong mailId)
        {
            // 本地直接设置已读
            var m = mMailList.Find(x => x.Id == mailId);
            if (m != null)
            {
                m.IsRead = true;
            }
            _NotifyStateChange();

            var resp = Game.Manager.networkMan.ReadMail(mailId);
            yield return resp;

            if (!resp.isSuccess)
            {
                DebugEx.FormatInfo("[MAIL] NetHandler. ----> read mail fail : {0}", resp.error);
                yield break;
            }
            else
            {
                DebugEx.FormatInfo("[MAIL] mail read success");

                DataTracker.mail_read.Track(m.Type.ToString(), m.FromUid.ToString(), Game.Manager.mailMan.SingleMailHasReward(m), IsLinkMail(m));
            }
        }

        public SimpleResultedAsyncTask<List<RewardCommitData>> RequestAllMailReward()
        {
            var task = new SimpleResultedAsyncTask<List<RewardCommitData>>();
            Game.Instance.StartCoroutineGlobal(_RequestAllMailReward(task));
            return task;
        }

        private IEnumerator _RequestAllMailReward(SimpleResultedAsyncTask<List<RewardCommitData>> holder)
        {
            var resp = Game.Manager.networkMan.FetchAllMailReward();
            yield return resp;

            if (!resp.isSuccess)
            {
                DebugEx.FormatInfo("[MAIL] NetHandler.FetchAllEmailAward ----> fetch all award fail : {0}", resp.error);
                holder.Fail();
                yield break;
            }
            else
            {
                DebugEx.FormatInfo("[MAIL] all mail reward success");
            }

            var body = resp.result as ClaimAllMailRewardResp;

            // set read & rewarded
            var cache = new HashSet<ulong>();
            foreach (var id in body.MailIds)
            {
                cache.AddIfAbsent(id);
            }
            foreach (var mail in mMailList)
            {
                if (cache.Contains(mail.Id))
                {
                    if (!mail.IsRead)
                    {
                        DataTracker.mail_read.Track(mail.Type.ToString(), mail.FromUid.ToString(), Game.Manager.mailMan.SingleMailHasReward(mail), IsLinkMail(mail));
                    }
                    if (!mail.IsClaimed)
                    {
                        DataTracker.mail_reward.Track(mail.Type.ToString(), mail.FromUid.ToString(), mail.Title, mail.Rewards, Game.Manager.mailMan.SingleMailHasReward(mail), IsLinkMail(mail));
                    }

                    mail.IsRead = true;
                    mail.IsClaimed = true;
                }
            }

            // reward
            var rewardList = new List<RewardCommitData>();
            _MergeRewards(body.Rewards, rewardList);
            Game.Manager.archiveMan.SendImmediately(true);            //save immediately to local
            holder.Success(rewardList);
            _NotifyStateChange();
        }

        public SimpleResultedAsyncTask<List<RewardCommitData>> RequestMailReward(ulong mailId)
        {
            var task = new SimpleResultedAsyncTask<List<RewardCommitData>>();
            Game.Instance.StartCoroutineGlobal(_RequestMailReward(task, mailId));
            return task;
        }

        private IEnumerator _RequestMailReward(SimpleResultedAsyncTask<List<RewardCommitData>> holder, ulong mailId)
        {
            var resp = Game.Manager.networkMan.FetchMailReward(mailId);
            yield return resp;

            if (!resp.isSuccess)
            {
                DebugEx.FormatInfo("[MAIL] NetHandler.FetchMailReward ----> fetch award fail : {0}", resp.error);
                holder.Fail();
                yield break;
            }
            else
            {
                DebugEx.FormatInfo("[MAIL] mail reward success");
            }

            // set rewarded
            var m = mMailList.Find(x => x.Id == mailId);
            if (m != null)
            {
                m.IsClaimed = true;
                DataTracker.mail_reward.Track(m.Type.ToString(), m.FromUid.ToString(), m.Title, m.Rewards, Game.Manager.mailMan.SingleMailHasReward(m), IsLinkMail(m));
            }

            var body = resp.result as GetMailRewardResp;
            var rewardList = new List<RewardCommitData>();
            _MergeRewards(body.Rewards, rewardList);
            Game.Manager.archiveMan.SendImmediately(true);            //save immediately to local
            holder.Success(rewardList);

            _NotifyStateChange();
        }

        // 合并重复奖励
        private void _MergeRewards(IDictionary<int, int> rewards, List<RewardCommitData> rewardList)
        {
            var rewardMgr = Game.Manager.rewardMan;
            var merge = new Dictionary<int, int>();
            rewardList.Clear();

            foreach (var kv in rewards)
            {
                if (merge.ContainsKey(kv.Key))
                {
                    merge[kv.Key] = merge[kv.Key] + kv.Value;
                }
                else
                {
                    merge.Add(kv.Key, kv.Value);
                }
            }

            // commit
            foreach (var kv in merge)
            {
                var reward = rewardMgr.BeginReward(kv.Key, kv.Value, ReasonString.mail);
                rewardList.Add(reward);
            }

            // sort
            rewardList.Sort((a, b) => a.rewardId - b.rewardId);
        }


        private void ReciveMailTrack()
        {
            foreach (var mail in mMailList)
            {
                DataTracker.mail_receive.Track(mail.Type.ToString(), mail.FromUid.ToString(), SingleMailHasReward(mail), mail.Title, mail.Rewards, IsLinkMail(mail));
            }
        }

        public bool SingleMailHasReward(Mail mail)
        {
            if (mail != null)
            {
                if (!mail.IsClaimed && mail.Rewards.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsNoneLinkMail()
        {
            foreach (var mail in mMailList)
            {
                if (mail.LinkType == MailLinkType.MailExternalLink)
                    continue;
                if (!mail.IsClaimed && mail.Rewards.Count > 0)
                    return true;
            }
            return false;
        }

        public bool IsLinkMail(Mail mail)
        {
            if (mail != null)
            {
                if (mail.LinkType == MailLinkType.MailExternalLink)
                    return true;
            }
            return false;
        }
    }
}