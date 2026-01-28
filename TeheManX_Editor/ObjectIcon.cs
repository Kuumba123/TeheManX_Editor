using SkiaSharp;
using System;

namespace TeheManX_Editor
{
    public class ObjectIcon
    {
        #region Properties
        public SKBitmap SpriteBitmap { get; private set; }
        public int PaletteId { get; private set; }   // Predefined palette ID
        public int SpriteFrame { get; private set; }
        public int SpriteId { get; private set; }    // Dynamic sprite ID based on enemy id
        public int TileId { get; private set; }      // Dynamic Id used to figure what se to of Compressed Tiles to load
        public int TileBase { get; private set; }    // Base tile index (mainly cus of capsule)
        public bool LoadFromTileSpec { get; private set; } = true; // Whether to load tile data from the tile specification table
        public int SpriteDataOffset { get; private set; } // Offset to sprite arragnment data in ROM
        public int BitmapX { get; private set; } // X position in the bitmap
        public int BitmapY { get; private set; } // Y position in the bitmap

        public int Left { get; private set; }
        public int Top { get; private set; }
        public int Right { get; private set; }
        public int Bottom { get; private set; }

        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }
        public int Area { get { return Width * Height; } }
        public int CenterX { get { return (Left + Right) / 2; } }
        public int CenterY { get { return (Top + Bottom) / 2; } }
        #endregion

        #region Constructors
        public ObjectIcon()
        {
        }
        public ObjectIcon(int paletteId)
        {
            PaletteId = paletteId;
        }
        public ObjectIcon(int paletteId, int spriteFrame)
        {
            PaletteId = paletteId;
            SpriteFrame = spriteFrame;
        }
        public ObjectIcon(int paletteId, int tileBase, bool loadFromSpec)
        {
            PaletteId = paletteId;
            TileBase = tileBase;
            LoadFromTileSpec = loadFromSpec;
        }
        public ObjectIcon(int paletteId, int tileBase, int compressTileId, int spriteId, int spriteFrame)
        {
            PaletteId = paletteId;
            TileBase = tileBase;
            TileId = compressTileId;
            SpriteId = spriteId;
            SpriteFrame = spriteFrame;
        }
        #endregion Constructors

