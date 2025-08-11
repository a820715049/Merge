/*
 * @Author: qun.chao
 * @Date: 2025-03-31 15:40:32
 */

using DG.Tweening;

namespace FAT.Merge
{
    public interface ISpawnEffect {}

    public interface ISpawnEffectWithTrail : ISpawnEffect
    {
        void AddTrail(MBItemView view, Tween tween);
    }
}