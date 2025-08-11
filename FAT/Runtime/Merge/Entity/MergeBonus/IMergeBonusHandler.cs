/**
 * @Author: handong.liu
 * @Date: 2023-02-17 10:29:36
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public class MergeBonusContext
    {
        public MergeWorld world;
        public MergeGrid[] boardGrids;
        public int srcId;
        public Item result;
    }
    public interface IMergeBonusHandler
    {
        int priority {get;}         //越小越先出
        void Process(MergeBonusContext context);
        void OnRegister();
        void OnUnRegister();
    }
}