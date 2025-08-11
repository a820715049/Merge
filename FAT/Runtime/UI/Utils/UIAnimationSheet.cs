/*
 * @Author: qun.chao
 * @Date: 2021-03-09 20:26:21
 */
using UnityEngine;
using UnityEngine.UI;

public class UIAnimationSheet : BaseMeshEffect
{
    [SerializeField] private float fps = 30;
    [SerializeField] private bool restartOnEnable;
    [SerializeField] private bool topToDown;
    [SerializeField] private bool loop;
    [SerializeField] private int col;
    [SerializeField] private int row;
    [SerializeField] private float loopInterval;

    private int curFrame;

    private Vector2 tempVec;
    private UIVertex tempVert;

    private float stepX;
    private float stepY;
    private float frameInterval;
    private float lastFrameTime;

    protected override void Awake()
    {
        base.Awake();

        _RefreshParams();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (restartOnEnable)
        {
            curFrame = 0;
            lastFrameTime = Time.realtimeSinceStartup;
            base.graphic.SetVerticesDirty();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    public void SetFps(int _fps)
    {
        fps = _fps;
        _RefreshParams();
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup - lastFrameTime > frameInterval)
        {
            lastFrameTime = Time.realtimeSinceStartup;
            if (loop)
            {
                curFrame = (curFrame + 1) % (col * row);
                if (curFrame == col * row - 1)
                {
                    // 最后1张 增加额外等待时间
                    lastFrameTime += loopInterval;
                }
            }
            else
            {
                if (curFrame < col * row - 1)
                {
                    ++curFrame;
                }
            }
            base.graphic.SetVerticesDirty();
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        _RefreshParams();
    }
#endif

    private void _RefreshParams()
    {
        if (col <= 0) col = 1;
        if (row <= 0) row = 1;
        if (fps <= 0) fps = 30;

        stepX = 1f / col;
        stepY = 1f / row;

        frameInterval = 1f / fps;
    }

    private void _CalcUV(ref Vector2 uv, int frame)
    {
        int coord_x = frame % col;
        int coord_y = frame / col;

        if (topToDown)
        {
            uv.x = stepX * coord_x;
            uv.y = 1f - stepY - stepY * coord_y;
        }
        else
        {
            uv.x = stepX * coord_x;
            uv.y = stepY * coord_y;
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
        {
            return;
        }

        // 只处理图 4 verts
        _UpdateUV(vh, tempVert, 0, Vector2.zero);
        _UpdateUV(vh, tempVert, 1, new Vector2(0f, stepY));
        _UpdateUV(vh, tempVert, 2, new Vector2(stepX, stepY));
        _UpdateUV(vh, tempVert, 3, new Vector2(stepX, 0f));
    }

    private void _UpdateUV(VertexHelper vh, UIVertex vert, int index, Vector2 offset)
    {
        vh.PopulateUIVertex(ref vert, index);
        _CalcUV(ref tempVec, curFrame);
        vert.uv0 = tempVec + offset;
        vh.SetUIVertex(vert, index);
    }
}