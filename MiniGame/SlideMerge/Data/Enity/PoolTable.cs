/*
 * @Author: qun.chao
 * @Date: 2025-04-23 18:35:15
 */
using UnityEngine;
using System.Collections.Generic;
using FAT;

namespace MiniGame.SlideMerge
{
    public struct CollisionEvent
    {
        public Ball ball1;
        public Ball ball2;
        public float time;
        public bool hitWall;
        public Vector2 normal;  // 碰撞位置的法线
    }

    public class PoolTable
    {
        public List<Ball> Balls => _balls;
        public float restitution { get; set; } = 0.1f; // 弹性系数
        public float friction { get; set; } = 0.98f;
        public float frictionMin { get; set; } = 0.01f;
        public float soundThreshold { get; set; } = 200f;
        public float soundThresholdWall { get; set; } = 50f;

        private const float epsilon = float.Epsilon;

        private List<Ball> _balls = new();
        private List<CollisionEvent> _collisions = new();
        private (float minX, float maxX, float minY, float maxY) _rect;

        private bool _isOverlaping = false;

        private Vector2 _bottomLeft;
        private Vector2 _bottomRight;
        private Vector2 _topLeft;
        private Vector2 _topRight;
        private Vector2 _leftNormal;    // 向内
        private Vector2 _rightNormal;   // 向内
        private Vector2[] _trapezoidVertices;

        public void Setup(float minX, float maxX, float minY, float maxY)
        {
            _rect = (minX, maxX, minY, maxY);
        }

        public void SetupTrapezoid(Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl)
        {
            _bottomLeft = bl;
            _bottomRight = br;
            _topRight = tr;
            _topLeft = tl;
            var leftEdge = tl - bl;
            _leftNormal = new Vector2(leftEdge.y, -leftEdge.x).normalized;
            var rightEdge = tr - br;
            _rightNormal = new Vector2(-rightEdge.y, rightEdge.x).normalized;

            _trapezoidVertices = new Vector2[] { bl, br, tr, tl };
        }


        public void AddBall(Ball ball)
        {
            _balls.Add(ball);
        }

        public Ball RemoveBall(Ball ball)
        {
            _balls.Remove(ball);
            return ball;
        }

        public Ball RemoveBall(int idx)
        {
            var ball = _balls[idx];
            _balls.RemoveAt(idx);
            return ball;
        }

        public void Clear()
        {
            _balls.Clear();
            _collisions.Clear();
            _isOverlaping = false;
        }

        public void Simulate(float dt)
        {
            ResolveRadiusScale(dt);

            ResolveBoundaryOverlap();

            ResolveBallOverlap();

            ProcessCollision(dt);

            ApplyFriction(dt);
        }

        private void ProcessCollision(float dt)
        {
            var remainingTime = dt;
            var maxIterations = 100;
            var iterations = 0;

            while (remainingTime > epsilon && iterations < maxIterations)
            {
                iterations++;
                var collision = FindCollisionInTime(remainingTime);
                if (collision.HasValue)
                {
                    // 移动到目标时间
                    MoveStep(collision.Value.time);
                    // 处理碰撞
                    if (collision.Value.hitWall)
                    {
                        HandleWallCollision(collision.Value);
                    }
                    else
                    {
                        HandleBallCollision(collision.Value);
                    }
                    // 更新剩余时间
                    remainingTime -= collision.Value.time;
                }
                else
                {
                    // 时间内未发生碰撞 直接移动
                    break;
                }
            }

            if (remainingTime > 0f)
            {
                MoveStep(remainingTime);
            }

#if UNITY_EDITOR
            if (iterations >= maxIterations)
            {
                Debug.Log($"[slidemerge] ProcessCollision overflow");
            }
#endif
        }

        public void ApplyFriction(float dt)
        {
            var coe = Mathf.Max(0f, 1f - frictionMin - friction * dt);
            foreach (var ball in _balls)
            {
                if (ball.IsAttracting)
                    continue;;
                ball.Vel *= coe;
            }
        }

        private void MoveStep(float dt)
        {
            foreach (var ball in _balls)
            {
                ball.Pos += ball.Vel * dt;
            }
        }

