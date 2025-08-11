using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace FAT
{
    public static class MapBuildingMaskSampler 
    {
        /// <summary>
        /// 关键入口：基于连通域拆分 + 分层格子 + 最中心匹配，生成均匀且位于 mask 区域内的 UV 采样点。
        /// </summary>
        /// <param name="uvPoints">外部传入的接收uv点的容器</param>
        /// <param name="pixels">从 GPU 读回的 NativeArray<Color32>，长度 = w*h</param>
        /// <param name="w">RenderTexture 宽度</param>
        /// <param name="h">RenderTexture 高度</param>
        /// <param name="effectCount">希望最终采样的总点数</param>
        /// <param name="threshold">alpha 阈值，小于此值视为“不属于建筑”</param>
        /// <param name="minComponentSize">连通块最小像素数阈值，小于此视为噪点并丢弃</param>
        /// <returns> List<Vector2>，每个 Vector2 是归一化后的 UV 坐标，范围 [0,1] </returns>
        public static void GenerateUniformUVPoints(List<Vector2> uvPoints, NativeArray<Color32> pixels, int w, int h, 
            int effectCount, byte threshold)
        {
            // ——1. 一次性拿到“所有”连通块（minSize=1，相当于不过滤）——
            var rawComponents = GetConnectedComponents(pixels, w, h, threshold, 1);
            if (rawComponents.Count <= 0)
                return;

            // ——2. 从 rawComponents 中提取它们的面积并排序，计算第 10 百分位位置——
            //     （我们默认把最小的 10% 连通域当作“噪点”直接过滤掉）
            var areas = rawComponents
                .Select(c => c.Count)
                .OrderBy(a => a)
                .ToList();

            int percentileIndex = Mathf.Clamp(
                Mathf.RoundToInt(areas.Count * 0.1f), 
                0, 
                areas.Count - 1);
            int minComponentSize = Mathf.Max(areas[percentileIndex], 1);

            // ——3. 原地移除面积 < minComponentSize 的连通块——
            rawComponents.RemoveAll(comp => comp.Count < minComponentSize);
            if (rawComponents.Count <= 0)
                return;

            // ——4. 按各连通块面积比例，在 effectCount 范围内分配采样数——
            int[] sampleCounts = AllocateSampleCounts(rawComponents, effectCount);

            // ——5. 对每个连通块，按分配数量做“分层格子 + 最中心匹配”采样，收集所有 UV——
            for (int i = 0; i < rawComponents.Count; i++)
            {
                int assignedCount = sampleCounts[i];
                if (assignedCount <= 0)
                    continue;

                var compPixels = rawComponents[i];
                // 组件内像素不够时，最多只能取到 compPixels.Count 个
                if (assignedCount > compPixels.Count)
                    assignedCount = compPixels.Count;

                var compUVs = SampleComponentUV(
                    compPixels,        // 本连通块的像素列表
                    assignedCount,     // 已分配的采样数
                    w, h);
                uvPoints.AddRange(compUVs);
            }
        }

        /// <summary>
        /// 扫描整个 pixels，依据 alpha ≥ threshold 拆分连通域，并丢弃像素数 < minSize 的小块
        /// </summary>
        private static List<List<Vector2Int>> GetConnectedComponents(NativeArray<Color32> pixels, int w, int h, byte threshold, int minSize)
        {
            int total = w * h;
            var visited = new bool[total];
            var components = new List<List<Vector2Int>>();

            // 四邻域偏移
            int[] offsetX = { 1, -1, 0, 0 };
            int[] offsetY = { 0, 0, 1, -1 };

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (visited[idx])
                        continue;

                    if (pixels[idx].a < threshold)
                    {
                        // 透明或半透明，跳过
                        visited[idx] = true;
                        continue;
                    }

                    // 新连通块的起点
                    var stack = new Stack<Vector2Int>();
                    var component = new List<Vector2Int>();
                    stack.Push(new Vector2Int(x, y));
                    visited[idx] = true;

                    while (stack.Count > 0)
                    {
                        var p = stack.Pop();
                        component.Add(p);

                        // 遍历四邻域
                        for (int k = 0; k < 4; k++)
                        {
                            int nx = p.x + offsetX[k];
                            int ny = p.y + offsetY[k];
                            if (nx < 0 || nx >= w || ny < 0 || ny >= h)
                                continue;

                            int nIdx = ny * w + nx;
                            if (visited[nIdx] || pixels[nIdx].a < threshold)
                                continue;

                            visited[nIdx] = true;
                            stack.Push(new Vector2Int(nx, ny));
                        }
                    }

                    // 过滤过小连通块
                    if (component.Count >= minSize)
                        components.Add(component);
                }
            }

            return components;
        }

        /// <summary>
        /// 按各连通块面积，在 effectCount 范围内做四舍五入分配，保证总和 = effectCount
        /// </summary>
        private static int[] AllocateSampleCounts(List<List<Vector2Int>> components, int effectCount)
        {
            int n = components.Count;
            var sampleCounts = new int[n];
            var areas = new int[n];
            long totalArea = 0L;

            for (int i = 0; i < n; i++)
            {
                areas[i] = components[i].Count;
                totalArea += areas[i];
            }

            if (totalArea == 0)
                return sampleCounts; // 全部零

            // 先做四舍五入分配
            int sumAssigned = 0;
            for (int i = 0; i < n; i++)
            {
                double raw = (double)areas[i] * effectCount / totalArea;
                sampleCounts[i] = Mathf.RoundToInt((float)raw);
                sumAssigned += sampleCounts[i];
            }

            // 调整使总和等于 effectCount
            if (sumAssigned != effectCount)
            {
                // 差额
                int diff = sumAssigned - effectCount;
                if (diff > 0)
                {
                    // 需要减少 diff 个单位：从那些 sampleCounts[i] > 0 且面积较小的组件开始减少
                    // 简单起见，按 sampleCounts[i] 大小排序，从最大开始减，直到 diff=0
                    var idxs = new List<int>(n);
                    for (int i = 0; i < n; i++) idxs.Add(i);
                    // 按 sampleCounts 降序
                    idxs.Sort((a, b) => sampleCounts[b].CompareTo(sampleCounts[a]));
                    int k = 0;
                    while (diff > 0 && k < idxs.Count)
                    {
                        int i = idxs[k++];
                        if (sampleCounts[i] > 0)
                        {
                            sampleCounts[i]--;
                            diff--;
                        }
                    }
                }
                else
                {
                    // diff < 0，需要增加 |diff|：从那些面积较大的组件开始增加
                    int needed = -diff;
                    var idxs = new List<int>(n);
                    for (int i = 0; i < n; i++) idxs.Add(i);
                    // 按 areas 降序
                    idxs.Sort((a, b) => areas[b].CompareTo(areas[a]));
                    int k = 0;
                    while (needed > 0 && k < idxs.Count)
                    {
                        int i = idxs[k++];
                        sampleCounts[i]++;
                        needed--;
                    }
                }
            }

            return sampleCounts;
        }
        
        /// <summary>
        /// 在单个连通块（compPixels）内，基于“分层格子 + 随机 + 邻域抖动”策略生成均匀且带随机性的 UV 采样点。
        /// - compPixels: 当前连通块所有像素的坐标列表（像素空间下）
        /// - sampleCount: 需要从该连通块中采样的点数
        /// - w, h: 整体 Mask 的宽度和高度，用于将像素坐标转换为 [0,1] 区间的 UV
        /// </summary>
        private static List<Vector2> SampleComponentUV(List<Vector2Int> compPixels, int sampleCount, int w, int h)
        {
            // 最终返回的 UV 列表，预分配容量为 sampleCount
            var result = new List<Vector2>(sampleCount);

            // ——1. 计算连通块在像素空间的外接矩形——
            // 用 minX,maxX,minY,maxY 来记录整块掩膜的边界像素
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var p in compPixels)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            // 外接矩形的宽高
            int maskW = maxX - minX + 1;
            int maskH = maxY - minY + 1;
            // 如果计算所得宽高不合法，直接返回空列表
            if (maskW <= 0 || maskH <= 0)
                return result;

            // ——2. 计算网格划分大小 gridX × gridY —— 
            // 我们希望总格子数 ≈ sampleCount，并让 (gridX:gridY) 的长宽比接近 (maskW:maskH)
            float aspect = maskW / (float)maskH;
            int gridX = Mathf.CeilToInt(Mathf.Sqrt(sampleCount * aspect));
            int gridY = Mathf.CeilToInt(sampleCount / (float)gridX);
            // 保证至少为 1
            if (gridX <= 0) gridX = 1;
            if (gridY <= 0) gridY = 1;
            // 每个格子在像素空间下的宽度和高度
            float cellW = maskW / (float)gridX;
            float cellH = maskH / (float)gridY;

            // ——3. 将 compPixels 按格子分桶存储——
            // buckets[gx,gy] 中存放该格子内所有的像素坐标
            var buckets = new List<Vector2Int>[gridX, gridY];
            for (int gx = 0; gx < gridX; gx++)
                for (int gy = 0; gy < gridY; gy++)
                    buckets[gx, gy] = new List<Vector2Int>();

            // 遍历连通块内所有像素，根据其在外接矩形的相对位置计算属于哪个格子
            foreach (var p in compPixels)
            {
                float localX = p.x - minX; // 转为相对于外接矩形左下角的局部坐标
                float localY = p.y - minY;
                int gx = Mathf.Clamp((int)(localX / cellW), 0, gridX - 1);
                int gy = Mathf.Clamp((int)(localY / cellH), 0, gridY - 1);
                buckets[gx, gy].Add(p);
            }

            // 为了在后续“邻域抖动”时快速判断某个像素是否在连通块内，构造一个 HashSet
            var compSet = new HashSet<Vector2Int>(compPixels);

            // 用于随机数生成
            var rand = new System.Random();

            // 记录已经挑选过的像素，避免补点阶段重复选中
            var usedPixels = new HashSet<Vector2Int>();

            // ——4. 遍历每个格子：随机选一个像素 + 邻域内随机抖动 —— 
            for (int gx = 0; gx < gridX; gx++)
            {
                for (int gy = 0; gy < gridY; gy++)
                {
                    var bucket = buckets[gx, gy];
                    if (bucket.Count == 0)
                        continue; // 如果该格子没有任何像素，则跳过

                    // 4.1 在当前格子里随机选一个像素（等概率）
                    int idx = rand.Next(bucket.Count);
                    var chosen = bucket[idx];

                    // 4.2 给选中的像素做“邻域抖动”：在 [-1, +1] 范围内随机偏移，最多尝试 3 次
                    //     只有当偏移后像素仍在 compSet（连通块内）时才接受，否则保留原点
                    Vector2Int jittered = chosen;
                    for (int t = 0; t < 3; t++)
                    {
                        int dx = rand.Next(-1, 1); // -1, 0, 1
                        int dy = rand.Next(-1, 1);
                        var candidate = new Vector2Int(chosen.x + dx, chosen.y + dy);
                        // 如果偏移后的坐标仍属于连通块，则使用该坐标并跳出循环
                        if (compSet.Contains(candidate))
                        {
                            jittered = candidate;
                            break;
                        }
                    }

                    // 4.3 记录已选像素，防止补点阶段重复
                    usedPixels.Add(jittered);

                    // 4.4 把 jittered 像素坐标转换为归一化 UV
                    float u = (jittered.x + 0.5f) / w;
                    float v = (jittered.y + 0.5f) / h;
                    result.Add(new Vector2(u, v));
                }
            }

            // ——5. 如果已选点数超过 sampleCount，随机截断到 sampleCount 个——
            if (result.Count > sampleCount)
            {
                // Fisher–Yates 随机打乱 result 列表，然后截掉多余元素
                for (int i = 0; i < result.Count; i++)
                {
                    int j = rand.Next(i, result.Count);
                    var tmp = result[i];
                    result[i] = result[j];
                    result[j] = tmp;
                }
                result.RemoveRange(sampleCount, result.Count - sampleCount);
            }

            // ——6. 如果已选点数不足 sampleCount，需要补点 —— 
            if (result.Count < sampleCount)
            {
                int toAdd = sampleCount - result.Count;
                int compTotal = compPixels.Count;

                for (int i = 0; i < toAdd; i++)
                {
                    int attempts = 0;
                    while (attempts < compTotal)
                    {
                        int ridx = rand.Next(compTotal);
                        var candidate = compPixels[ridx];

                        // 如果该候选像素还没被使用，就尝试补点
                        if (!usedPixels.Contains(candidate))
                        {
                            // 在候选像素周围做同样的邻域抖动
                            Vector2Int finalPixel = candidate;
                            for (int t = 0; t < 3; t++)
                            {
                                int dx = rand.Next(-1, 1);
                                int dy = rand.Next(-1, 1);
                                var nb = new Vector2Int(candidate.x + dx, candidate.y + dy);
                                if (compSet.Contains(nb))
                                {
                                    finalPixel = nb;
                                    break;
                                }
                            }

                            usedPixels.Add(finalPixel); // 标记为已用
                            float u2 = (finalPixel.x + 0.5f) / w;
                            float v2 = (finalPixel.y + 0.5f) / h;
                            result.Add(new Vector2(u2, v2));
                            break;
                        }
                        attempts++;
                    }
                    // 如果尝试 compTotal 次都没找到可用像素，则跳出循环
                    if (attempts >= compTotal)
                        break;
                }
            }

            return result;
        }

    }
}