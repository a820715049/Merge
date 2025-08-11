/*
 * @Author: qun.chao
 * @Date: 2023-10-24 16:18:43
 */
using System;
using System.Collections.Generic;
using fat.gamekitdata;
using FAT.Merge;

namespace FAT
{
    public enum OrderProviderType
    {
        Common = 0,
        Detector,
        Random,
    }

    public static class OrderProviderTypeExtensions
    {
        public static OrderProviderTypeMask ToMask(this OrderProviderType pt)
        {
            return (OrderProviderTypeMask)(1 << (int)pt);
        }

        public static int ToIntMask(this OrderProviderType pt)
        {
            return 1 << (int)pt;
        }
    }

    public enum OrderProviderTypeMask
    {
        Common = 1 << OrderProviderType.Common,
        Detector = 1 << OrderProviderType.Detector,
        Random = 1 << OrderProviderType.Random,

        All = Common | Detector | Random,
    }


    public interface IOrderProvider
    {
        void Reset();
        void Update();
        int FillActiveOrders(List<IOrderData> container);
        void Serialize(IList<OrderRecord> records);
        void Deserialize(IList<OrderRecord> records, MergeWorldTracer _tracer, IOrderHelper _helper, Action<List<IOrderData>, List<IOrderData>> onDirty);
        bool TryFinishOrder(int id, ICollection<RewardCommitData> rewards);
        void SetDirty();
    }
}