        private void ResolveRadiusScale(float dt)
        {
            var scaleSpeed = 8f;
            foreach (var ball in _balls)
            {
                if (ball.RadiusScale < 1f)
                {
                    ball.RadiusScale += scaleSpeed * dt;
                    if (ball.RadiusScale > 1f)
                    {
                        ball.RadiusScale = 1f;
                    }
                }
            }
        }

        // 避免穿墙
        private void ResolveBoundaryOverlap()
        {
            for (var i = 0; i < _balls.Count; i++)
            {
                // ClampByBoundary(_balls[i]);
                ClampByTrapezoidBoundary(_balls[i]);
            }
        }


        private void ClampByTrapezoidBoundary(Ball ball)
        {
            // 上下边界
            var _maxY = _topLeft.y;
            var _minY = _bottomLeft.y;
            var pos = ball.Pos;
            var radius = ball.Radius;

            // 上下边界
            if (pos.y > _maxY - radius)
            {
                pos.y = _maxY - radius;
            }
            if (pos.y < _minY + radius)
            {
                pos.y = _minY + radius;
            }

            // 左右斜边
            var offsetTL = _topLeft + _leftNormal * radius;
            var offsetBL = _bottomLeft + _leftNormal * radius;
            var t_left = Mathf.InverseLerp(offsetBL.y, offsetTL.y, pos.y);
            var x_left = Mathf.Lerp(offsetBL.x, offsetTL.x, t_left);
            if (pos.x < x_left)
            {
                pos.x = x_left;
            }

            var offsetTR = _topRight + _rightNormal * radius;
            var offsetBR = _bottomRight + _rightNormal * radius;
            var t_right = Mathf.InverseLerp(offsetBR.y, offsetTR.y, pos.y);
            var x_right = Mathf.Lerp(offsetBR.x, offsetTR.x, t_right);
            if (pos.x > x_right)
            {
                pos.x = x_right;
            }
            ball.Pos = pos;
        }

        // 避免小球重叠
        private void ResolveBallOverlap()
        {
            var maxIterations = 5;
            var iter = 0;
            var tolerance = 0.1f;
            var overlapState = _isOverlaping;

            var balls = _balls;
            while (iter < maxIterations)
            {
                var anyOverlap = false;
                for (var i = 0; i < balls.Count; i++)
                {
                    var ball = balls[i];
                    for (var j = i + 1; j < balls.Count; j++)
                    {
                        var other = balls[j];
                        var diff = other.Pos - ball.Pos;
                        var sqrDistance = diff.sqrMagnitude;
                        var minDist = ball.Radius + other.Radius;
                        if (sqrDistance < minDist * minDist - tolerance)
                        {
                            anyOverlap = true;
                            var normal = diff.normalized;
                            var correction = (minDist - diff.magnitude) * 0.5f * normal;

                            // 按质量比例推开
                            var totalMass = ball.Mass + other.Mass;
                            ball.Pos -= correction * other.Mass / totalMass;
                            other.Pos += correction * ball.Mass / totalMass;

                            // 避免再次穿墙
                            // ClampByBoundary(ball);
                            // ClampByBoundary(other);
                            ClampByTrapezoidBoundary(ball);
                            ClampByTrapezoidBoundary(other);

                            var relativeVelocity = ball.Vel - other.Vel;
                            var velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

                            // 计算碰撞冲量
                            var impulseMagnitude = -(1 + restitution) * velocityAlongNormal;
                            impulseMagnitude /= 1 / ball.Mass + 1 / other.Mass;
                            var impulse = impulseMagnitude * normal;

                            // 应用速度调整
                            ball.Vel += impulse / ball.Mass;
                            other.Vel -= impulse / other.Mass;
                        }
                    }
                }
                if (!anyOverlap)
                {
                    _isOverlaping = false;
                    break;
                }
                iter++;
            }

            if (iter >= maxIterations)
            {
                _isOverlaping = true;
            }

            if (overlapState != _isOverlaping)
            {
                Debug.Log($"[slidemerge] ResolveBallOverlap {_isOverlaping}");
            }
        }

