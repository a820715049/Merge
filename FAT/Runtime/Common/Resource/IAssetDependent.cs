using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace EL.Resource {
    using static PoolMapping;

    [Flags]
    public enum AssetTag {
        Required = 1<<0,
        Optional = 1<<1,
        All = 0x7FFFFFFF,
    }

    public interface IAssetDependent {
        public IEnumerable<(string, AssetTag)> ResEnumerate();
    }

    public abstract class AssetDependency : IAssetDependent {
        public IAssetDependent target;
        public UniTask pending;
        public bool Pending => pending.Status == UniTaskStatus.Pending;
        public int Priority { get; set; }
        public static Action<IAssetDependent> AssetReady;

        public void Setup(IAssetDependent target_, int priority_) {
            target = target_;
            Priority = priority_;
        }

        public IEnumerable<(string, AssetTag)> ResEnumerate()
            => target.ResEnumerate();

        public abstract void WhenReady();
    }

    public static partial class ResManager {
        public static bool Prepare(AssetDependency target_) {
            if (target_.Pending) {
                DebugEx.Info($"asset_dep skip for {target_.target}");
                return false;
            }
            AssetDependency.AssetReady ??= t_ => {
                if (t_ is not AssetDependency d) {
                    DebugEx.Warning($"asset_dep target mismatch: {t_} is not {nameof(AssetDependency)}");
                    return;
                }
                d.WhenReady();
            };
            target_.pending = Prepare(target_, AssetDependency.AssetReady);
            return target_.pending.Status == UniTaskStatus.Pending;
        }

        public static UniTask Prepare(IAssetDependent target_, Action<IAssetDependent> WhenReady_) {
            static async UniTask W(ResourceAsyncTask t_) {
                await t_;
            }
            static async UniTask A(List<UniTask> list_, IAssetDependent target_, Action<IAssetDependent> WhenReady_) {
                await UniTask.WhenAll(list_);
                DebugEx.Info($"asset_dep ready for {target_}");
                WhenReady_?.Invoke(target_);
            }
            using var _ = PoolMappingAccess.Borrow(out List<UniTask> list);
            foreach(var (r, t) in target_.ResEnumerate()) {
                var rr = r.ConvertToAssetConfig();
                if (string.IsNullOrEmpty(rr.Group)) continue;
                if (!IsGroupLoaded(rr.Group)) {
                    var l = LoadGroup(rr.Group);
                    if ((t & AssetTag.Required) != 0) list.Add(W(l));
                }
            }
            if (list.Count > 0) {
                DebugEx.Info($"asset_dep prepare for {target_} count:{list.Count}");
                return A(list, target_, WhenReady_);
            }
            return default;
        }
    }
}