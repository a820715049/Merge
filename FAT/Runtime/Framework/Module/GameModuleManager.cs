/*
 * @Author: qun.chao
 * @Date: 2023-10-12 16:33:58
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using EL;

namespace FAT
{
    public class GameModuleManager
    {
        // 模块作用域 从上到下包含关系
        public enum ModuleScope
        {
            AppLaunch,
            GameStart,
            ConfReady,
            ArchiveLoaded,
        }

        class GameModuleProvider
        {
            public Type instType;
            public ModuleScope scope;
            public IGameModule inst;
            private readonly Func<IGameModule> imp;

            public void EnsureInst()
            {
                inst ??= imp?.Invoke();
            }

            public GameModuleProvider(Func<IGameModule> imp)
            {
                this.imp = imp;
            }
        }

        private List<GameModuleProvider> gameModuleProviders = new List<GameModuleProvider>();
        public void RigisteAllModules()
        {
            _Module(ModuleScope.AppLaunch, UIManager.Instance);
            AutoRegisterModulesInGameManager();
        }

        private void AutoRegisterModulesInGameManager()
        {
            var mgr = Game.Manager;
            var type = mgr.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                var ft = field.FieldType;
                if (ft.ImplementsInterface(typeof(IGameModule)))
                {
                    var mark = field.GetCustomAttribute<ModuleMark>();
                    var inst = field.GetValue(mgr);
                    var p = new GameModuleProvider(() => Activator.CreateInstance(ft) as IGameModule)
                    {
                        scope = mark.Scope,
                        instType = ft,
                        inst = inst as IGameModule,
                    };
                    gameModuleProviders.Add(p);
                    if (inst == null)
                    {
                        p.EnsureInst();
                        field.SetValue(mgr, p.inst);
                        // DebugEx.Info($"make instance {mark.Scope} {ft}");
                    }
                    else
                    {
                        // DebugEx.Info($"bind instance {mark.Scope} {ft}");
                    }
                }
            }
        }

        public void FillModuleByType<T>(IList<T> handlers)
        {
            foreach (var provider in gameModuleProviders)
            {
                if (provider.inst is T tt)
                {
                    handlers?.Add(tt);
                }
            }
        }

        public void ResetAll(ModuleScope scope, bool within = false)
        {
            if (within)
                JobWithinScope(scope, Imp_Reset);
            else
                JobForScope(scope, Imp_Reset);
        }

        public void LoadConfigAll(ModuleScope scope, bool within = false)
        {
            if (within)
                JobWithinScope(scope, Imp_LoadConfig);
            else
                JobForScope(scope, Imp_LoadConfig);
        }

        public void StartupAll(ModuleScope scope, bool within = false)
        {
            if (within)
                JobWithinScope(scope, Imp_Startup);
            else
                JobForScope(scope, Imp_Startup);
        }

        public void UpdateAll(ModuleScope scope, float dt, bool within = false)
        {
            _tickDeltaTime = dt;
            if (within)
                JobWithinScope(scope, Imp_Update);
            else
                JobForScope(scope, Imp_Update);
        }
        
        public void SecondUpdateAll(ModuleScope scope, float dt, bool within = false)
        {
            _secondDeltaTime = dt;
            if (within)
                JobWithinScope(scope, Imp_SecondUpdate);
            else
                JobForScope(scope, Imp_SecondUpdate);
        }

        private void _Module<T>(ModuleScope lc, T inst) where T : IGameModule, new()
        {
            var p = new GameModuleProvider(() => new T());
            p.scope = lc;
            p.instType = typeof(T);
            p.inst = inst;
            gameModuleProviders.Add(p);
        }

        private void JobForScope(ModuleScope scope, Action<GameModuleProvider> job)
        {
            foreach (var provider in gameModuleProviders)
            {
                if (provider.scope == scope)
                {
                    job?.Invoke(provider);
                }
            }
        }

        private void JobWithinScope(ModuleScope scope, Action<GameModuleProvider> job)
        {
            foreach (var provider in gameModuleProviders)
            {
                if (provider.scope >= scope)
                {
                    if (provider.inst != null)
                        job?.Invoke(provider);
                }
            }
        }

        #region module interface

        private void Imp_Reset(GameModuleProvider provider)
        {
            provider.EnsureInst();
            provider.inst.Reset();
            // DebugEx.Info($"[GameModule] reset {provider.instType}");
        }

        private void Imp_LoadConfig(GameModuleProvider provider)
        {
            provider.inst.LoadConfig();
        }

        private void Imp_Startup(GameModuleProvider provider)
        {
            provider.inst.Startup();
        }

        private float _tickDeltaTime = 0f;
        private void Imp_Update(GameModuleProvider provider)
        {
            if (provider.inst is IUpdate updateProvider)
            {
                updateProvider.Update(_tickDeltaTime);
            }
        }
        
        private float _secondDeltaTime = 0f;
        private void Imp_SecondUpdate(GameModuleProvider provider)
        {
            if (provider.inst is ISecondUpdate secondUpdateProvider)
            {
                secondUpdateProvider.SecondUpdate(_secondDeltaTime);
            }
        }

        #endregion
    }
}