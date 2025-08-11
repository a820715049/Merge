/*
 * @Author: qun.chao
 * @Date: 2023-09-26 14:58:17
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public class SpawnBonusContext
    {
        public MergeWorld world;
        public MergeGrid[] boardGrids;
        public Item from;
        public Item result;
        public bool isBubble;
        public int energyCost;
        public ItemSpawnReason reason;
    }
    public interface ISpawnBonusHandler
    {
        int priority {get;}         //越小越先出
        void Process(SpawnBonusContext context);
        void OnRegister();
        void OnUnRegister();
    }
}