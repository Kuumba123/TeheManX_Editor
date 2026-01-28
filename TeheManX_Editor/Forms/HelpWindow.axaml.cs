using Avalonia.Controls;

namespace TeheManX_Editor.Forms;

public partial class HelpWindow : Window
{
    static readonly string[] messages =
    {
            //0
            "Using the editor is pretty straight forward. " +
            "just click open to open your MegaMan X1/X2/X3 SFC Rom!\n" +
            "The japanese version of the games also work but most of the testing was done on the north american versions of the games (and 1.0 speficly for X1). " +
            "There are also some general purpose tools for the games witch you can acsess by clicking Tools " +
            "(keep in mind nothing in the tools tab is connected to the rest of the editor)",
            //1
            "This is where you can edit what screens go where (20hex width 20hex height). " +
            "You can move around by using W A S D . " +
            "Right Click = Copy , Left Click = Paste and " +
            "Hold Shift + Right Click = Selecting the Clicked screen in the Screen Tab. " +
            "Lastly if you press the Delete Key (Num Pad) it will delete the selected screen!",
            //2
            "This where you can edit what tiles make up a screen , Left Click = Paste , Right Click = Copy. " +
            "Normally each screen is made up of 32x32 tiles but if you click 16x16 mode button it will allow you to edit the screens via 16x16 tiles and  it " +
            "puts the editor in this sort locked down state.\n" +
            "Editing in 16x16 mode is much easier than in 32x32 tile mode , just keep in mind each stage has a limit to how many 32x32 tiles you can create.",
            //3
            "This tab is basicly incomplete but I will explain what is finished. " +
            "You can move around by using W A S D just like in the layout tab or via mouse middle button. " +
            "Holding shift and then moving will make you move by pixels instead of 256x256 screens and you can " +
            "zoom in and out via SHIFT + + and SHIFT + -",
            //4
            "This project window has some more advanced tweeking that most people should not really be messing around with (unless you know what your doing)." +
            "The one features basicly everyone would want to use is the 4MB expansion , it will expand the ROM file to 4MB and move the stage data in " +
            "the Layout , Screen , 32x32 and 16x16 tabs so that you almost never run into size related errors in those tabs. " +
            "As for the all the options below (the json related checkmarks) this allows you to get around size related limits that every tab " +
            "after the 16x16 tab. It basicly does this by keep track of the data via a json project file and then when you want to export it to the game " +
            "you tell the editor (via the settings in this project window) the ROM offset you want to dump it at. " +
            "However this feature is more for advanced users since there is no pre-included patch (atleast for now...) so far now your gonna have to write " +
            "the code that loads this data your self.",
            //5
            "This is where you can edit the Dr.Light capsule settings (I cant believe I actually had to make this...). " +
            "At the top you will find the stage containing the Hadouken/Shoryuken/Golden-Armor (as well as a revist count for X1). " +
            "Below that you will find the Capsule Stage Specfic Settings. The following properties are Position X , Position Y , " +
            "Camera X , Camera Y , Armor Index (basicly determines witch upgrade you are going to get) , Text Box Id , Upgrade Movement Index and " +
            "some MegaMan X3 exclusive property that I do not know about. As for the armor index I also dont really have a table as to what is what but " +
            "you can always just look at what has already been configured and see what value you need to set to for spefic upgrade. I will " +
            "mention that you normally still need to set that property even for the Hadouken/Shoryuken/Golden-Armor Capsule. " +
            "As for the Camera X & Camera Y these basicly tell the game where to scroll the screen to but if you set the X to 0 it won't bother scrolling at all. " +
            "Lastly MegaMan X2 has some weird Camera spefic code for the Ostrich capsule (assuming my notes are right) you can apply that fix for it by simply clicking " +
            "on the coresponding button.",
            //6
            "By clicking this button you will expand the game to 4MB. After the game is expanded to 4MB the editor will move the level data the data plus " +
            $"the pointers to the new free data. Doing this will allow for max layout length of {Const.ExpandLayoutLength:X} , " +
            $"a max amount of 32x32 tiles of {Const.ExpandMaxTiles32[0]:X}/{Const.ExpandMaxTiles32[1]:X} , " +
            $"a max amount of 16x16 tiles of {Const.ExpandMaxTiles16:X} and lastly " +
            $"the max amount of screens will be {Const.ExpandMaxScreens[0]:X}/{Const.ExpandMaxScreens[1]:X} for X1 or {Const.ExpandMaxScreens2[0]:X}/{Const.ExpandMaxScreens2[1]:X} for X2 & X3.",
            //7
            "Sadly , the 4MB expandsion is not enough to get around the size limitations for the data in every tab after the 16x16 tab. " +
            "This is because the data is controlled via 16 bit pointers rather than 24 bit pointers like in the earlier tabs. " +
            "You can set the location the data will dump the data that uses 16-bit pointers (ex: enemy tab) and use a JSON file to track " +
            "the data so you can still uses the editor. If you want to use the editor to track the data your self but you want to export it your self " +
            "then you can specify an offset of 0."
    };
    public HelpWindow(int msgId)
    {
        InitializeComponent();
        box.Text = messages[msgId];
    }
}