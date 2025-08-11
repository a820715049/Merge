/*
 * @Author: qun.chao
 * @Date: 2021-03-31 20:21:02
 */
using UnityEngine;
using UnityEngine.UI;

public class UITilling : BaseMeshEffect
{
    [SerializeField] private Vector2 tilling;
    public float x => tilling.x;
    public float y => tilling.y;

    public void SetTilling(Vector2 t)
    {
        tilling = t;
        base.graphic.SetVerticesDirty();
    }

    #if UNITY_WEBGL


    struct UIVertexFloat
    {
        public UIVertex data;
        public Vector4 color;

        public void FromUIVertex(UIVertex v)
        {
            data = v;
            color = new Vector4(v.color.r, v.color.g, v.color.b, v.color.a);
        }

        public void ToUIVertex(out UIVertex res)
        {
            res = data;
            res.color.r = (byte)Mathf.Round(color.x);
            res.color.g = (byte)Mathf.Round(color.y);
            res.color.b = (byte)Mathf.Round(color.z);
            res.color.a = (byte)Mathf.Round(color.w);
        }

        public void Lerp(ref UIVertexFloat v1, ref UIVertexFloat v2, float factor)
        {
            this.data.position = Vector3.Lerp(v1.data.position, v2.data.position, factor);

            this.color = Vector4.Lerp(v1.color, v2.color, factor);

            this.data.normal = v1.data.normal;

            this.data.tangent = v2.data.normal;

            this.data.uv0 = Vector2.Lerp(v1.data.uv0, v2.data.uv0, factor);
            this.data.uv1 = Vector2.Lerp(v1.data.uv1, v2.data.uv1, factor);
            this.data.uv2 = Vector2.Lerp(v1.data.uv2, v2.data.uv2, factor);
            this.data.uv3 = Vector2.Lerp(v1.data.uv3, v2.data.uv3, factor);
        }
    }

    private UIVertex[] mCachedQuad = new UIVertex[4];
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
        {
            return;
        }

        UIVertex tempV = default;
        vh.PopulateUIVertex(ref tempV, 0);
        UIVertexFloat v1 = default;
        v1.FromUIVertex(tempV);
        vh.PopulateUIVertex(ref tempV, 1);
        UIVertexFloat v2 = default;
        v2.FromUIVertex(tempV);
        vh.PopulateUIVertex(ref tempV, 2);
        UIVertexFloat v3 = default;
        v3.FromUIVertex(tempV);
        vh.PopulateUIVertex(ref tempV, 3);
        UIVertexFloat v4 = default;
        v4.FromUIVertex(tempV);

        vh.Clear();
        int width = Mathf.CeilToInt(tilling.x);
        int height = Mathf.CeilToInt(tilling.y);
        for(int y = 0; y < height; y++)
        {
            float uvy1 = y, uvy2 = y + 1;
            float uvMaxY = 1;
            if(y == height - 1)
            {
                uvy2 = tilling.y;
                uvMaxY = tilling.y - Mathf.Floor(tilling.y);
            }
            UIVertexFloat line1Start = default;
            UIVertexFloat line1End = default;
            UIVertexFloat line2Start = default;
            UIVertexFloat line2End = default;
            line1Start.Lerp(ref v1, ref v2, uvy1 / tilling.y);
            line1End.Lerp(ref v4, ref v3, uvy1 / tilling.y);
            line2Start.Lerp(ref v1, ref v2, uvy2 / tilling.y);
            line2End.Lerp(ref v4, ref v3, uvy2 / tilling.y);
            for(int x = 0; x < width; x++)
            {
                float uvx1 = x, uvx2 = x + 1;
                float uvMaxX = 1;
                if(x == width - 1)
                {
                    uvx2 = tilling.x;
                    uvMaxX = tilling.x - Mathf.Floor(tilling.x);
                }
                UIVertexFloat vv1 = default;
                UIVertexFloat vv2 = default;
                UIVertexFloat vv3 = default;
                UIVertexFloat vv4 = default;
                vv1.Lerp(ref line1Start, ref line1End, uvx1 / tilling.x);
                vv2.Lerp(ref line2Start, ref line2End, uvx1 / tilling.x);
                vv3.Lerp(ref line2Start, ref line2End, uvx2 / tilling.x);
                vv4.Lerp(ref line1Start, ref line1End, uvx2 / tilling.x);
                vv1.data.uv0.Set(0, 0);
                vv2.data.uv0.Set(0, uvMaxY);
                vv3.data.uv0.Set(uvMaxX, uvMaxY);
                vv4.data.uv0.Set(uvMaxX, 0);
                vv1.ToUIVertex(out mCachedQuad[0]);
                vv2.ToUIVertex(out mCachedQuad[1]);
                vv3.ToUIVertex(out mCachedQuad[2]);
                vv4.ToUIVertex(out mCachedQuad[3]);
                vh.AddUIVertexQuad(mCachedQuad);
            }
        }
    }

    #else

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0)
        {
            return;
        }

        // 只处理图 4 verts
        // _UpdateUV(vh, 0, Vector2.zero);
        _UpdateUV(vh, 1, new Vector2(0f, tilling.y));
        _UpdateUV(vh, 2, tilling);
        _UpdateUV(vh, 3, new Vector2(tilling.x, 0f));
    }

    private void _UpdateUV(VertexHelper vh, int index, Vector2 uv)
    {
        UIVertex vert = new UIVertex();
        vh.PopulateUIVertex(ref vert, index);
        vert.uv0 = uv;
        vh.SetUIVertex(vert, index);
    }
    #endif
}