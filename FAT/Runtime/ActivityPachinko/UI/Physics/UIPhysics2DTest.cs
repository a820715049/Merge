using Box2DSharp.Collision.Collider;
using Box2DSharp.Collision.Shapes;
using Box2DSharp.Common;
using Box2DSharp.Dynamics;
using Box2DSharp.Dynamics.Contacts;
using UnityEngine;
using EL;

namespace FAT
{
    public class UIPhysics2DTest : UIBase, IContactListener
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform ball;
        [SerializeField] private CircleCollider2D ballCollider2D;
        [SerializeField] private RectTransform panelB;
        [SerializeField] private BoxCollider2D panelBCollider2D;
        [SerializeField] private RectTransform panelL;
        [SerializeField] private BoxCollider2D panelLCollider2D;
        [SerializeField] private RectTransform panelR;
        [SerializeField] private BoxCollider2D panelRCollider2D;
        private Vector3 _originBallPos;
        private Vector3 _cacheBallPos = new Vector3();
        private Vector3 _cacheBallAngle = new Vector3();
        private Matrix4x4 _cacheRootMatrix; //缓存根节点的转换矩阵

        private World _world;
        private Body _ballBody;
        private Body _panelBBody;
        private Body _panelLBody;
        private Body _panelRBody;
        
        private int _basePixelsPerUnit = 100;
        private FP _fixedDeltaTime = -1;
        
        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            transform.AddButton("Content/BtnClose/Btn", base.Close);
            _originBallPos = ball.localPosition;
            //创建世界
            _world = new World(new FVector2(0, -10));
            _world.SetContactListener(this);
            //从UI位置和宽高出发 转换成body的位置和宽高
            //设置位置
            _ballBody = _world.CreateBody(new BodyDef() { BodyType = BodyType.DynamicBody, UserData = "Ball" });
            //根据root位置以及小球的世界坐标，得出小球相对于root的局部坐标
            var localPos = root.InverseTransformPoint(ball.position).ToFVector2() / _basePixelsPerUnit;
            _ballBody.SetTransform(localPos, _ballBody.GetAngle());
            //设置大小
            var shape = new CircleShape() {Radius = ballCollider2D.radius / _basePixelsPerUnit};
            _ballBody.CreateFixture(new FixtureDef {Shape = shape, Density = 1.0f, Restitution = 0.5f, Friction = 0.3f});
            //创建panel
            _panelBBody = _InitPanelBody(panelB, panelBCollider2D);
            _panelLBody = _InitPanelBody(panelL, panelLCollider2D);
            _panelRBody = _InitPanelBody(panelR, panelRCollider2D);
        }

        protected override void OnPreOpen()
        {
            _cacheRootMatrix = root.localToWorldMatrix; //每次打开界面时缓存矩阵
            ball.localPosition = _originBallPos;
            //根据root位置以及小球的世界坐标，得出小球相对于root的局部坐标
            var localPos = root.InverseTransformPoint(ball.position).ToFVector2() / _basePixelsPerUnit;
            _ballBody.SetTransform(localPos, FP.Zero);
            _ballBody.SetLinearVelocity(10 * GetDirectionVectorFromAngle(-30));
        }
        
        private void FixedUpdate()
        {
            if (_fixedDeltaTime == -1)
                _fixedDeltaTime = Time.fixedDeltaTime;
            _world.Step(_fixedDeltaTime, 8, 3);
            //设置位置
            var box2dPos = _ballBody.GetPosition() * _basePixelsPerUnit;
            _cacheBallPos.Set(box2dPos.X.AsFloat, box2dPos.Y.AsFloat, FP.Zero.AsFloat);
            //基于root 将_ballBody的(局部)坐标转换成世界坐标并赋值给ball
            //这里缓存并直接使用root的转换矩阵，减少运算
            ball.position = _cacheRootMatrix.MultiplyPoint3x4(_cacheBallPos);
            //设置角度
            var box2dAngle = _ballBody.GetAngle() * FP.Rad2Deg;
            _cacheBallAngle.Set(0, 0, box2dAngle.AsFloat);
            ball.eulerAngles = _cacheBallAngle;
        }

        private Body _InitPanelBody(RectTransform panel, BoxCollider2D panelCollider2D)
        {
            //设置平面板位置  
            var panelBody = _world.CreateBody(new BodyDef() { BodyType = BodyType.StaticBody, UserData = panel.name});
            //根据root位置以及panel的世界坐标，得出panel相对于root的局部坐标
            var localPos = root.InverseTransformPoint(panel.position).ToFVector2() / _basePixelsPerUnit;
            var angle = panel.eulerAngles.z * FP.Deg2Rad;
            panelBody.SetTransform(localPos, angle);
            //设置平面板大小
            var shape1 = new PolygonShape();
            var size = panelCollider2D.size;
            shape1.SetAsBox(size.x / _basePixelsPerUnit / 2, size.y / _basePixelsPerUnit / 2);
            panelBody.CreateFixture(new FixtureDef {Shape = shape1, Density = 0f, Restitution = 0.2f, Friction = 0.8f});
            return panelBody;
        }
        
        private FVector2 GetDirectionVectorFromAngle(FP angleInDegrees)
        {
            // 将角度从度数转换为弧度
            FP angleInRadians = angleInDegrees * FP.Deg2Rad;
            // 计算 X 和 Y 分量
            FP xComponent = FP.Cos(angleInRadians);
            FP yComponent = FP.Sin(angleInRadians);
            return new FVector2(xComponent, yComponent);
        }

        public void BeginContact(Contact contact)
        {
            var fixtureA = contact.FixtureA;
            var fixtureB = contact.FixtureB;
            Debug.LogError("[1.BeginContact] fixtureA = " + fixtureA.Body.UserData + "  fixtureB = " + fixtureB.Body.UserData);
        }

        public void EndContact(Contact contact)
        {
            var fixtureA = contact.FixtureA;
            var fixtureB = contact.FixtureB;
            Debug.LogError("[2.EndContact] fixtureA = " + fixtureA.Body.UserData + "  fixtureB = " + fixtureB.Body.UserData);
        }

        public void PreSolve(Contact contact, in Manifold oldManifold)
        {
            var fixtureA = contact.FixtureA;
            var fixtureB = contact.FixtureB;
            // Debug.LogError("[3.PreSolve] fixtureA = " + fixtureA.Body.UserData + "  fixtureB = " + fixtureB.Body.UserData);
        }

        public void PostSolve(Contact contact, in ContactImpulse impulse)
        {
            var fixtureA = contact.FixtureA;
            var fixtureB = contact.FixtureB;
            // Debug.LogError("[4.PostSolve] fixtureA = " + fixtureA.Body.UserData + "  fixtureB = " + fixtureB.Body.UserData);
        }
    }
}