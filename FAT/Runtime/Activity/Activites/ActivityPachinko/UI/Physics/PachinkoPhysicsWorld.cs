/*
 * @Author: tang.yan
 * @Description: 弹珠游戏界面-物理世界部分 
 * @Date: 2024-12-03 14:12:51
 */

using System;
using System.Collections.Generic;
using Box2DSharp.Collision.Collider;
using Box2DSharp.Collision.Shapes;
using Box2DSharp.Common;
using Box2DSharp.Dynamics;
using Box2DSharp.Dynamics.Contacts;
using UnityEngine;
using EL;
using Cysharp.Text;

namespace FAT
{
    public class PachinkoPhysicsWorld : MonoBehaviour, IContactListener
    {
        //每个unity单位长度对应100像素，这里用做UI表现层和box2d物理世界之间的映射转换
        private FP BasePixelsPerUnit = 100;
        //对外可配置参数
        [SerializeField][Range(0f, 100f)][Tooltip("重力系数")] private float gravity;
        [SerializeField][Range(0f, 1f)][Tooltip("弹力系数")] private float bounciness;
        [SerializeField][Range(0f, 100f)][Tooltip("小球质量")] private float ballMass;
        //弹力球
        [SerializeField] private RectTransform Ball;
        //基础布局根节点
        [SerializeField] private RectTransform BasicLeft;
        [SerializeField] private RectTransform BasicRight;
        [SerializeField] private RectTransform BasicTop;
        [SerializeField] private RectTransform BasicDown;
        //自定义布局根节点
        [SerializeField] private RectTransform ObstacleLeft;
        [SerializeField] private RectTransform ObstacleRight;
        [SerializeField] private RectTransform ObstacleMiddle;
        [SerializeField] private RectTransform ObstacleBumper;
        [SerializeField] private RectTransform ObstaclePeg;

        //直接设置fixedDeltaTime 确保使用时不会有误差
        private FP _fixedDeltaTime = 0.02;
        //box2d物理世界相关实体
        private World _world;
        //各个实体数据类 里面包了box2d的body
        private PachinkoBallData _ballData;
        private List<PachinkoBumperData> _bumperDataList;
        private List<PachinkoPegData> _pegDataList;
        //小球的可配置参数 可以通过外部设置或者内部debug模式设置
        private int _startPosIndex = 0; //初始发射口的index序号
        private FP _posOffset = 0;  //基于初始发射口中心位置的左右偏移值(左负右正)
        private FP _startVelocity = 0;  //小球初速度 默认0为自然下落且此时角度偏移值无效
        private FP _angleOffset = -90; //基于初始发射口中心位置向下的角度偏移(顺时针向下 范围0到-180) -90为垂直向下
        //记录每次球发射时 整个流程中和所有Flippers、Bumper、Peg的碰撞次数，供策划评判这次发射是否好玩
        private int _totalColliderCount = 0;

        //物理实体类型
        private enum PachinkoPhysicsBodyType
        {
            None = 0,
            Ball = 1,
            BasicWall = 2,
            BasicTopPillar = 3,
            BasicDownPillar = 4,
            BasicDownEndPos = 5,
            FlippersLeft = 6,
            FlippersRight = 7,
            FlippersMiddle = 8,
            Bumper = 9,
            Peg = 10,
        }
        
        //对世界发送的指令类型 世界根据指令来决定自身的下一步操作
        private enum WorldCommandType
        {
            None = 0,       //无指令
            PlayNormal = 1, //正常发射一次小球
            PlayDebug = 2,  //从debug面板发射小球 小球会根据当前参数循环play
            AutoTest = 3,   //开启自动测试流程
            WeightTest = 4,   //开启自动测试权重配置正确性流程
        }
        private WorldCommandType _worldCommandType = WorldCommandType.None;
        
        //世界自身逻辑决断出的下一步操作类型 会改变世界所处的状态
        private enum WorldOperateType
        {
            None = 0,           //无操作
            ResetBallPos = 1,   //重置小球UI层位置
            ResetBall = 2,      //重置小球body
            ResetWorld = 3,     //重置整个世界world
        }
        private WorldOperateType _worldOperateType = WorldOperateType.None;
        
        //当前物理世界所处状态
        private enum WorldState
        {
            None = 0,   //未初始化
            Wait = 1,   //等待运行
            Ready = 2,  //准备运行
            Play = 3,   //正在运行
            Bingo = 4,  //小球落入奖励口时
            End = 5,    //结束
        }
        private WorldState _worldState = WorldState.None;

        private Action<int> _onPlayEndCallBack;
        private Action<int> _onBumperColliderCallBack;
        public void OnInit()
        {
            _InitRectTransform();
            _InitUIData();
            _InitEffect();
        }

        //外部注册小球播放结束(落入得分口)回调
        public void RegisterPlayEndCb(Action<int> callback)
        {
            _onPlayEndCallBack = callback;
        }
        
        //外部注册小球与保险杠发生碰撞时的回调
        public void RegisterBumperColliderCb(Action<int> callback)
        {
            _onBumperColliderCallBack = callback;
        }

        public void OnRefresh()
        {
            //每次开界面时刷新下矩阵
            _ballData?.RefreshCacheMatrix(transform.localToWorldMatrix);
            //每次开界面时都重新初始化world中所有内容 对应OnClear
            _InitWorldInfo();
            //刷新保险杠UI信息
            _RefreshBumperUI();
        }
        
        public void OnClear()
        {
            //销毁世界及其中的body
            _DestroyWorld();
            //重置各种状态
            _worldCommandType = WorldCommandType.None;
            _worldOperateType = WorldOperateType.None;
            _worldState = WorldState.End;
            //结束时重置小球位置
            _ballData.Reset();
            //结束时清理碰撞次数
            _ClearAllBumperColliderCount();
            _totalColliderCount = 0;
            //结束时清理当前自动测试配置信息
            _ClearAuToTestInfo();
            _ClearAuToWeightTestInfo();
        }

