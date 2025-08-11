
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Rounded Rectangle")]
public class UIRoundedRectangle : Graphic
{
    [Range(0, 100)] public float cornerRadius = 10f;
    [Range(4, 32)] public int segments = 8;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(cornerRadius, rect.width / 2, rect.height / 2);

        // 生成中心矩形和四个圆角
        Vector2 center = rect.center;
        AddCenterQuad(vh,
            new Vector2(center.x - rect.width / 2 + radius, center.y - rect.height / 2 + radius),
            new Vector2(center.x + rect.width / 2 - radius, center.y + rect.height / 2 - radius));
        GenerateEdgeQuads(vh, rect, radius);

        GenerateCorner(vh, rect, radius, new Vector2(1, 1), 0);
        GenerateCorner(vh, rect, radius, new Vector2(-1, 1), 90);
        GenerateCorner(vh, rect, radius, new Vector2(-1, -1), 180);
        GenerateCorner(vh, rect, radius, new Vector2(1, -1), 270);
    }

    void GenerateEdgeQuads(VertexHelper vh, Rect rect, float radius)
    {
        // 上边
        AddQuad(vh,
            new Vector2(rect.xMin + radius, rect.yMax - radius),
            new Vector2(rect.xMax - radius, rect.yMax),
            new Vector2(0, 1));

        // 下边
        AddQuad(vh,
            new Vector2(rect.xMin + radius, rect.yMin),
            new Vector2(rect.xMax - radius, rect.yMin + radius),
            new Vector2(0, -1));

        // 左边
        AddQuad(vh,
            new Vector2(rect.xMin, rect.yMin + radius),  // 左下角
            new Vector2(rect.xMin + radius, rect.yMax - radius),  // 右上角
            new Vector2(-1, 0));

        // 右边
        AddQuad(vh,
            new Vector2(rect.xMax - radius, rect.yMin + radius),
            new Vector2(rect.xMax, rect.yMax - radius),
            new Vector2(1, 0));
    }

    void AddQuad(VertexHelper vh, Vector2 start, Vector2 end, Vector2 uv)
    {
        int vertIndex = vh.currentVertCount;

        // 生成带调试颜色的顶点
        Color debugColor = new Color(uv.x, uv.y, 0, 1);

        vh.AddVert(new Vector2(start.x, start.y), debugColor, uv);
        vh.AddVert(new Vector2(start.x, end.y), debugColor, uv);
        vh.AddVert(new Vector2(end.x, end.y), debugColor, uv);
        vh.AddVert(new Vector2(end.x, start.y), debugColor, uv);

        vh.AddTriangle(vertIndex, vertIndex + 1, vertIndex + 2);
        vh.AddTriangle(vertIndex, vertIndex + 2, vertIndex + 3);
    }

    void GenerateCorner(VertexHelper vh, Rect rect, float radius, Vector2 dir, float startAngle)
    {
        Vector2 center = rect.center;
        Vector2 cornerCenter = new Vector2(
            center.x + dir.x * (rect.width / 2 - radius),
            center.y + dir.y * (rect.height / 2 - radius));

        int centerIndex = vh.currentVertCount;
        vh.AddVert(cornerCenter, color, Vector2.zero);

        float angleStep = 90f / segments;
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + i * angleStep;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 pos = cornerCenter + new Vector2(
                Mathf.Cos(rad) * radius * Mathf.Abs(dir.x),
                Mathf.Sin(rad) * radius * Mathf.Abs(dir.y));

            vh.AddVert(pos, color, Vector2.zero);

            if (i > 0)
            {
                int currentIndex = vh.currentVertCount - 1;
                vh.AddTriangle(centerIndex, currentIndex - 1, currentIndex);
            }
        }
    }

    void AddCenterQuad(VertexHelper vh, Vector2 min, Vector2 max)
    {
        int vertIndex = vh.currentVertCount;
        vh.AddVert(new Vector3(min.x, min.y), color, Vector2.zero); // 左下
        vh.AddVert(new Vector3(max.x, min.y), color, Vector2.zero); // 右下
        vh.AddVert(new Vector3(max.x, max.y), color, Vector2.zero); // 右上
        vh.AddVert(new Vector3(min.x, max.y), color, Vector2.zero); // 左上

        Vector2 center = (min + max) * 0.5f;
        int centerIndex = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);

        // 连接四个三角形扇面
        vh.AddTriangle(vertIndex, vertIndex + 1, centerIndex);
        vh.AddTriangle(vertIndex + 1, vertIndex + 2, centerIndex);
        vh.AddTriangle(vertIndex + 2, vertIndex + 3, centerIndex);
        vh.AddTriangle(vertIndex + 3, vertIndex, centerIndex);
    }
}