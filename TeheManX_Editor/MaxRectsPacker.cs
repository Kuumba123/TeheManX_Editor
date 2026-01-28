using System;
using System.Collections.Generic;

namespace TeheManX_Editor
{
    struct PackRect
    {
        public int width;
        public int height;
        public int x;
        public int y;
    }
    internal class MaxRectsPacker
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<PackRect> FreeRects { get; private set; }
        public MaxRectsPacker(int width, int height)
        {
            Width = width;
            Height = height;
            FreeRects = new List<PackRect>();
            FreeRects.Add(new PackRect() { x = 0, y = 0, width = width, height = height });
        }
        public void Initialize(int width, int height)
        {
            Width = width;
            Height = height;
            FreeRects.Clear();
            FreeRects.Add(new PackRect() { x = 0, y = 0, width = width, height = height });
        }
        public void Commit(int freeIndex, int usedWidth, int usedHeight)
        {
            PackRect free = FreeRects[freeIndex];

            // Right slice
            if (free.width > usedWidth)
            {
                FreeRects.Add(new PackRect
                {
                    x = free.x + usedWidth,
                    y = free.y,
                    width = free.width - usedWidth,
                    height = usedHeight
                });
            }

            // Bottom slice
            if (free.height > usedHeight)
            {
                FreeRects.Add(new PackRect
                {
                    x = free.x,
                    y = free.y + usedHeight,
                    width = free.width,
                    height = free.height - usedHeight
                });
            }

            FreeRects.RemoveAt(freeIndex);
        }

        public bool TryInsert(int width, int height, out PackRect results)
        {
            results = new PackRect();

            int bestScore = int.MaxValue;
            int bestIndex = -1;

            for (int i = 0; i < FreeRects.Count; i++)
            {
                PackRect free = FreeRects[i];

                if (free.width >= width && free.height >= height)
                {
                    int leftoverW = free.width - width;
                    int leftoverH = free.height - height;
                    int score = Math.Min(leftoverW, leftoverH);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;

                        results.x = free.x;
                        results.y = free.y;
                        results.width = width;
                        results.height = height;
                    }
                }
            }

            return bestIndex != -1;
        }
        public bool TryInsertCommit(int width, int height, out PackRect results)
        {
            results = new PackRect();

            int bestScore = int.MaxValue;
            int bestIndex = -1;

            for (int i = 0; i < FreeRects.Count; i++)
            {
                PackRect free = FreeRects[i];

                if (free.width >= width && free.height >= height)
                {
                    int leftoverW = free.width - width;
                    int leftoverH = free.height - height;
                    int score = Math.Min(leftoverW, leftoverH);

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;

                        results.x = free.x;
                        results.y = free.y;
                        results.width = width;
                        results.height = height;
                    }
                }
            }

            if (bestIndex == -1)
                return false;

            Commit(bestIndex, width, height);
            return true;
        }
    }
}
