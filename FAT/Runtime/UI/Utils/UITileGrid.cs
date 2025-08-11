/*
 * @Author: qun.chao
 * @Date: 2021-07-22 12:33:35
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class UVHelper
{
    // 记录3x3格子涉及到的uv坐标
    private static Vector2[,] coord = new Vector2[4, 4];

    static UVHelper()
    {
        _CalcUV();
    }

    // bottom -> top / left -> right
    private static void _CalcUV()
    {
        var div3 = 1f / 3f;

        // bottom row
        coord[0, 0] = Vector2.zero;
        coord[0, 1] = new Vector2(div3, 0f);
        coord[0, 2] = new Vector2(1f - div3, 0f);
        coord[0, 3] = Vector2.right;

        // mid 1
        coord[1, 0] = new Vector2(0f, div3);
        coord[1, 1] = new Vector2(div3, div3);
        coord[1, 2] = new Vector2(1f - div3, div3);
        coord[1, 3] = new Vector2(1f, div3);

        // mid 2
        coord[2, 0] = new Vector2(0f, 1f - div3);
        coord[2, 1] = new Vector2(div3, 1f - div3);
        coord[2, 2] = new Vector2(1f - div3, 1f - div3);
        coord[2, 3] = new Vector2(1f, 1f - div3);

        // top row
        coord[3, 0] = Vector2.up;
        coord[3, 1] = new Vector2(div3, 1f);
        coord[3, 2] = new Vector2(1f - div3, 1f);
        coord[3, 3] = Vector2.one;
    }

    public static Vector2 GetUV(int row, int col)
    {
        return coord[col, row];
    }
}

/*
1x1 1x3 3x1 3x3 四种tile适用不同尺寸的区域
原始tile图片由外部指定
*/
public class UITileGrid : BaseMeshEffect
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private float size;

    private UIVertex[] cache = new UIVertex[4];
    private int curX = 0;
    private int curY = 0;
    private const float div3 = 1 / 3f;

    public void SetSize(int w, int h)
    {
        width = w;
        height = h;
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;
        vh.Clear();
        _Build(vh);
    }

    private void _Build(VertexHelper vh)
    {
        if (width < 1 || height < 1)
            return;

        curX = 0;
        curY = 0;

        if (width == 1 && height == 1)
        {
            _Fill_1x1(vh);
            return;
        }

        if (width == 1)
        {
            _Fill_1x3_Bottom(vh);
            ++curY;
            while (curY < height - 1)
            {
                _Fill_1x3_Mid(vh);
                ++curY;
            }
            _Fill_1x3_Top(vh);
        }
        else if (height == 1)
        {
            _Fill_3x1_Left(vh);
            ++curX;
            while (curX < width - 1)
            {
                _Fill_3x1_Mid(vh);
                ++curX;
            }
            _Fill_3x1_Right(vh);
        }
        else
        {
            // bottom line
            _Fill_3x3_BL(vh);
            ++curX;
            while (curX < width - 1)
            {
                _Fill_3x3_BM(vh);
                ++curX;
            }
            _Fill_3x3_BR(vh);
            ++curY;
            // all mid line
            while (curY < height - 1)
            {
                curX = 0;
                _Fill_3x3_ML(vh);
                ++curX;
                while (curX < width - 1)
                {
                    _Fill_3x3_MM(vh);
                    ++curX;
                }
                _Fill_3x3_MR(vh);
                ++curY;
            }
            // top line
            curX = 0;
            _Fill_3x3_TL(vh);
            ++curX;
            while (curX < width - 1)
            {
                _Fill_3x3_TM(vh);
                ++curX;
            }
            _Fill_3x3_TR(vh);
        }
    }

    private Vector2 _UV(int row, int col)
    {
        return UVHelper.GetUV(row, col);
    }

    private void _MakeQuad(VertexHelper vh, Vector2 uv00, Vector2 uv10, Vector2 uv11, Vector2 uv01)
    {
        var v = new UIVertex();
        v.position = new Vector2(size * curX, size * curY);
        v.color = Color.white;
        v.uv0 = uv00;
        cache[0] = v;

        v.position = new Vector2(size * curX + size, size * curY);
        v.color = Color.white;
        v.uv0 = uv10;
        cache[1] = v;

        v.position = new Vector2(size * curX + size, size * curY + size);
        v.color = Color.white;
        v.uv0 = uv11;
        cache[2] = v;

        v.position = new Vector2(size * curX, size * curY + size);
        v.color = Color.white;
        v.uv0 = uv01;
        cache[3] = v;

        vh.AddUIVertexQuad(cache);
    }

    #region 1x1
    private void _Fill_1x1(VertexHelper vh)
    {
        _MakeQuad(vh, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
    }
    #endregion

    #region 1x3
    private void _Fill_1x3_Bottom(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(0, 0), _UV(3, 0), _UV(3, 1), _UV(0, 1));
    }

    private void _Fill_1x3_Mid(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(0, 1), _UV(3, 1), _UV(3, 2), _UV(0, 2));
    }

    private void _Fill_1x3_Top(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(0, 2), _UV(3, 2), _UV(3, 3), _UV(0, 3));
    }
    #endregion

    #region 3x1
    private void _Fill_3x1_Left(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(0, 0), _UV(1, 0), _UV(1, 3), _UV(0, 3));
    }

    private void _Fill_3x1_Mid(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(1, 0), _UV(2, 0), _UV(2, 3), _UV(1, 3));
    }

    private void _Fill_3x1_Right(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(2, 0), _UV(3, 0), _UV(3, 3), _UV(2, 3));
    }
    #endregion

    #region 3x3
    private void _Fill_3x3_BL(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(0, 0), _UV(1, 0), _UV(1, 1), _UV(0, 1));
    }

    private void _Fill_3x3_BM(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(1, 0), _UV(2, 0), _UV(2, 1), _UV(1, 1));
    }

    private void _Fill_3x3_BR(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(2, 0), _UV(3, 0), _UV(3, 1), _UV(2, 1));
    }

    private void _Fill_3x3_ML(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(0, 1), _UV(1, 1), _UV(1, 2), _UV(0, 2));
    }

    private void _Fill_3x3_MM(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(1, 1), _UV(2, 1), _UV(2, 2), _UV(1, 2));
    }

    private void _Fill_3x3_MR(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(2, 1), _UV(3, 1), _UV(3, 2), _UV(2, 2));
    }

    private void _Fill_3x3_TL(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(0, 2), _UV(1, 2), _UV(1, 3), _UV(0, 3));
    }

    private void _Fill_3x3_TM(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(1, 2), _UV(2, 2), _UV(2, 3), _UV(1, 3));
    }

    private void _Fill_3x3_TR(VertexHelper vh)
    {
        _MakeQuad(vh, _UV(2, 2), _UV(3, 2), _UV(3, 3), _UV(2, 3));
    }
    #endregion
}