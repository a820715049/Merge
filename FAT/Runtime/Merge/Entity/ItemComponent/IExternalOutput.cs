/*
 * @Author: qun.chao
 * @Date: 2025-04-07 18:31:42
 */
namespace FAT.Merge
{
    public interface IExternalOutput : MergeWorld.IActivityHandler
    {
        bool CanUseItem(Item source);
        bool TrySpawnItem(Item source, out int outputId, out ItemSpawnContext context);
    }
}