        //设置小球的初始发射信息
        public void SetBallStartInfo(int startPosIndex, FP posOffset, FP startVelocity, FP angleOffset)
        {
            _startPosIndex = startPosIndex;
            _posOffset = posOffset;
            _startVelocity = startVelocity;
            _angleOffset = angleOffset;
            _worldCommandType = WorldCommandType.PlayNormal;
            _worldOperateType = WorldOperateType.ResetBallPos;
            _worldState = WorldState.Ready;
        }
        
        //根据当前参数应用小球的初始发射信息
        private void _ApplyBallStartInfo()
        {
            if (!_ballStartPosList.TryGetByIndex(_startPosIndex, out var basePos)) return;
            var localPos = new FVector2(basePos.X + _posOffset / BasePixelsPerUnit, basePos.Y);
            _ballData.GetBody().SetTransform(localPos, FP.Zero);  //这里本质上是根据对应UI节点的局部位置(基于根节点transform通过InverseTransformPoint得出)来设置body的位置
            //给ball初始线速度 有初始速度时 角度偏移才会生效
            if (_startVelocity > 0)
            {
                _ballData.GetBody().SetLinearVelocity(_startVelocity * _GetDirectionVectorFromAngle(_angleOffset));
            }
        }

        #region 设置世界小球信息相关

        private void _InitWorldInfo()
        {
            //初始化世界
            _InitWorld();
            //初始化世界中的固定物理body
            _InitBasicBody();
            //初始化可被编辑的自定义物理body（由策划编辑布局信息）
            _InitCustomBody();
            //初始化弹力球body
            _InitBallBody();
            //初始化完毕后设置成等待状态
            _worldState = WorldState.Wait;
        }
        
        private void FixedUpdate()
        {
            //每帧检查当前操作类型并执行
            if (!_CheckWorldOperate()) return;
            //每帧根据当前状态决定是否处理后续逻辑
            if (!_CheckWorldState()) return;
            //基于Unity生命周期方法FixedUpdate来驱动物理世界world
            _world.Step(_fixedDeltaTime, 8, 3);
            //每帧刷新UI层球的位置信息
            _ballData?.FixedUpdateBallTransInfo(BasePixelsPerUnit);
        }

        private bool _CheckWorldOperate()
        {
            if (_worldOperateType == WorldOperateType.ResetWorld)
            {
                _ClearAllBumperColliderCount();//重置时清理碰撞次数
                _totalColliderCount = 0;
                _DestroyWorld();
                _InitWorldInfo();
                //默认ResetWorld后伴随着ResetBallPos
                _worldOperateType = WorldOperateType.ResetBallPos;
                return false;
            }
            if (_worldOperateType == WorldOperateType.ResetBallPos)
            {
                _ballData.Reset();
                //默认ResetBallPos后伴随着ResetBall
                _worldOperateType = WorldOperateType.ResetBall;
                return false;
            }
            if (_worldOperateType == WorldOperateType.ResetBall)
            {
                _ClearAllBumperColliderCount();//重置时清理碰撞次数
                _totalColliderCount = 0;
                _ballData.ClearBody(_world);
                _InitBallBody();
                //根据目前参数设置小球body初始位置
                _ApplyBallStartInfo();
                //默认ResetBall之后无其他操作
                _worldOperateType = WorldOperateType.None;
                //ResetBall之后进行状态切换
                if (_worldState == WorldState.Bingo)    //球落入得分口后 如果是normal模式切到Wait 其他情况切到Play即继续动播放
                {
                    if (_worldCommandType == WorldCommandType.PlayNormal)
                    {
                        _worldState = WorldState.Wait;
                    }
                    else
                    {
                        _worldState = WorldState.Play;
                    }
                }
                else if (_worldState == WorldState.Ready)   //UI层点击按钮发射时处于Ready状态
                {
                    _worldState = WorldState.Play;
                }
                else
                {
                    _worldState = WorldState.Wait;
                }
                return false;
            }
            return _worldOperateType == WorldOperateType.None;
        }

        //检查目前世界的状态
        private bool _CheckWorldState()
        {
            return _worldState == WorldState.Play;
        }
        
        private FVector2 _GetDirectionVectorFromAngle(FP angleInDegrees)
        {
            // 将角度从度数转换为弧度
            var angleInRadians = angleInDegrees * FP.Deg2Rad;
            // 计算 X 和 Y 分量
            var xComponent = FP.Cos(angleInRadians);
            var yComponent = FP.Sin(angleInRadians);
            return new FVector2(xComponent, yComponent);
        }

        private void _DestroyWorld()
        {
            if (_world == null) return;
            //置空当前所有的body引用
            _ballData.ClearBody();
            foreach (var data in _bumperDataList) data.ClearBody();
            foreach (var data in _pegDataList) data.ClearBody();
            //销毁所有的body
            var bodyNode = _world.BodyList.First;
            while (bodyNode != null)
            {
                var currentBody = bodyNode.Value;
                bodyNode = bodyNode.Next; // 获取下一个节点引用
                _world.DestroyBody(currentBody); // 销毁当前物体，DestroyBody 方法内部会自动销毁所有关节、接触点和夹具
            }
            //解除接触监听器
            _world.SetContactListener(null);
            //置空world
            _world = null;
        }

        //保证存储的保险杠数量不变的情况下重置其次数为0
        private void _ClearAllBumperColliderCount()
        {
            foreach (var bumperData in _bumperDataList)
            {
                bumperData.ResetColliderCount();
            }
        }

