/*
 * @Author: qun.chao
 * @Date: 2025-04-23 12:03:26
 */

using UnityEngine;

namespace MiniGame.SlideMerge
{
    public class Ball
    {
        public int Id;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Mass;
        public float Radius => RadiusOrig * RadiusScale;
        public float RadiusOrig;
        public float RadiusScale = 1f;   // 半径缩放系数 新生成的棋子需要有从小到大的出生动画
        public bool IsNewBorn;
        public bool IsDead;
        public bool IsAttracting;
    }
}