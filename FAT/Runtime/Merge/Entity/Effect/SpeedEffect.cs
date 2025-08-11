/**
 * @Author: handong.liu
 * @Date: 2021-03-22 14:38:59
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    //if other effect is add by designer, create a base class for all Effect, and manage base class in "Board"
    public class SpeedEffect
    {
        public bool isDead => mDead;
        public bool isDying => milliLeft < 0;
        public int milliLeft => mMilliLeft;
        public Item creator => mCreator;
        public int speedPercent => mSpeedPercent;
        public int mSpeedPercent;         //the percentage of speed, 100 means no speed at all
        private Item mCreator;            //the creator of the effect
        private List<Vector2Int> mGrids = new List<Vector2Int>();
        private int mMilliLeft;
        private bool mDead = false;

        public SpeedEffect(Item c, int percent, int life)
        {
            mMilliLeft = life;
            mCreator = c;
            mSpeedPercent = percent;
        }

        public bool IsGridAffected(Vector2Int coord)
        {
            return mGrids.IndexOf(coord) >= 0;
        }

        public void Update(int milli)
        {
            mMilliLeft -= milli;
        }

        public void SetDead()
        {
            mDead = true;
        }
    }
}