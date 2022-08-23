namespace GraphicIDE;

public static class BrushesAndPens{
    private static readonly float[] dashes = new[] { 5f, 2f };
    public static readonly Color
        listRed         = Color.FromArgb(255, 157, 059, 052),
        redOpaqe        = Color.FromArgb(100, 255, 000, 000),
        blueOpaqe       = Color.FromArgb(100, 075, 180, 245),
        lightGray       = Color.FromArgb(080, 200, 200, 200),
        keyOrange       = Color.FromArgb(255, 245, 190, 080),
        greenOpaqe      = Color.FromArgb(100, 000, 255, 000),
        mathPurple      = Color.FromArgb(255, 164, 128, 207),
        truckColor      = Color.FromArgb(255, 089, 025, 025),
        truckIColor     = Color.FromArgb(255, 137, 024, 026),
        tabBarColor     = Color.FromArgb(255, 100, 100, 100),
        orangeOpaqe     = Color.FromArgb(100, 250, 200, 093);
    public static readonly SolidBrush
        keyOrangeB  = new(keyOrange),
        mathPurpleB = new(mathPurple),
        truckIBrush = new(truckIColor),
        yellowB     = new(Color.Wheat),
        whiteBrush  = new(Color.White),
        blackBrush  = new(Color.Black),
        curserBrush = new(Color.WhiteSmoke),
        smokeWhiteBrush = new(Color.WhiteSmoke),
        redBrush            = new(Color.FromArgb(255, 200, 049, 45)),
        intBrush            = new(Color.FromArgb(255, 207, 255, 182)),
        timeBrush           = new(Color.FromArgb(255, 100, 100, 100)),
        textBrush           = new(Color.FromArgb(255, 160, 160, 160)),
        greenBrush          = new(Color.FromArgb(255, 110, 255, 130)),
        selectBrush         = new(Color.FromArgb(100, 000, 100, 255)),
        stringBrush         = new(Color.FromArgb(255, 255, 204, 116)),
        tabBarBrush         = new(tabBarColor),
        parenthesiesBrush   = new(Color.FromArgb(255, 076, 175, 104));
    public static SolidBrush curTextBrush = textBrush;
    public static readonly Pen
        redDashed       = new(redOpaqe, 5)      { DashPattern = dashes },
        blueDashed      = new(blueOpaqe, 5)     { DashPattern = dashes },
        greenDashed     = new(greenOpaqe, 5)    { DashPattern = dashes },
        orangeDashed    = new(orangeOpaqe, 5)   { DashPattern = dashes },
        yellowP         = new(Color.Wheat, 5),
        redListP        = new(listRed, 5),
        redOpaqeP       = new(redOpaqe, 5),
        blueOpaqeP      = new(blueOpaqe, 5),
        truckBrush      = new(truckColor, 4),
        keyOrangeP      = new(keyOrange, 5),
        greenOpaqeP     = new(greenOpaqe, 5),
        mathPurpleP     = new(mathPurple, 5),
        orangeOpaqeP    = new(orangeOpaqe, 5);
}