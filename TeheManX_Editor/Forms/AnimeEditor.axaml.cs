using Avalonia.Controls;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using static TeheManX_Editor.PaletteAnime;

namespace TeheManX_Editor.Forms;

public partial class AnimeEditor : UserControl
{
    #region Fields
    public static List<PaletteAnime> PaletteAnimes = new List<PaletteAnime>();
    #endregion Fields

    #region Constructor
    public AnimeEditor()
    {
        InitializeComponent();

        loopInt.Maximum = -1;
    }
    #endregion Constructor

    #region Methods
    public void CollectData()
    {
        if (Level.Project.PaletteAnimes == null)
        {
            Const.PaletteAnimeColorBank = Const.PaletteColorBank;
            List<PaletteAnime> paletteAnimes = new List<PaletteAnime>();

            bool x1 = Const.Id == Const.GameId.MegaManX;

            int readBank = Const.PaletteAnimeInfoOffset / 0x8000;
            int maxOffset = int.MinValue;

            for (int i = 0; i < Const.PaletteAnimeCount; i++)
            {
                int readOffset = SNES.CpuToOffset(BinaryPrimitives.ReadInt16LittleEndian(SNES.rom.AsSpan(Const.PaletteAnimeInfoOffset + i * 2)), readBank);

                PaletteAnime anime = new PaletteAnime();

                anime.ColorIndex = SNES.rom[readOffset];

                if (x1)
                {
                    anime.ColorIndex = SNES.rom[readOffset];
                    anime.ColorCount = 16;
                    readOffset++;
                }
                else
                {
                    anime.ColorIndex = (byte)(SNES.rom[readOffset] / 2);
                    anime.ColorCount = SNES.rom[readOffset + 1];
                    readOffset += 2;
                }

                while (true) // Loop through frames until we find a loop command (timer = 0).
                {
                    byte timer = SNES.rom[readOffset];

                    if (timer == 0) // Loop command, no more frames after this one.
                    {
                        short relativeOffset = BinaryPrimitives.ReadInt16LittleEndian(SNES.rom.AsSpan(readOffset + 1));
                        anime.LoopIndex = relativeOffset / 3;
                        maxOffset = Math.Max(maxOffset, readOffset + 3);
                        break;
                    }

                    // Normal frame.
                    ushort colorPointer = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(readOffset + 1));
                    PaletteFrame frame = new PaletteFrame() { Timer = timer, ColorPointer = colorPointer };
                    anime.Frames.Add(frame);
                    readOffset += 3;
                }

                // Get the sides of the palette anime's effect area.
                ushort leftSide, rightSide, bottomSide, topSide;

                if (x1)
                {
                    //Left Side Entries then Right Sides Entries
                    int leftSideOffset = Const.PaletteAnimeBoundsOffset + (i * 2);
                    int rightSideOffset = leftSideOffset + Const.PaletteAnimeCount * 2;
                    leftSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(leftSideOffset));
                    rightSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(rightSideOffset));
                    topSide = 0;
                    bottomSide = 0x2000; // X1 doesn't have bottom and top sides, so set them to default values that won't cause issues.
                }
                else
                {
                    int sideOffset = Const.PaletteAnimeBoundsOffset + (i * 8);
                    leftSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(sideOffset));
                    rightSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(sideOffset + 2));
                    topSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(sideOffset + 4));
                    bottomSide = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(sideOffset + 6));
                }
                anime.LeftSide = leftSide;
                anime.RightSide = rightSide;
                anime.BottomSide = bottomSide;
                anime.TopSide = topSide;

                // Add the palette anime to the list.
                paletteAnimes.Add(anime);
            }

            PaletteAnimes = paletteAnimes;
        }
        else
        {
            PaletteAnimes = Level.Project.PaletteAnimes;
            Const.PaletteAnimeColorBank = Level.Project.PaletteAnimeColorBank;
        }

        animeInt.Maximum = PaletteAnimes.Count - 1;
        if (animeInt.Value > animeInt.Maximum)
            animeInt.Value = animeInt.Maximum;

        frameInt.Value = 0;

        SetAnimeIntValues();
        SetFrameIntValues();
    }
    public static void GetSharedAnimeInfo(List<PaletteAnime> sourceSettings, int[] shared)
    {
        // 1. Initialize all entries as un-shared
        for (int i = 0; i < sourceSettings.Count; i++)
            shared[i] = -1;

        // 2. Scan each pointer starting from index 1
        for (int i = 1; i < sourceSettings.Count; i++)
        {
            ushort animePointer = BinaryPrimitives.ReadUInt16LittleEndian(
                SNES.rom.AsSpan(Const.PaletteAnimeInfoOffset + i * 2)
            );

            // Scan ALL previous indices from 0 up to i - 1
            for (int j = 0; j < i; j++)
            {
                ushort comparePointer = BinaryPrimitives.ReadUInt16LittleEndian(
                    SNES.rom.AsSpan(Const.PaletteAnimeInfoOffset + j * 2)
                );

                if (animePointer == comparePointer)
                {
                    // Link directly to the root source index if j was also shared
                    shared[i] = (shared[j] == -1) ? j : shared[j];
                    break; // Found the original match, stop searching
                }
            }
        }
    }
    public static byte[] CreateAnimeInfoData(List<PaletteAnime> sourceSettings,int[] shared, int baseCpu)
    {
        baseCpu &= 0xFFFF;
        bool x1 = Const.Id == Const.GameId.MegaManX;

        // 1. Calculate total size accurately
        int size = sourceSettings.Count * 2;
        int headerSize = x1 ? 1 : 2;
        for (int i = 0; i < sourceSettings.Count; i++)
        {
            if (shared[i] == -1)
                size += headerSize + (sourceSettings[i].Frames.Count * 3) + 3; // Header + Frames + Loop Cmd
        }

        byte[] data = new byte[size];
        int currentDataPos = sourceSettings.Count * 2; // Start writing data after pointer table

        for (int i = 0; i < sourceSettings.Count; i++)
        {
            // Write the pointer to the Table (Point to the Header)
            if (shared[i] == -1)
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), (ushort)(currentDataPos + baseCpu));
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(shared[i] * 2)));
                continue;
            }

            // 2. Write Header
            if (x1)
            {
                data[currentDataPos] = sourceSettings[i].ColorIndex;
                currentDataPos += 1;
            }
            else
            {
                data[currentDataPos] = (byte)(sourceSettings[i].ColorIndex * 2);
                data[currentDataPos + 1] = sourceSettings[i].ColorCount;
                currentDataPos += 2;
            }

            // 3. Write Frames
            for (int f = 0; f < sourceSettings[i].Frames.Count; f++)
            {
                data[currentDataPos] = sourceSettings[i].Frames[f].Timer;
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(currentDataPos + 1), sourceSettings[i].Frames[f].ColorPointer);
                currentDataPos += 3;
            }

            // 4. Write Loop Command
            data[currentDataPos] = 0; // Timer = 0 (Loop Signal)

            short loopValue = (short)(sourceSettings[i].LoopIndex * 3 - 1);
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(currentDataPos + 1), loopValue);
            currentDataPos += 3;
        }

        return data;
    }
    public static byte[] CreateAnimeBoundsData(List<PaletteAnime> sourceSettings)
    {
        bool x1 = Const.Id == Const.GameId.MegaManX;

        int size;

        if (x1)
            size = sourceSettings.Count * 2 * 2; // 2 sides, 2 bytes each.
        else
            size = sourceSettings.Count * 4 * 2; // 4 sides, 2 bytes each.
        
        byte [] data = new byte[size];

        if (x1)
        {
            int rightSideStart = sourceSettings.Count * 2;

            for (int i = 0; i < sourceSettings.Count; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), sourceSettings[i].LeftSide);
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(rightSideStart + i * 2), sourceSettings[i].RightSide);
            }
        }
        else
        {
            int destOffset = 0;
            for (int i = 0; i < sourceSettings.Count; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(destOffset), sourceSettings[i].LeftSide);
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(destOffset + 2), sourceSettings[i].RightSide);
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(destOffset + 4), sourceSettings[i].TopSide);
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(destOffset + 6), sourceSettings[i].BottomSide);
                destOffset += 4 * 2;
            }
        }
        return data;
    }
    private void SetAnimeIntValues()
    {
        int id = animeInt.Value;

        colorIndexInt.Value = PaletteAnimes[id].ColorIndex;
        colorCountInt.Value = PaletteAnimes[id].ColorCount;

        loopInt.Value = PaletteAnimes[id].LoopIndex;
        leftSideInt.Value = PaletteAnimes[id].LeftSide;
        rightSideInt.Value = PaletteAnimes[id].RightSide;
        bottomSideInt.Value = PaletteAnimes[id].BottomSide;
        topSideInt.Value = PaletteAnimes[id].TopSide;
        frameInt.Maximum = PaletteAnimes[id].Frames.Count - 1;
    }
    private void SetFrameIntValues()
    {
        if (PaletteAnimes[animeInt.Value].Frames.Count == 0)
        {
            timerInt.IsEnabled = false;
            colorPointerInt.IsEnabled = false;
            return;
        }
        int id = frameInt.Value;
        timerInt.Value = PaletteAnimes[animeInt.Value].Frames[id].Timer;
        colorPointerInt.Value = PaletteAnimes[animeInt.Value].Frames[id].ColorPointer;

        timerInt.IsEnabled = true;
        colorPointerInt.IsEnabled = true;
    }
    public void ApplyPaletteFrame(PaletteAnime anime, PaletteFrame frame)
    {
        int srcOffset = SNES.CpuToOffset(frame.ColorPointer, Const.PaletteAnimeColorBank);
        int dstIndex = anime.ColorIndex;

        for (int i = 0; i < anime.ColorCount; i++)
        {
            ushort color = BinaryPrimitives.ReadUInt16LittleEndian(SNES.rom.AsSpan(srcOffset));
            byte R = (byte)(color % 32 * 8);
            byte G = (byte)(color / 32 % 32 * 8);
            byte B = (byte)(color / 1024 % 32 * 8);

            if (dstIndex < 0x80) // Only apply the color if it's within the palette's range.
                Level.Palette[dstIndex] = (uint)(0xFF000000 | (R << 16) | (G << 8) | B);

            srcOffset += 2;
            dstIndex++;
        }
        MainWindow.window.layoutE.DrawLayout();
        MainWindow.window.layoutE.DrawScreen();
        MainWindow.window.screenE.DrawScreen();
        MainWindow.window.screenE.DrawTiles();
        MainWindow.window.screenE.DrawTile();
        MainWindow.window.tile32E.DrawTiles();
        MainWindow.window.tile32E.Draw16xTiles();
        MainWindow.window.tile32E.DrawTile();
        MainWindow.window.tile16E.Draw16xTiles();
        MainWindow.window.tile16E.DrawVramTiles();
        MainWindow.window.tile16E.DrawTile();
        MainWindow.window.enemyE.DrawLayout();
        MainWindow.window.paletteE.DrawPalette();
        MainWindow.window.paletteE.DrawVramTiles();
    }
    #endregion Methods

    #region Events
    private void animeInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;

        enableAnimeCheck.IsChecked = false;
        MainWindow.window.ResetPaletteAnime();
        frameInt.Value = 0;
        SetAnimeIntValues();
        SetFrameIntValues();
    }
    private void colorIndexInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;
    }
    private void colorCountInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;
    }
    private void frameInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;
        SetFrameIntValues();
    }
    private void colorPointerInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;
    }
    private void loopInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;
    }
    private void leftSideInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;
    }
    private void rightSideInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;
    }
    private void topSideInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;
    }
    private void bottomSideInt_ValueChanged(object? sender, int e)
    {
        if (SNES.rom == null)
            return;
    }
    #endregion Events
}