        #endregion

        #region 碰撞检测相关

        public void BeginContact(Contact contact)
        {
            var aBody = contact.FixtureA.Body;
            var bBody = contact.FixtureB.Body;
            var aUserData = (BodyUserData)aBody.UserData;
            var bUserData = (BodyUserData)bBody.UserData;
            if (bUserData.Type == PachinkoPhysicsBodyType.Ball)
            {
                var aType = aUserData.Type;
                if (aType == PachinkoPhysicsBodyType.Bumper)
                {
                    //保险杠碰撞时 记录对应的碰撞次数
                    var aIndex = aUserData.Index;
                    _bumperDataList[aIndex].ExecuteColliderBegin(_CheckIsDebug());
                    _totalColliderCount++;
                    _onBumperColliderCallBack?.Invoke(aIndex);
                    _PlayVibrateMedium();   //小球发生碰撞时震动
                }
                if (aType == PachinkoPhysicsBodyType.Peg)
                {
                    var aIndex = aUserData.Index;
                    _pegDataList[aIndex].ExecuteColliderBegin(_CheckIsDebug());
                    _totalColliderCount++;
                    _PlayVibrateMedium();   //小球发生碰撞时震动
                }
                //只考虑策划可编辑的碰撞物体
                if (aType == PachinkoPhysicsBodyType.FlippersLeft || aType == PachinkoPhysicsBodyType.FlippersMiddle ||
                    aType == PachinkoPhysicsBodyType.FlippersRight)
                {
                    _totalColliderCount++;
                    _PlayBallHitStock();    //播碰撞音效
                    _PlayVibrateMedium();   //小球发生碰撞时震动
                }
            }
        }

        public void EndContact(Contact contact)
        {
            var aBody = contact.FixtureA.Body;
            var bBody = contact.FixtureB.Body;
            var aUserData = (BodyUserData)aBody.UserData;
            var bUserData = (BodyUserData)bBody.UserData;
            if (aUserData.Type == PachinkoPhysicsBodyType.BasicDownEndPos && bUserData.Type == PachinkoPhysicsBodyType.Ball)
            {
                _worldState = WorldState.Bingo;
                var dropIndex = aUserData.Index;    //得分口序号
                if (_worldCommandType == WorldCommandType.PlayNormal)
                {
                    _worldOperateType = WorldOperateType.ResetBallPos;
                    //小球落入得分口时回调
                    _onPlayEndCallBack?.Invoke(dropIndex);
                    //小球落入得分口时震动
                    _PlayVibrateMedium();
                }
                else if (_worldCommandType == WorldCommandType.PlayDebug)
                {
                    var bumperLog = "";
                    var totalBumper = _bumperDataList.Count;
                    for (var i = 0; i < totalBumper; i++)
                    {
                        bumperLog += $"碰撞{i}号保险杠{_bumperDataList[i].GetColliderCount()}次,";
                    }
                    _AutoTestDebug($"球从({_startPosIndex})号口发射,落入({dropIndex})号口,{bumperLog}总共碰撞{_totalColliderCount}次," + 
                                   $"gravity={gravity},bounciness={bounciness},ballMass={ballMass}," +
                                   $"_startPosIndex={_startPosIndex},_posOffset={_posOffset},_startVelocity={_startVelocity},_angleOffset={_angleOffset}");
                    _worldOperateType = WorldOperateType.ResetBallPos;
                }
                else if (_worldCommandType == WorldCommandType.AutoTest)
                {
                    var colliderLog = "";
                    var totalBumper = _bumperDataList.Count;
                    for (var i = 0; i < totalBumper; i++)
                    {
                        colliderLog = ZString.Concat(colliderLog, _bumperDataList[i].GetColliderCount(), ",");
                    }
                    colliderLog = ZString.Concat(colliderLog, _totalColliderCount, ",");
                    //以配置的形式记录每次碰撞信息
                    _stringBuilder.Append(ZString.Concat($"{_startPosIndex},{dropIndex},", colliderLog, $"{_posOffset},{_startVelocity},{_angleOffset}", "\n"));
                    _ClearAllBumperColliderCount();
                    _totalColliderCount = 0;
                    _MoveNextAutoTestConfig();
                }
                else if (_worldCommandType == WorldCommandType.WeightTest)
                {
                    var colliderLog = "";
                    var totalBumper = _bumperDataList.Count;
                    for (var i = 0; i < totalBumper; i++)
                    {
                        colliderLog = ZString.Concat(colliderLog, _bumperDataList[i].GetColliderCount(), ",");
                    }
                    colliderLog = ZString.Concat(colliderLog, _totalColliderCount, ",");
                    //以配置的形式记录每次碰撞信息
                    var resultStr = ZString.Concat($"{_startPosIndex},{dropIndex},", colliderLog, $"{_posOffset},{_startVelocity},{_angleOffset}");
                    //记录每个发射参数出现的次数 同时保证每个发射参数只会被打印一次
                    if (!_curTestHitCount.ContainsKey(resultStr))
                    {
                        _curTestHitCount.Add(resultStr, 1);
                        _stringBuilder.Append(ZString.Concat(resultStr, "\n"));
                    }
                    else
                    {
                        _curTestHitCount[resultStr]++;
                    }
                    //记录落到不同区间的次数
                    if (!_curTestDropInfo.ContainsKey(dropIndex)) _curTestDropInfo.Add(dropIndex, 1);
                    else _curTestDropInfo[dropIndex]++;
                    _ClearAllBumperColliderCount();
                    _totalColliderCount = 0;
                    _MoveNextAutoTestWeightConfig();
                }
            }
        }

        public void PreSolve(Contact contact, in Manifold oldManifold) { }

