/*
 * @Author: qun.chao
 * @Date: 2023-09-28 18:16:17
 */
namespace FAT.Merge
{
    public class DisposeBonusContext
    {
        public MergeWorld world;
        public Item item;
        public Item dieToTarget; // 有时候item被特定目标dispose 通过记录target可以更准确的识别奖励发生的位置
        public ItemDeadType deadType;
    }
    public interface IDisposeBonusHandler
    {
        int priority {get;}         //越小越先出
        void Process(DisposeBonusContext context);
        void OnRegister();
        void OnUnRegister();
    }
}