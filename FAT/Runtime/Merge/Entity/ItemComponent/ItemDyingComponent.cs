/**
 * @Author: handong.liu
 * @Date: 2021-02-23 18:43:17
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;
using fat.gamekitdata;

namespace FAT.Merge
{
    public class ItemDyingComponent: ItemComponentBase, IEffectReceiver
    {
        // public bool canTriggerDie => isSelfTrigger && mLifeCounter >= mConfig.DieTime * 1000;
        // public int dieCounterMilli => mLifeCounter;
        // public ComMergeDying config => mConfig;
        // public bool isSelfTrigger {
        //     get {
        //         if(item?.HasComponent(ItemComponentType.Skill, true) ?? false)
        //         {
        //             return false;
        //         }
        //         if(item?.HasComponent(ItemComponentType.ClickSouce, true) ?? false)
        //         {
        //             var click = item.GetItemComponent(ItemComponentType.ClickSouce) as ItemClickSourceComponent;
        //             if(click.config.DeadItem <= 0 && (click.config.StageCount > 0 || click.config.ReviveTime <= 0))     //会死，并且click没有配置死亡产出，则需用dying
        //             {
        //                 return false;
        //             }
        //         }

        //         return true;
        //     }
        // } 
        // private int mLifeCounter;
        // private ComMergeDying mConfig;
        
        bool IEffectReceiver.WillReceiveEffect(SpeedEffect effect)
        {
            return effect is SpeedEffect;
        }

        public static bool SerializeDelta(MergeItem newData, MergeItem oldData)
        {
            if(oldData.ComDying != null && oldData.ComDying.Equals(newData.ComDying))
            {
                newData.ComDying = null;
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool Validate(ItemComConfig config)
        {
            return config?.dyingConfig != null;
        }

        // public override void OnSerialize(MergeItem itemData)
        // {
        //     base.OnSerialize(itemData);
        //     long lifeStart = 0;
        //     int lifeCounter = 0;
        //     // if(item.parent == null)
        //     // {
        //     lifeCounter = mLifeCounter;
        //     // }
        //     // else
        //     // {
        //     //     lifeStart = (item.world.lastTickMilli - mLifeCounter) / 1000;
        //     // }
        //     itemData.ComDying = new ComDying();
        //     itemData.ComDying.Life = lifeCounter;
        //     itemData.ComDying.Start = lifeStart;
        // }

        // public override void OnDeserialize(MergeItem itemData)
        // {
        //     base.OnDeserialize(itemData);
        //     if(itemData.ComDying != null)
        //     {
        //         mLifeCounter = itemData.ComDying.Life;
        //     }
        // }

        // public int CalculateSpeedCost()
        // {
        //     if(!canTriggerDie && isSelfTrigger)
        //     {
        //         return EL.MathUtility.LerpInteger(0, mConfig.SpeedCost, mConfig.DieTime * 1000 - mLifeCounter, mConfig.DieTime * 1000);
        //     }
        //     else
        //     {
        //         return 0;
        //     }
        // }

        // public bool SpeedDie()
        // {
        //     if(!canTriggerDie && isSelfTrigger)
        //     {
        //         var dieTotalMilli = mConfig.DieTime * 1000;
        //         Update(Mathf.Max(0, dieTotalMilli - mLifeCounter));
        //         item.world.TriggerItemEvent(item, ItemEventType.ItemEventSpeedUp);
        //         return true;
        //     }
        //     return false;
        // }

        // public override int CalculateUpdateMilli(int maxMilli)
        // {
        //     if(mConfig.AutoDie && isSelfTrigger)
        //     {
        //         var dieTime = mConfig.DieTime * 1000;
        //         return Mathf.Clamp(dieTime - mLifeCounter, 0, maxMilli);
        //     }
        //     else
        //     {
        //         return maxMilli;
        //     }
        // }

        // protected override void OnUpdateInactive(int dt)
        // {
        //     base.OnUpdateInactive(dt);
        //     if(!isSelfTrigger)
        //     {
        //         return;
        //     }
        //     _UpdateRecharge(dt, false);
        // }

        // protected override void OnUpdate(int dt)
        // {
        //     if(isGridNotMatch)
        //     {
        //         return;
        //     }
        //     base.OnUpdate(dt);
        //     if(!isSelfTrigger)
        //     {
        //         return;
        //     }
        //     // DebugEx.FormatTrace("ItemDyingComponent::OnUpdate {0}", item);
        //     dt = EffectUtility.CalculateMilliBySpeedEffect(this, dt);
        //     _UpdateRecharge(dt, true);
        // }

        // public override void OnPositionChange()
        // {
        //     base.OnPositionChange();
        //     RefreshEnableState();
        // }

        // protected override void OnPostAttach()
        // {
        //     base.OnPostAttach();
        //     mConfig = Env.Instance.GetItemComConfig(item.tid).dyingConfig;
        //     mLifeCounter = 0;
        // }

        // protected override bool RefreshEnableStateImp(bool enableSetting)
        // {
        //     bool enable = base.RefreshEnableStateImp(enableSetting);
        //     if(enable && mLifeCounter == 0 && isGridNotMatch)
        //     {
        //         enable = false;
        //     }
        //     return enable;
        // }

        // private void _UpdateRecharge(int dt, bool allowDie)
        // {
        //     if (item.parent != null && mLifeCounter < mConfig.DieTime * 1000)
        //     {
        //         mLifeCounter += dt;
        //         if (canTriggerDie && allowDie)
        //         {
        //             if (mConfig.AutoDie)
        //             {
        //                 item.ExecuteAfterUpdate(() =>
        //                 {
        //                     item.parent.TriggerItemDie(item, null, out var state);
        //                 });
        //             }
        //             else
        //             {
        //                 item.parent.TriggerItemStatusChange(item);
        //             }
        //         }
        //     }
        // }
    }
}