        public void PostSolve(Contact contact, in ContactImpulse impulse) { }

        #endregion

        #region 初始化相关

        private RectTransform _basicLeftWall;
        private RectTransform _basicRightWall;
        private List<RectTransform> _basicTopPillar = new List<RectTransform>();
        private List<RectTransform> _basicDownPillar = new List<RectTransform>();
        private List<RectTransform> _basicDownPillarCircle = new List<RectTransform>();
        private List<RectTransform> _basicDownEndPos = new List<RectTransform>();
        private List<RectTransform> _obstacleLeftChildren = new List<RectTransform>();
        private List<RectTransform> _obstacleRightChildren = new List<RectTransform>();
        private List<RectTransform> _obstacleMiddleChildren = new List<RectTransform>();
        //球发射的起始位置可选列表 可以在此位置基础上进行左右偏移
        private List<FVector2> _ballStartPosList = new List<FVector2>();

        //给world传入初始值时，使用四舍五入保留三位小数的形式过一遍 避免初始误差
        private static float _RoundToDecimals(float num, float multiplier = 1000f)
        {
            return Mathf.Round(num * multiplier) / multiplier;
        }
        
        private static Vector3 _RoundToDecimals(Vector3 vector, float multiplier = 1000f)
        {
            return new Vector3(
                _RoundToDecimals(vector.x, multiplier), 
                _RoundToDecimals(vector.y, multiplier), 
                _RoundToDecimals(vector.z, multiplier));
        }
        
        private static Vector2 _RoundToDecimals(Vector2 vector, float multiplier = 1000f)
        {
            return new Vector2(
                _RoundToDecimals(vector.x, multiplier), 
                _RoundToDecimals(vector.y, multiplier));
        }
        
        //第一次创建时初始化所有用到的rectTransform
        private void _InitRectTransform()
        {
            _basicLeftWall = BasicLeft.FindEx<RectTransform>("Wall");
            _basicRightWall = BasicRight.FindEx<RectTransform>("Wall");
            //顶部柱子6个 外加1个封顶
            var pillarPath = "Pillar/Pillar";
            for (var i = 0; i < 7; i++)
            {
                var path = ZString.Concat(pillarPath, i);
                _basicTopPillar.Add(BasicTop.FindEx<RectTransform>(path));
            }
            //底部柱子8个 分为顶部的圆形和底部的长方形
            for (var i = 0; i < 8; i++)
            {
                var path = ZString.Concat(pillarPath, i);
                var pathCircle = ZString.Concat(pillarPath, "Circle", i);
                _basicDownPillar.Add(BasicDown.FindEx<RectTransform>(path));
                _basicDownPillarCircle.Add(BasicDown.FindEx<RectTransform>(pathCircle));
            }
            //底部终点位置Trigger点 7个
            var endPosPath = "EndPos/Pos";
            for (var i = 0; i < 7; i++)
            {
                var path = ZString.Concat(endPosPath, i);
                _basicDownEndPos.Add(BasicDown.FindEx<RectTransform>(path));
            }
            //球发射的起始位置 5个
            var startPosPath = "StartPos/Pos";
            for (var i = 0; i < 5; i++)
            {
                var path = ZString.Concat(startPosPath, i);
                var rectTrans = BasicTop.FindEx<RectTransform>(path);
                var pos = transform.InverseTransformPoint(rectTrans.position);
                var localPos = _RoundToDecimals(pos).ToFVector2() / BasePixelsPerUnit;
                _ballStartPosList.Add(localPos);
            }
            //左侧Flippers
            for (var i = 0; i < ObstacleLeft.childCount; i++)
            {
                _obstacleLeftChildren.Add(ObstacleLeft.GetChild(i).FindEx<RectTransform>("Icon"));
            }
            //右侧Flippers
            for (var i = 0; i < ObstacleRight.childCount; i++)
            {
                _obstacleRightChildren.Add(ObstacleRight.GetChild(i).FindEx<RectTransform>("Icon"));
            }
            //中间Flippers
            for (var i = 0; i < ObstacleMiddle.childCount; i++)
            {
                _obstacleMiddleChildren.Add(ObstacleMiddle.GetChild(i).FindEx<RectTransform>("Icon"));
            }
        }

        //第一次创建时初始化所有用到的UI层数据类
        private void _InitUIData()
        {
            _bumperDataList = new List<PachinkoBumperData>();
            _pegDataList = new List<PachinkoPegData>();
            //Bumper
            for (var i = 0; i < ObstacleBumper.childCount; i++)
            {
                var data = new PachinkoBumperData();
                var root = ObstacleBumper.GetChild(i).GetComponent<RectTransform>();
                data.BindRoot(root, i);
                _bumperDataList.Add(data);
            }
            //Peg
            for (var i = 0; i < ObstaclePeg.childCount; i++)
            {
                var data = new PachinkoPegData();
                var root = ObstaclePeg.GetChild(i).GetComponent<RectTransform>();
                data.BindRoot(root, i);
                _pegDataList.Add(data);
            }
            //Ball
            _ballData = new PachinkoBallData();
            _ballData.BindRoot(Ball, 0);
        }

        private void _InitWorld()
        {
            //创建box2d world实体
            _world = new World(new FVector2(0, -gravity));
            _world.SetContactListener(this);
        }