        private void ClampByBoundary(Ball ball, float offset = 0f)
        {
            var (minX, maxX, minY, maxY) = _rect;
            var radius = ball.Radius + offset;
            var pos = ball.Pos;
            var changed = false;
            if (pos.x < minX + radius)
            {
                pos.x = minX + radius;
                changed = true;
            }
            if (pos.x > maxX - radius)
            {
                pos.x = maxX - radius;
                changed = true;
            }
            if (pos.y < minY + radius)
            {
                pos.y = minY + radius;
                changed = true;
            }
            if (pos.y > maxY - radius)
            {
                pos.y = maxY - radius;
                changed = true;
            }
            if (changed)
            {
                ball.Pos = pos;
            }
        }

        private void FindProjectPoint(Vector2 ballPos, Vector2 p1, Vector2 p2, out Vector2 closestPoint, out Vector2 normal)
        {
            var lineDir = p2 - p1;
            var ballToP1 = ballPos - p1;
            // 计算投影比例 t
            var t = Vector2.Dot(ballToP1, lineDir) / lineDir.sqrMagnitude;
            t = Mathf.Clamp01(t);
            // 计算最近点
            closestPoint = p1 + t * lineDir;
            // 计算法线
            normal = (ballPos - closestPoint).normalized;
        }

        private CollisionEvent? GetTrapezoidCollision(Ball ball, Vector2[] trapezoidVertices, float maxTime)
        {
            var earliestT = maxTime;
            var bestNormal = Vector2.zero;

            for (var i = 0; i < 4; i++)
            {
                var p1 = trapezoidVertices[i];
                var p2 = trapezoidVertices[(i + 1) % 4];

                FindProjectPoint(ball.Pos, p1, p2, out var closestPoint, out var normal);
                // 计算碰撞时间（基于速度投影）
                var velocityAlongNormal = Vector2.Dot(ball.Vel, normal);
                if (velocityAlongNormal < 0) // 仅处理朝向墙的运动
                {
                    var collisionTime = (ball.Radius - Vector2.Distance(ball.Pos, closestPoint)) / -velocityAlongNormal;
                    if (collisionTime > epsilon && collisionTime < earliestT)
                    {
                        earliestT = collisionTime;
                        bestNormal = normal;
                    }
                }
            }

            if (earliestT < maxTime)
            {
                return new CollisionEvent()
                {
                    ball1 = ball,
                    time = earliestT,
                    hitWall = true,
                    normal = bestNormal,
                };
            }
            return null;
        }

        private CollisionEvent? FindCollisionInTime(float maxTime)
        {
            static int Sort(CollisionEvent a, CollisionEvent b) => a.time.CompareTo(b.time);

            _collisions.Clear();
            var balls = _balls;

            foreach (var ball in balls)
            {
                // var col = CalcBoundaryCollision(ball, maxTime);
                var col = GetTrapezoidCollision(ball, _trapezoidVertices, maxTime);
                if (col.HasValue)
                {
                    _collisions.Add(col.Value);
                }
            }

            for (var i = 0; i < balls.Count; i++)
            {
                for (var j = i + 1; j < balls.Count; j++)
                {
                    var col = CalcBallCollision(balls[i], balls[j], maxTime);
                    if (col.HasValue)
                    {
                        _collisions.Add(col.Value);
                    }
                }
            }

            // 按碰撞时间排序
            if (_collisions.Count > 0)
            {
                _collisions.Sort(Sort);
                return _collisions[0];
            }
            return null;
        }