        #region Methods
        public void Draw(SKCanvas canvas, float x, float y) // Draws the sprite with via top-left corner at (x, y)
        {
            SKRect src = new SKRect(BitmapX, BitmapY, BitmapX + Width, BitmapY + Height);
            SKRect dst = new SKRect(x, y, x + Width, y + Height);
            canvas.DrawBitmap(SpriteBitmap, src, dst);
        }
        public void DrawCentre(SKCanvas canvas, float x, float y)
        {
            SKRect src = new SKRect(BitmapX, BitmapY, BitmapX + Width, BitmapY + Height);
            float drawX = x - (Width / 2);
            float drawY = y - (Height / 2);
            SKRect dst = new SKRect(drawX, drawY, drawX + Width, drawY + Height);
            canvas.DrawBitmap(SpriteBitmap, src, dst);
        }
        public void ExtractSpriteData(int spriteId, int spriteDataOffset, int tileIdCMP)
        {
            SpriteId = spriteId;
            SpriteDataOffset = spriteDataOffset;
            TileId = tileIdCMP;
            int count = SNES.rom[spriteDataOffset];
            int offset = spriteDataOffset + 1;


            for (int i = 0; i < count; i++)
            {
                int x;
                int y;
                bool size16;

                if (Const.Id == Const.GameId.MegaManX)
                {
                    x = (sbyte)SNES.rom[offset + 0];
                    y = (sbyte)SNES.rom[offset + 1];
                    size16 = (SNES.rom[offset + 3] & 0x20) != 0;
                }
                else
                {
                    x = (sbyte)SNES.rom[offset + 1];
                    y = (sbyte)SNES.rom[offset + 2];
                    size16 = (SNES.rom[offset + 0] & 0x20) != 0;
                }

                Left = Math.Min(Left, x);
                Top = Math.Min(Top, y);

                if (size16) // 16x16
                {
                    Right = Math.Max(Right, x + 16);
                    Bottom = Math.Max(Bottom, y + 16);
                }
                else // 8x8
                {
                    Right = Math.Max(Right, x + 8);
                    Bottom = Math.Max(Bottom, y + 8);
                }
                offset += 4;
            }
        }
        private unsafe void DumpSpriteTile(byte* bmpPtr, int stride, int drawX, int drawY, bool flipH, bool flipV, byte* tilesPtr, int tileId, uint* palettePtr)
        {
            byte* readBasePtr = tilesPtr + tileId * 32;
            byte* drawPtr = bmpPtr + drawY * stride + drawX * 4;

            for (int ty = 0; ty < 8; ty++)
            {
                int pixelY = flipV ? 7 - ty : ty;

                byte* base1 = readBasePtr + (pixelY * 2);
                byte* base2 = readBasePtr + 0x10 + (pixelY * 2);

                for (int tx = 0; tx < 8; tx++)
                {
                    int bit = 7 - tx;

                    int p0 = (base1[0] >> bit) & 1;
                    int p1 = (base1[1] >> bit) & 1;
                    int p2 = (base2[0] >> bit) & 1;
                    int p3 = (base2[1] >> bit) & 1;

                    int colorIndex = p0 | (p1 << 1) | (p2 << 2) | (p3 << 3);
                    if (colorIndex == 0)
                        continue;

                    int destX = flipH ? (7 - tx) : tx;
                    byte* pixelPtr = drawPtr + destX * 4;

                    *(uint*)pixelPtr = palettePtr[colorIndex];
                }

                drawPtr += stride;
            }
        }
        public void DumpSpriteToBitmap(SKBitmap bitmap, int destX, int destY, byte[] tiles, uint[] palette)
        {
            SpriteBitmap = bitmap;
            int count = SNES.rom[SpriteDataOffset];
            int offset = SpriteDataOffset + 1 + (count - 1) * 4;

            int drawBaseX = destX + -Left;
            int drawBaseY = destY + -Top;
            BitmapX = destX;
            BitmapY = destY;

            unsafe
            {
                byte* bmpPtr = (byte*)bitmap.GetPixels();
                int stride = bitmap.RowBytes;

                fixed (byte* tilesPtr = tiles)
                fixed (uint* palettePtr = palette)
                {

                    for (int i = 0; i < count; i++)
                    {
                        int x;
                        int y;
                        int tileId;
                        int attribute;

                        if (Const.Id == Const.GameId.MegaManX)
                        {
                            x = (sbyte)SNES.rom[offset + 0];
                            y = (sbyte)SNES.rom[offset + 1];
                            tileId = SNES.rom[offset + 2];
                            attribute = SNES.rom[offset + 3];
                        }
                        else
                        {
                            x = (sbyte)SNES.rom[offset + 1];
                            y = (sbyte)SNES.rom[offset + 2];
                            tileId = SNES.rom[offset + 3];
                            attribute = SNES.rom[offset + 0];
                        }

                        bool hFlip = (attribute & 0x40) != 0;
                        bool vFlip = (attribute & 0x80) != 0;
                        bool size16 = (attribute & 0x20) != 0;

                        if (size16)
                        {
                            int tileIdTL = tileId;
                            int tileIdTR = tileId + 1;
                            int tileIdBL = tileId + 16;
                            int tileIdBR = tileId + 17;

                            /*When Dumping 16x16 Tiles we need to swap the actual indavisual tiles if flipping is used*/

                            if (hFlip)
                            {
                                (tileIdTL, tileIdTR) = (tileIdTR, tileIdTL);
                                (tileIdBL, tileIdBR) = (tileIdBR, tileIdBL);
                            }
                            if (vFlip)
                            {
                                (tileIdTL, tileIdBL) = (tileIdBL, tileIdTL);
                                (tileIdTR, tileIdBR) = (tileIdBR, tileIdTR);
                            }
                            DumpSpriteTile(bmpPtr, stride, drawBaseX + x, drawBaseY + y, hFlip, vFlip, tilesPtr, tileIdTL + TileBase, palettePtr);
                            DumpSpriteTile(bmpPtr, stride, drawBaseX + x + 8, drawBaseY + y, hFlip, vFlip, tilesPtr, tileIdTR + TileBase, palettePtr);
                            DumpSpriteTile(bmpPtr, stride, drawBaseX + x, drawBaseY + y + 8, hFlip, vFlip, tilesPtr, tileIdBL + TileBase, palettePtr);
                            DumpSpriteTile(bmpPtr, stride, drawBaseX + x + 8, drawBaseY + y + 8, hFlip, vFlip, tilesPtr, tileIdBR + TileBase, palettePtr);
                        }
                        else
                            DumpSpriteTile(bmpPtr, stride, drawBaseX + x, drawBaseY + y, hFlip, vFlip, tilesPtr, tileId + TileBase, palettePtr);

                        offset -= 4; //Loop through the OAM tiles backwards to mimic the SNES drawing order
                    }
                }
            }
        }
        #endregion Methods
    }
}