        private void _InitBasicBody()
        {
            //左右侧墙壁
            _CreateStaticBody(_basicLeftWall, 1, 1, ShapeType.Polygon, new BodyUserData(){ Type = PachinkoPhysicsBodyType.BasicWall, Index = 0});
            _CreateStaticBody(_basicRightWall, 1, 1, ShapeType.Polygon, new BodyUserData(){ Type = PachinkoPhysicsBodyType.BasicWall, Index = 1});
            //顶部柱子6个 外加1个封顶
            for (var i = 0; i < _basicTopPillar.Count; i++)
            {
                _CreateStaticBody(_basicTopPillar[i], 1, 1, ShapeType.Polygon, new BodyUserData(){ Type = PachinkoPhysicsBodyType.BasicTopPillar, Index = i});
            }
            //底部柱子8个
            for (var i = 0; i < _basicDownPillar.Count; i++)
            {
                _CreateStaticBody(_basicDownPillar[i], 1, 1, ShapeType.Polygon, new BodyUserData(){ Type = PachinkoPhysicsBodyType.BasicDownPillar, Index = i});
                _CreateStaticBody(_basicDownPillarCircle[i], 1, 1, ShapeType.Circle, new BodyUserData(){ Type = PachinkoPhysicsBodyType.BasicDownPillar, Index = i});
            }
            //底部终点位置Trigger点 7个
            for (var i = 0; i < _basicDownEndPos.Count; i++)
            {
                _CreateStaticBody(_basicDownEndPos[i], 1, 1, ShapeType.Polygon, new BodyUserData(){ Type = PachinkoPhysicsBodyType.BasicDownEndPos, Index = i});
            }
        }

        private void _InitCustomBody()
        {
            //左侧Flippers
            for (var i = 0; i < _obstacleLeftChildren.Count; i++)
            {
                var childRect = _obstacleLeftChildren[i];
                var scale = childRect.parent.localScale;
                _CreateStaticBody(childRect, scale.x, scale.y, ShapeType.Polygon, new BodyUserData(){ Type = PachinkoPhysicsBodyType.FlippersLeft, Index = i});
            }
            //右侧Flippers
            for (var i = 0; i < _obstacleRightChildren.Count; i++)
            {
                var childRect = _obstacleRightChildren[i];
                var scale = childRect.parent.localScale;
                _CreateStaticBody(childRect, scale.x, scale.y, ShapeType.Polygon, new BodyUserData(){ Type = PachinkoPhysicsBodyType.FlippersRight, Index = i});
            }
            //中间Flippers
            for (var i = 0; i < _obstacleMiddleChildren.Count; i++)
            {
                var childRect = _obstacleMiddleChildren[i];
                var scale = childRect.parent.localScale;
                _CreateStaticBody(childRect, scale.x, scale.y, ShapeType.Polygon, new BodyUserData(){ Type = PachinkoPhysicsBodyType.FlippersMiddle, Index = i});
            }
            //Bumper
            for (var i = 0; i < _bumperDataList.Count; i++)
            {
                var data = _bumperDataList[i];
                var scale = data.GetRoot().localScale;
                var body = _CreateStaticBody(data.GetIconTrans(), scale.x, scale.y, ShapeType.Circle, new BodyUserData(){ Type = PachinkoPhysicsBodyType.Bumper, Index = i});
                data.BindBody(body);
                data.ResetColliderCount();  //每次重新绑定body时都重置一下保险杠的碰撞次数
            }
            //Peg
            for (var i = 0; i < _pegDataList.Count; i++)
            {
                var data = _pegDataList[i];
                var scale = data.GetRoot().localScale;
                var body = _CreateStaticBody(data.GetIconTrans(), scale.x, scale.y, ShapeType.Circle, new BodyUserData(){ Type = PachinkoPhysicsBodyType.Peg, Index = i});
                data.BindBody(body);
            }
        }

        private void _InitBallBody()
        {
            var scale = _ballData.GetRoot().localScale;
            var body = _CreateDynamicBody(_ballData.GetIconTrans(), scale.x, scale.y, ShapeType.Circle, new BodyUserData() { Type = PachinkoPhysicsBodyType.Ball, Index = 0 });
            body.GetMassData(out var massData);
            massData.Mass = ballMass;
            body.SetMassData(massData);
            _ballData.BindBody(body);
        }
        
        #region 基础创建body方法 默认锚点都在中心

        //创建body时传入的自定义数据 主要用于区分body类型和顺序
        private class BodyUserData
        {
            public PachinkoPhysicsBodyType Type = PachinkoPhysicsBodyType.None;
            public int Index = -1;
        }
        
        //创建静态body   scaleX scaleY为UI根节点设置的x y缩放值
        private Body _CreateStaticBody(RectTransform rectTrans, float scaleX, float scaleY, ShapeType shapeType, BodyUserData userData)
        {
            if (_world == null || rectTrans == null) 
                return null;
            var body = _world.CreateBody(new BodyDef() { BodyType = BodyType.StaticBody, UserData = userData});
            //静态物体密度为0 摩擦系数0.4 没有弹力系数
            var fixtureDef = new FixtureDef { Density = 0, Friction = 0.4, Restitution = 0 };
            _InitializeBody(rectTrans, scaleX, scaleY, shapeType, body, fixtureDef);
            return body;
        }
        
        //创建动态body   scaleX scaleY为UI根节点设置的x y缩放值
        private Body _CreateDynamicBody(RectTransform rectTrans, float scaleX, float scaleY, ShapeType shapeType, BodyUserData userData)
        {
            if (_world == null || rectTrans == null) 
                return null;
            var body = _world.CreateBody(new BodyDef() { BodyType = BodyType.DynamicBody, UserData = userData});
            //动态物体密度默认为1 摩擦系数Friction默认为0.4和弹力系数Restitution默认0.8 可对外暴露参数
            var fixtureDef = new FixtureDef { Density = 1.0f, Friction = 0.4, Restitution = bounciness};
            _InitializeBody(rectTrans, scaleX, scaleY, shapeType, body, fixtureDef);
            return body;
        }