        // 计算两个小球的最早碰撞时间
        // 假定两球匀速直线运动, 求解距离球心距离等于半径和的点
        // https://web.archive.org/web/20100629145557/http://www.gamasutra.com/view/feature/3383/simple_intersection_tests_for_games.php?page=2
        private CollisionEvent? CalcBallCollision(Ball ballA, Ball ballB, float maxTime)
        {
            var d0 = ballA.Pos - ballB.Pos;  // 初始位置差
            var vAB = ballA.Vel - ballB.Vel; // 相对速度
            var rSum = ballA.Radius + ballB.Radius;      // 半径和

            // 二次方程系数: a*t² + b*t + c = 0
            var a = vAB.sqrMagnitude;
            // 无相对运动
            if (a < epsilon) return null;
            var b = 2 * Vector2.Dot(d0, vAB);
            var c = d0.sqrMagnitude - rSum * rSum;

            // 判别式 D = b² - 4ac
            var D = b * b - 4 * a * c;
            // 无实数解（不会碰撞）
            if (D < 0) return null;

            // 计算两个解
            var sqrtD = Mathf.Sqrt(D);
            var t1 = (-b - sqrtD) / (2 * a);
            var t2 = (-b + sqrtD) / (2 * a);

            var t = maxTime;
            if (t1 > epsilon && t1 < maxTime) t = t1;
            if (t2 > epsilon && t2 < maxTime) t = Mathf.Min(t, t2);
            if (t >= maxTime) return null;

            var normal = (ballB.Pos + ballB.Vel * t - ballA.Pos - ballA.Vel * t).normalized;
            return new CollisionEvent()
            {
                ball1 = ballA,
                ball2 = ballB,
                time = t,
                normal = normal,
            };
        }

        private CollisionEvent? CalcBoundaryCollision(Ball ball, float maxTime)
        {
            var (minX, maxX, minY, maxY) = _rect;
            var normal = Vector2.zero;
            var t = maxTime;

            // 左右边界
            if (ball.Vel.x != 0)
            {
                var tLeft = (minX + ball.Radius - ball.Pos.x) / ball.Vel.x;
                var tRight = (maxX - ball.Radius - ball.Pos.x) / ball.Vel.x;
                if (tLeft >= 0 && tLeft < t)
                {
                    t = tLeft;
                    normal = Vector2.right;
                }
                if (tRight >= 0 && tRight < t)
                {
                    t = tRight;
                    normal = Vector2.left;
                }
            }

            // 上下边界
            if (ball.Vel.y != 0)
            {
                var tBottom = (minY + ball.Radius - ball.Pos.y) / ball.Vel.y;
                var tTop = (maxY - ball.Radius - ball.Pos.y) / ball.Vel.y;
                if (tBottom >= 0 && tBottom < t)
                {
                    t = tBottom;
                    normal = Vector2.up;
                }
                if (tTop >= 0 && tTop < t)
                {
                    t = tTop;
                    normal = Vector2.down;
                }
            }
            if (t < maxTime)
            {
                return new CollisionEvent()
                {
                    ball1 = ball,
                    time = t,
                    hitWall = true,
                    normal = normal,
                };
            }
            return null;
        }

        private void HandleBallCollision(CollisionEvent collision)
        {
            var a = collision.ball1;
            var b = collision.ball2;
            var normal = collision.normal;

            // 计算相对速度
            var relativeVelocity = b.Vel - a.Vel;
            var velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

            // 只有当物体相互靠近时才处理碰撞
            if (velocityAlongNormal > 0) return;

            // 计算冲量大小 (基于动量守恒和能量守恒)
            var impulseMagnitude = -(1 + restitution) * velocityAlongNormal;
            impulseMagnitude /= 1 / a.Mass + 1 / b.Mass;
            var impulse = impulseMagnitude * normal;

            if (Mathf.Abs(impulseMagnitude) / ((a.Mass + b.Mass) * 0.5f) > soundThreshold)
                Game.Manager.audioMan.TriggerSound("MiniGameSlideMergeHit");

            // 应用冲量
            a.Vel -= impulse / a.Mass;
            b.Vel += impulse / b.Mass;

            // 轻微位置修正防止抖动
            var posOffset = 0.01f;
            a.Pos += a.Vel.normalized * posOffset;
            b.Pos += b.Vel.normalized * posOffset;
        }

        private void HandleWallCollision(CollisionEvent collision)
        {
            var ball = collision.ball1;
            var normal = collision.normal;
            var velocityAlongNormal = Vector2.Dot(ball.Vel, normal);
            if (velocityAlongNormal < 0)
            {
                var impulseMagnitude = -(1 + restitution) * velocityAlongNormal;
                if (Mathf.Abs(impulseMagnitude) > soundThresholdWall)
                    Game.Manager.audioMan.TriggerSound("MiniGameSlideMergeHitTable");
                ball.Vel += impulseMagnitude * normal;
            }
            // ClampByBoundary(ball, 0.01f);
            ClampByTrapezoidBoundary(ball);
        }
    }
}