        //初始化body相关参数 scaleX scaleY为UI根节点设置的x y缩放值
        private void _InitializeBody(RectTransform rectTrans, FP scaleX, FP scaleY, ShapeType shapeType, Body body, FixtureDef fixtureDef)
        {
            var offset = FVector2.Zero;
            //根据类型设置形状 默认按多边形处理
            if (shapeType == ShapeType.Circle)
            {
                var circleCollider = rectTrans.GetComponent<CircleCollider2D>();
                //圆形默认只能等比缩放
                var shape = new CircleShape() { Radius = _RoundToDecimals(circleCollider.radius) * scaleX / BasePixelsPerUnit };
                fixtureDef.Shape = shape;
                fixtureDef.IsSensor = circleCollider.isTrigger;
                //球形shape默认不应该有offset
            }
            else
            {
                //默认使用
                if (rectTrans.TryGetComponent<BoxCollider2D>(out var boxCollider))
                {
                    var shape = new PolygonShape();
                    var size = _RoundToDecimals(boxCollider.size);
                    shape.SetAsBox(size.x * scaleX / BasePixelsPerUnit / 2, size.y * scaleY / BasePixelsPerUnit / 2);
                    fixtureDef.Shape = shape;
                    fixtureDef.IsSensor = boxCollider.isTrigger;
                    var colliderOffset = _RoundToDecimals(boxCollider.offset);
                    offset.Set(colliderOffset.x * scaleX, colliderOffset.y * scaleY);
                }
                else if (rectTrans.TryGetComponent<PolygonCollider2D>(out var polygonCollider))
                {
                    // 默认只处理第一个路径下的点
                    var points = polygonCollider.GetPath(0);
                    var shape = new PolygonShape();
                    var numPoints = points.Length;
                    var vertices = new FVector2[numPoints];
                    for (var i = 0; i < numPoints; i++)
                    {
                        var point = _RoundToDecimals(points[i]);
                        vertices[i] = new FVector2(point.x * scaleX / BasePixelsPerUnit, point.y * scaleY / BasePixelsPerUnit);
                    }
                    shape.Set(vertices, numPoints);
                    fixtureDef.Shape = shape;
                    fixtureDef.IsSensor = polygonCollider.isTrigger;
                    var colliderOffset = _RoundToDecimals(polygonCollider.offset);
                    offset.Set(colliderOffset.x * scaleX, colliderOffset.y * scaleY);
                }
            }
            //设置fixture
            body.CreateFixture(fixtureDef);
            //设置位置和角度
            //根据root以及物体的世界坐标，得出物体相对于root的局部坐标，将此坐标作为box2d的运算坐标传给对应body
            var pos = transform.InverseTransformPoint(rectTrans.position);
            var localPos = (_RoundToDecimals(pos).ToFVector2() + offset) / BasePixelsPerUnit;
            var angle = _RoundToDecimals(rectTrans.eulerAngles.z) * FP.Deg2Rad;
            body.SetTransform(localPos, angle);
        }

        #endregion

        #endregion

        #region 界面刷新相关

        private void _RefreshBumperUI()
        {
            foreach (var bumperData in _bumperDataList)
            {
                bumperData.RefreshBumperUI();
            }
        }

        #endregion
        
        #region 动特效相关

        private Animator _basicAnimator;    //Basic节点的总控动画机 用于播放获得中间大奖时的效果
        private List<Animator> _startPosAnimList = new List<Animator>();
        private List<Animator> _endPosAnimList = new List<Animator>();

        private void _InitEffect()
        {
            _basicAnimator = transform.FindEx<Animator>("Basic");
            //球发射的起始位置特效 5个
            var startPosPath = "StartPos/Pos";
            for (var i = 0; i < 5; i++)
            {
                var path = ZString.Concat(startPosPath, i, "/UI_pachinko_light_up");
                _startPosAnimList.Add(BasicTop.FindEx<Animator>(path));
            }
            //球发射的起始位置特效 7个
            var endPosPath = "EndPos/Pos";
            for (var i = 0; i < 7; i++)
            {
                var path = ZString.Concat(endPosPath, i, "/UI_pachinko_light_down");
                _endPosAnimList.Add(BasicDown.FindEx<Animator>(path));
            }
        }

        //为了避免总控动画机对各个节点动画机节点的影响 故节点动画机在播放时可以自行判断总控动画机是否生效 
        private void _SetBasicAnimActive(bool isActive)
        {
            _basicAnimator.enabled = isActive;
        }

        private void _SetStartPosAnimActive(bool isActive)
        {
            foreach (var animator in _startPosAnimList)
            {
                animator.enabled = isActive;
            }
        }
        
        private void _SetEndPosAnimActive(bool isActive)
        {
            foreach (var animator in _endPosAnimList)
            {
                animator.enabled = isActive;
            }
        }
        
        //小球发射前播放发射口特效  延迟0.3秒后小球发射
        public void PlayStartPosEffect(int index)
        {
            //debug模式下不播放动效
            if (_CheckIsDebug()) return;
            if (_startPosAnimList.TryGetByIndex(index, out var animator))
            {
                animator.enabled = true;
                _SetBasicAnimActive(false);
                animator.ResetTrigger("Punch");
                animator.SetTrigger("Punch");
            }
        }
        
        //小球落入口时播放特效
        public void PlayEndPosEffect(int index)
        {
            //debug模式下不播放动效
            if (_CheckIsDebug()) return;
            if (_endPosAnimList.TryGetByIndex(index, out var animator))
            {
                animator.enabled = true;
                _SetBasicAnimActive(false);
                animator.ResetTrigger("Punch");
                animator.SetTrigger("Punch");
            }
        }

        //播放获得大奖时的动画
        public void PlayBigRewardAnim()
        {
            //debug模式下不播放动效
            if (_CheckIsDebug()) return;
            _PlayBasicBigRewardAnim();
            _PlayBumperBigRewardAnim();
        }

        private bool _CheckIsDebug()
        {
            return _worldCommandType != WorldCommandType.PlayNormal;
        }
        
        private void _PlayBasicBigRewardAnim()
        {
            _SetStartPosAnimActive(false);
            _SetEndPosAnimActive(false);
            _SetBasicAnimActive(true);
            _basicAnimator.ResetTrigger("Punch");
            _basicAnimator.SetTrigger("Punch");
        }

        private void _PlayBumperBigRewardAnim()
        {
            foreach (var bumperData in _bumperDataList)
            {
                bumperData.PlayBigRewardEffect();
            }
        }

        //监听动画机上的动画播放完时的事件
        public void OnAnimPlayEnd(AnimatorStateInfo stateInfo)
        {
            foreach (var bumperData in _bumperDataList)
            {
                bumperData.OnAnimPlayEnd(stateInfo);
            }
        }

        private void _PlayVibrateMedium()
        {
            //debug模式下不震动
            if (_CheckIsDebug()) return;
            VibrationManager.VibrateMedium();
        }

        private void _PlayBallHitStock()
        {
            //debug模式下不播音效
            if (_CheckIsDebug()) return;
            Game.Manager.audioMan.TriggerSound("PachinkoHitStock");
        }

        #endregion
        
        #region Debug相关
        
        private int _curTestConfigIndex = -1;    //记录目前正在执行哪个Index对应的配置（执行次数）
        private int _curTestStartPosIndex = -1;  //记录目前正在测试哪个发射口 
        private Utf16ValueStringBuilder _stringBuilder = ZString.CreateStringBuilder();

        public void DebugPachinkoInfo(int paramType, string param)
        {
            if (paramType == 1)
            {
                if (float.TryParse(param, out var num)) { gravity = num; }
                _worldCommandType = WorldCommandType.PlayDebug;
                _worldOperateType = WorldOperateType.ResetWorld;
            }
            else if (paramType == 2)
            {
                if (float.TryParse(param, out var num)) { bounciness = num; }
                _worldCommandType = WorldCommandType.PlayDebug;
                _worldOperateType = WorldOperateType.ResetBallPos;
            }
            else if (paramType == 3)
            {
                if (float.TryParse(param, out var num)) { ballMass = num; }
                _worldCommandType = WorldCommandType.PlayDebug;
                _worldOperateType = WorldOperateType.ResetBallPos;
            }
            else if (paramType == 4)
            {
                var r = param.Split(",");
                var startPosIndex = r.GetElementEx(0, ArrayExt.OverflowBehaviour.Default);
                var posOffset = r.GetElementEx(1, ArrayExt.OverflowBehaviour.Default);
                var startVelocity = r.GetElementEx(2, ArrayExt.OverflowBehaviour.Default);
                var angleOffset = r.GetElementEx(3, ArrayExt.OverflowBehaviour.Default);
                if (!string.IsNullOrEmpty(startPosIndex) && int.TryParse(startPosIndex, out var num1)) { _startPosIndex = num1; }
                if (!string.IsNullOrEmpty(posOffset) && float.TryParse(posOffset, out var num2)) { _posOffset = num2; }
                if (!string.IsNullOrEmpty(startVelocity) && float.TryParse(startVelocity, out var num3)) { _startVelocity = num3; }
                if (!string.IsNullOrEmpty(angleOffset) && float.TryParse(angleOffset, out var num4)) { _angleOffset = num4; }
                _worldCommandType = WorldCommandType.PlayDebug;
                _worldOperateType = WorldOperateType.ResetBallPos;
            }
            else if (paramType == 5)
            {
                _worldState = WorldState.Play;
            }
            else if (paramType == 6)
            {
                _worldState = WorldState.Wait;
            }
            else if (paramType == 7)
            {
                _worldState = WorldState.Wait;
                _DebugRunAutoTest(param);
            }
            else if (paramType == 8)
            {
                _worldState = WorldState.Wait;
                if (int.TryParse(param, out var num))
                {
                    _DebugRunWeightTest(num);
                }
            }
        }

        private List<string> _autoTestConfigList = new List<string>();  //记录目前正在执行的自动测试配置信息
        private void _DebugRunAutoTest(string configStr)
        {
            var confStrList = configStr.Split("|");
            if (confStrList.Length < 1) return;
            _ClearAuToTestInfo();
            _autoTestConfigList = confStrList.ToList();
            _AutoTestDebug("自动测试任务开始");
            _MoveNextAutoTestStartPos();
            _worldCommandType = WorldCommandType.AutoTest;
            _worldState = WorldState.Ready;
        }
        
        //移动到下一个自动测试小球发射口Index
        private void _MoveNextAutoTestStartPos()
        {
            _curTestStartPosIndex++;
            if (_curTestStartPosIndex < _ballStartPosList.Count)
            {
                _startPosIndex = _curTestStartPosIndex;
                _AutoTestDebug($"{_startPosIndex}号发射口开始测试，请耐心等待");
                _MoveNextAutoTestConfig();
            }
            else
            {
                //所有发射口的测试均已结束
                _startPosIndex = 0;
                _curTestStartPosIndex = -1;
                _AutoTestDebug("自动测试任务结束");
                _ClearAuToTestInfo();
                _worldCommandType = WorldCommandType.None;
                _worldState = WorldState.Wait;
            }
        }
        
        //移动到下一个自动测试配置信息
        private void _MoveNextAutoTestConfig()
        {
            _curTestConfigIndex++;
            if (_autoTestConfigList.TryGetByIndex(_curTestConfigIndex, out var conf))
            {
                var r = conf.Split(",");
                var posOffset = r.GetElementEx(0, ArrayExt.OverflowBehaviour.Default);
                var startVelocity = r.GetElementEx(1, ArrayExt.OverflowBehaviour.Default);
                var angleOffset = r.GetElementEx(2, ArrayExt.OverflowBehaviour.Default);
                if (!string.IsNullOrEmpty(posOffset) && float.TryParse(posOffset, out var num2)) { _posOffset = num2; }
                if (!string.IsNullOrEmpty(startVelocity) && float.TryParse(startVelocity, out var num3)) { _startVelocity = num3; }
                if (!string.IsNullOrEmpty(angleOffset) && float.TryParse(angleOffset, out var num4)) { _angleOffset = num4; }
                //打印当前测试数据 方便出问题时debug
                // _AutoTestDebug(ZString.Concat("Index = ", _curTestConfigIndex, " conf = ", conf));
                _worldOperateType = WorldOperateType.ResetBallPos;
            }
            else
            {
                _curTestConfigIndex = -1;
                var result = ZString.Concat($"{_startPosIndex}号发射口测试结束，测试结果如下：\n", _stringBuilder);
                _AutoTestDebug(result);
                _stringBuilder.Clear();
                _MoveNextAutoTestStartPos();
            }
        }

        private void _ClearAuToTestInfo()
        {
            _autoTestConfigList.Clear();
            _curTestConfigIndex = -1;
            _curTestStartPosIndex = -1;
            _stringBuilder.Clear();
        }
        
        private int _curTestTotalCount = -1;     //记录目前每个发射口要测试的次数
        private Dictionary<string, int> _curTestHitCount = new Dictionary<string, int>();   //当前发射口各个测试数据经权重随机后的命中次数
        private Dictionary<int, int> _curTestDropInfo = new Dictionary<int, int>(); //每个口在跑数据过程中，记录分别落到不同区间的次数

        //times: 每个口要执行的次数（每次都基于策划配置的权重进行）
        private void _DebugRunWeightTest(int times)
        {
            if (times <= 0) return;
            _ClearAuToWeightTestInfo();
            _curTestTotalCount = times;
            for (var i = 0; i < _basicDownEndPos.Count; i++)
            {
                _curTestDropInfo.Add(i, 0);
            }
            _AutoTestDebug("自动测试权重正确性任务开始，测试次数 = " + times);
            _MoveNextAutoTestWeightStartPos();
            _worldCommandType = WorldCommandType.WeightTest;
            _worldState = WorldState.Ready;
        }
        
        //移动到下一个自动测试小球发射口Index
        private void _MoveNextAutoTestWeightStartPos()
        {
            _curTestStartPosIndex++;
            if (_curTestStartPosIndex < _ballStartPosList.Count)
            {
                _startPosIndex = _curTestStartPosIndex;
                _AutoTestDebug($"{_startPosIndex}号发射口开始测试，请耐心等待");
                _MoveNextAutoTestWeightConfig();
            }
            else
            {
                //所有发射口的测试均已结束
                _startPosIndex = 0;
                _curTestStartPosIndex = -1;
                _AutoTestDebug("自动测试权重正确性任务结束");
                _ClearAuToWeightTestInfo();
                _worldCommandType = WorldCommandType.None;
                _worldState = WorldState.Wait;
            }
        }
        
        //移动到下一个自动测试配置信息
        private void _MoveNextAutoTestWeightConfig()
        {
            _curTestConfigIndex++;
            var isFinish = _curTestConfigIndex >= _curTestTotalCount;
            if (!isFinish)
            {
                var dropInfo = Game.Manager.pachinkoMan.RandomDrop(_startPosIndex);
                if (dropInfo != null)
                {
                    _posOffset = dropInfo.Offset;
                    _startVelocity = dropInfo.Velocity;
                    _angleOffset = dropInfo.Angle;
                    //打印当前测试数据 方便出问题时debug
                    // _AutoTestDebug(ZString.Concat("Count = ", _curTestConfigIndex, " dropInfo = ", dropInfo));
                    _worldOperateType = WorldOperateType.ResetBallPos;
                }
            }
            else
            {
                _curTestConfigIndex = -1;
                //各个区间掉落次数
                var dropInfo = "";
                var total = _curTestDropInfo.Count;
                for (var i = 0; i < total; i++)
                {
                    dropInfo = ZString.Concat(dropInfo, i, ":", _curTestDropInfo[i], ",");
                }
                _AutoTestDebug($"{_startPosIndex}号发射口对应各个区间分布情况 = {dropInfo}");
                //最终结果日志 追加记录一下每个测试数据出现的次数
                foreach (var info in _curTestHitCount)
                {
                    _stringBuilder.Replace(info.Key, ZString.Concat(info.Key, ",", info.Value));
                }
                var result = ZString.Concat($"{_startPosIndex}号发射口权重正确性测试结束，测试结果如下：\n", _stringBuilder);
                _AutoTestDebug(result);
                _stringBuilder.Clear();
                _curTestHitCount.Clear();
                _curTestDropInfo.Clear();
                for (var i = 0; i < _basicDownEndPos.Count; i++)
                {
                    _curTestDropInfo.Add(i, 0);
                }
                _MoveNextAutoTestWeightStartPos();
            }
        }

        private void _ClearAuToWeightTestInfo()
        {
            _curTestConfigIndex = -1;
            _curTestStartPosIndex = -1;
            _curTestTotalCount = -1;
            _stringBuilder.Clear();
            _curTestHitCount.Clear();
            _curTestDropInfo.Clear();
        }
        
        private static void _AutoTestDebug(string log)
        {
#if UNITY_EDITOR
            Debug.LogError(log);
#else
            Debug.Log(log);
#endif
        }

        #endregion
    }
}