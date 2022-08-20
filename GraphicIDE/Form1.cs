using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using IronPython;
using IronPython.Hosting;
using IronPython.Compiler;
using IronPython.Compiler.Ast;

using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.CSharp.RuntimeBinder;

using GraphicIDE.Properties;

using System.Net;

// todo print and exception
// todo make terminal a Window
// todo when changing font size need to change pen sizes as well 
// todo https://stackoverflow.com/questions/1264406/how-do-i-get-the-taskbars-position-and-size
// todo cache some of the textline images


namespace GraphicIDE;

public partial class Form1: Form {
    private static (int line, int col)? selectedLine = null;
    private static int? lastCol = null;
    private static Keys lastPressed;
    private static List<string> linesText = null!;
    private static readonly TextBox textBox = new();
    private static readonly StringFormat stringFormat = new();
    private static Font
        boldFont = null!,
        tabFont = new(FontFamily.GenericMonospace, 10, FontStyle.Bold);
    private static bool iChanged = false;
    private static readonly Graphics nullGraphics = Graphics.FromImage(new Bitmap(1,1));
    private const int LINE_HEIGHT = 30, WM_KEYDOWN = 0x100, TAB_HEIGHT = 25, TAB_WIDTH = 80;
    private static Dictionary<Font, float> fontToPipeSize = new();
    public static int indentW, qWidth, qHeight, upSideDownW, txtHeight;
    private static int screenWidth = 0, screenHeight = 0, prevHeight, prevWidth;
    private static List<Window> windows = new();
    private static (string txt, ConsoleTxtType typ) consoleTxt = ("", ConsoleTxtType.text);
    private static Window console = null!;
    private static string executedTime = "";
    private static bool isConsoleVisible = false;
    private static Button? closeConsoleBtn, openConsoleBtn, errOpenButton;
    private static bool dragging = false, doubleClick = false;
    private static string? errLink = null;
    private static List<(Button btn, Func<(int w, int h), (int x, int y)> calcPos)> buttonsOnScreen = new();

    #region Tabs
    private static readonly List<Button> tabButtons = new();
    private static int tabButtonEnd = 0;
    private static readonly Dictionary<string, Function> nameToFunc = new();
    private static Function curFunc = null!;
    public static Window curWindow = null!;
    #endregion
    #region BrusesAndPens
    static readonly float[] dashes = new[] { 5f, 2f };
    static readonly Color
        listRed         = Color.FromArgb(255, 157, 59, 52),
        redOpaqe        = Color.FromArgb(100, 255, 000, 000),
        blueOpaqe       = Color.FromArgb(100, 75, 180, 245),
        keyOrange       = Color.FromArgb(255, 245, 190, 80),
        greenOpaqe      = Color.FromArgb(100, 000, 255, 000),
        mathPurple      = Color.FromArgb(255, 164, 128, 207),
        tabBarColor     = Color.FromArgb(255, 100, 100, 100),
        orangeOpaqe     = Color.FromArgb(100, 250, 200, 93);
    private static readonly SolidBrush
        keyOrangeB  = new(keyOrange),
        mathPurpleB = new(mathPurple),
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
    static readonly Pen
        redDashed       = new(redOpaqe, 5)      { DashPattern = dashes },
        blueDashed      = new(blueOpaqe, 5)     { DashPattern = dashes },
        greenDashed     = new(greenOpaqe, 5)    { DashPattern = dashes },
        orangeDashed    = new(orangeOpaqe, 5)   { DashPattern = dashes },
        yellowP         = new(Color.Wheat, 5),
        redListP        = new(listRed, 5),
        redOpaqeP       = new(redOpaqe, 5),
        blueOpaqeP      = new(blueOpaqe, 5),
        keyOrangeP      = new(keyOrange, 5),
        greenOpaqeP     = new(greenOpaqe, 5),
        mathPurpleP     = new(mathPurple, 5),
        orangeOpaqeP    = new(orangeOpaqe, 5);
    #endregion
    #region Images
    private static readonly Bitmap
        emptyListImg = new(Resources.emptyList),
        printerImg = new(Resources.printer),
        consoleImg = new(Resources.console),
        searchImg = new(Resources.search),
        rulerImg = new(Resources.ruler),
        truckImg = new(Resources.truck),
        debugImg = new(Resources.bug),
        playImg = new(Resources.play),
        passImg = new(Resources.pass),
        sumImg = new(Resources.sum),
        xImg = new(Resources.red_X);
    #endregion

    #region Start
    public Form1() {
        InitializeComponent();
        this.MinimumSize = new(100, 100);
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.Black;
        this.DoubleBuffered = true;

        textBox.AcceptsReturn = true;
        textBox.AcceptsTab = true;
        textBox.Dock = DockStyle.None;
        textBox.Size = new Size(0, 0);
        /*textBox.ShortcutsEnabled = false;*/

        textBox.Multiline = true;
        textBox.ScrollBars = ScrollBars.Vertical;
        textBox.Text = "()";
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 0;
        textBox.Focus();
        SetTabWidth(textBox, 4);

        ChangeFontSize(15);

        Controls.Add(textBox);
        textBox.TextChanged += new EventHandler(TextBox_TextChanged!);
        textBox.KeyDown += new KeyEventHandler(Form1_KeyDown!);

        stringFormat.SetTabStops(0, new float[] { 4 });

        (prevHeight, prevWidth) = (Height, Width);
        (screenHeight, screenWidth) = (Height - TAB_HEIGHT, Width / 2);
        AddTab("Main", size: (screenWidth, screenHeight), pos: (0, TAB_HEIGHT), isFirst: true);
        AddTab("Main2", size: (screenWidth, screenHeight), pos: (screenWidth, TAB_HEIGHT));
        curWindow = windows[1];

        AddRunBtn();
        AddDebugBtn();
        AddTabBtn();

        AddConsole();
        
        DrawTextScreen();
        Invalidate();
        FocusTB();
        
        void AddRunBtn() {
            int gap = 5;
            Button run = new(){
                BackColor = tabBarColor,
                Size = new(TAB_HEIGHT, TAB_HEIGHT),
                BackgroundImageLayout = ImageLayout.None,
                FlatStyle = FlatStyle.Flat,
            };
            run.FlatAppearance.BorderSize = 0;
            run.FlatAppearance.BorderColor = Color.White;
            run.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 200, 200, 200);
            Bitmap b = new(run.Size.Width, run.Size.Height);
            using(var g = Graphics.FromImage(b)) {
                Bitmap scaled = new(playImg, run.Size.Width - gap * 2, run.Size.Height - gap * 2);
                g.DrawImage(scaled, gap, gap);
            }
            run.BackgroundImage = b;
            run.Location = new(Width - 2 * run.Size.Width, 0);
            run.Click += new EventHandler(ExecuteBtn!);
            Controls.Add(run);
            buttonsOnScreen.Add((run, (size) => (size.w - 2 * run.Size.Width, 0)));
        }
        void AddDebugBtn() {
            Button run = new(){
                BackColor = tabBarColor,
                Size = new(TAB_HEIGHT, TAB_HEIGHT),
                BackgroundImageLayout = ImageLayout.None,
                FlatStyle = FlatStyle.Flat,
            };
            run.FlatAppearance.BorderSize = 0;
            run.FlatAppearance.BorderColor = Color.White;
            run.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 200, 200, 200);
            Bitmap b = new(run.Size.Width, run.Size.Height);
            using(var g = Graphics.FromImage(b)) {
                Bitmap scaled = new(debugImg, run.Size.Width, run.Size.Height);
                g.DrawImage(scaled, 0, 0);
            }
            run.BackgroundImage = b;
            run.Location = new(Width - 3 * run.Size.Width - 10, 0);
            run.Click += new EventHandler(ExecuteBtn!);
            Controls.Add(run);
            buttonsOnScreen.Add((run, (size) => (size.w - 3 * run.Size.Width - 10, 0)));
        }
        void AddTabBtn() {
            Button run = new(){
                BackColor = tabBarColor,
                Size = new(TAB_HEIGHT, TAB_HEIGHT),
                BackgroundImageLayout = ImageLayout.None,
                FlatStyle = FlatStyle.Flat,
            };
            run.FlatAppearance.BorderSize = 0;
            run.FlatAppearance.BorderColor = Color.White;
            run.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 200, 200, 200);
            Bitmap b = new(run.Size.Width, run.Size.Height);
            using(var g = Graphics.FromImage(b)) {
                Bitmap scaled = new(xImg, run.Size.Width, run.Size.Height);
                g.DrawImage(scaled, 0, 0);
            }
            run.BackgroundImage = b;
            run.Location = new(Width - 4 * run.Size.Width - 20, 0);
            run.Click += new EventHandler(AddTabEvent!);
            Controls.Add(run);
            buttonsOnScreen.Add((run, (size) => (size.w - 4 * run.Size.Width - 20, 0)));
        }
        void AddConsole() {
            int consolePos = Height - (Height / 4);
            Bitmap img = new(Width, Height - consolePos);
            console = new Window(new Function() { DisplayImage = img }) {
                Pos = (0, consolePos),
                Size = (Width, Height - consolePos),
            };
        }
    }
    async void FocusTB(){
        await Task.Delay(100);
        textBox.Focus();
        CtrlTab();
    }
    #endregion

    #region AST
    private BM_Middle? MakeImg(dynamic ast) {
        try {
            return ((Func<BM_Middle?>)(ast.NodeName switch {
                "PythonAst" => () => MainModule(ast),
                "SuiteStatement" => () => SuiteStatement(ast),
                "ExpressionStatement" => () => MakeImg(ast.Expression),
                "PrintStatement" => () => PrintStatement(ast),
                "ParenthesisExpression" => () => ParenthesisExpression(ast),
                "literal" => () => Literal(ast),
                "TupleExpression" => () => TupleExpression(ast),
                "NameExpression" => () => MakeTxtBM(ast.Name.ToString()),
                "AssignmentStatement" => () => AssignmentStatement(ast),
                "operator" => () => Operator(ast),
                "function call" => () => FunctionCall(ast),
                "comparison" => () => Comparison(ast),
                "UnaryExpression" => () => UnaryExpression(ast),
                "IfStatement" => () => IfExpression(ast),
                "WhileStatement" => () => WhileStatement(ast),
                "EmptyStatement" => () => EmptyStatement(),
                "ListExpression" => () => ListExpression(ast),
                "AndExpression" => () => AndExpression(ast),
                "OrExpression" => () => OrExpression(ast),
                "ImportStatement" => () => ImportStatement(ast),
                "AugmentedAssignStatement" => () => AugmentedAssignStatement(ast),
                _ => () => null
            }))();
        } catch(Exception) { 
            return null;
        }
    }
    // TODO 
    private BM_Middle ImportStatement(dynamic ast){
        string name = ast.Names[0].Names[0];
        int width = MeasureWidth(name, boldFont);
        int gap = 7;
        Bitmap truck = new(truckImg, txtHeight + gap * 2, txtHeight + gap * 2);
        Bitmap res = new(gap * 2 + width + truck.Width, truck.Height);
        using(var g = Graphics.FromImage(res)){
            g.FillRectangle(whiteBrush, 0, 0, res.Width, res.Height);
            g.DrawString(name, boldFont, blackBrush, gap, gap);
            g.DrawImage(truck, res.Width - truck.Width, 0);
        }
        return new(res, res.Height / 2);

    }
    private BM_Middle AndExpression(dynamic ast){
        var l = MakeImg(ast.Left);
        var r = MakeImg(ast.Right);
        int height = l.Img.Height + r.Img.Height + 5;
        int andWidth = MeasureWidth("and", boldFont); 
        int width = Max(l.Img.Width, r.Img.Width) + andWidth;
        Bitmap bm = new(width + 20, height + 20);
        using(var g = Graphics.FromImage(bm)){
            g.DrawImage(l.Img, andWidth + 10, 10);
            g.DrawImage(r.Img, andWidth + 10, l.Img.Height + 20);
            int andPosY = (int)(l.Img.Height - txtHeight / 2);
            g.DrawLine(yellowP, 2, l.Img.Height + 15, 10, l.Img.Height + 15);
            g.DrawLine(yellowP, 10 + andWidth, l.Img.Height + 15, bm.Width - 2, l.Img.Height + 15);
            
            g.DrawString("and", boldFont, yellowB, 10, andPosY + 15);
            // the twos are brush size / 2
            g.DrawLine(yellowP, 2, 2, 2, bm.Height - 2);
            g.DrawLine(yellowP, 2, 2, bm.Width - 2, 2);
            g.DrawLine(yellowP, bm.Width - 2, bm.Height - 2, bm.Width - 2, 2);
            g.DrawLine(yellowP, bm.Width - 2, bm.Height - 2, 2, bm.Height - 2);
        }
        return new(bm, bm.Height / 2);
    }
    private BM_Middle OrExpression(dynamic ast){
        var l = MakeImg(ast.Left);
        var r = MakeImg(ast.Right);
        int height = l.Img.Height + r.Img.Height + 5;
        int orWidth = MeasureWidth("or", boldFont); 
        int width = Max(l.Img.Width, r.Img.Width) + orWidth;
        Bitmap bm = new(width + 20, height + 20);
        using(var g = Graphics.FromImage(bm)){
            g.DrawImage(l.Img, orWidth + 10, 10);
            g.DrawImage(r.Img, orWidth + 10, l.Img.Height + 20);
            int andPosY = (int)(l.Img.Height - txtHeight / 2);
            g.DrawLine(yellowP, 2, l.Img.Height + 15, 10, l.Img.Height + 15);
            g.DrawLine(yellowP, 10 + orWidth, l.Img.Height + 15, bm.Width - 2, l.Img.Height + 15);
            g.DrawString("or", boldFont, yellowB, 10, andPosY + 15);
            // the twos are brush size / 2
            g.DrawLine(yellowP, 2, 2, 2, bm.Height - 2);
            g.DrawLine(yellowP, 2, 2, bm.Width - 2, 2);
            g.DrawLine(yellowP, bm.Width - 2, bm.Height - 2, bm.Width - 2, 2);
            g.DrawLine(yellowP, bm.Width - 2, bm.Height - 2, 2, bm.Height - 2);
        }
        return new(bm, bm.Height / 2);
    }
    // private BM_Middle AndExpression(dynamic ast){
    //     var l = MakeImg(ast.Left);
    //     var r = MakeImg(ast.Right);
    //     int height = Max(l.Middle, r.Middle) + Max(l.Img.Height - l.Middle, r.Img.Height - r.Middle);
    //     int andWidth = MeasureWidth(" and ", boldFont); 
    //     int width = l.Img.Width + r.Img.Width + andWidth;
    //     Bitmap bm = new(width, height);
    //     using(var g = Graphics.FromImage(bm)){
    //         g.DrawImage(l.Img, 0, bm.Height / 2 - l.Middle);
    //         g.DrawImage(r.Img, l.Img.Width + andWidth, (int)(bm.Height / 2 - r.Middle));
    //         g.DrawString(" and ", boldFont, mathPurpleB, l.Img.Width, (int)(bm.Height / 2 - txtHeight / 2));
    //     }
    //     return new(bm, bm.Height / 2);
    // }
    // private BM_Middle OrExpression(dynamic ast){
    //     var l = MakeImg(ast.Left);
    //     var r = MakeImg(ast.Right);
    //     int height = Max(l.Middle, r.Middle) + Max(l.Img.Height - l.Middle, r.Img.Height - r.Middle);
    //     int orWidth = MeasureWidth(" or ", boldFont); 
    //     int width = l.Img.Width + r.Img.Width + orWidth;
    //     Bitmap bm = new(width, height);
    //     using(var g = Graphics.FromImage(bm)){
    //         g.DrawImage(l.Img, 0, bm.Height / 2 - l.Middle);
    //         g.DrawImage(r.Img, l.Img.Width + orWidth, (int)(bm.Height / 2 - r.Middle));
    //         g.DrawString(" or ", boldFont, mathPurpleB, l.Img.Width, (int)(bm.Height / 2 - txtHeight / 2));
    //     }
    //     return new(bm, bm.Height / 2);
    // }
    
    private static BM_Middle? emptyListScaled = null;
    private BM_Middle ListExpression(dynamic ast) {
        if(ast.Items.Length == 0) {
            if(emptyListScaled is BM_Middle bm && bm.Img.Height == txtHeight + 15) {
                return bm;
            }
            Bitmap emptyList = new(emptyListImg, (int)(emptyListImg.Width / (emptyListImg.Height / (txtHeight + 15))), txtHeight + 15);
            Bitmap padded = new(emptyList.Width + 5, emptyList.Height);
            using(var pg = Graphics.FromImage(padded)) {
                pg.DrawImage(emptyList, 5, 0);
            }
            emptyListScaled = new(padded, (int)(padded.Height / 2));
            return emptyListScaled!;
        }
        int lineLen = 5;
        int gap = 5;
        List<BM_Middle> elements = new();
        var (width, heightT, heightB) = (lineLen + gap, 0, 0);
        foreach(var item in ast.Items) {
            var img = MakeImg(item);
            var middle = img.Middle;
            elements.Add(img);
            width += lineLen + img.Img.Width + 2 * gap;
            heightT = Max(heightT, middle);
            heightB = Max(heightB, img.Img.Height - middle);
        }
        Bitmap res = new(width, heightB + heightT + 20);
        using(var g = Graphics.FromImage(res)) {
            var end = 2;
            foreach(var (img, _) in elements) {
                end += gap;
                g.DrawLine(redListP, end, 5, end, res.Height - 5);
                end += lineLen;
                g.DrawImage(img, end, (int)(res.Height / 2 - img.Height / 2));
                end += img.Width + gap;
            }
            end += gap;
            g.DrawLine(redListP, end, 5, end, res.Height - 5);
            g.DrawLine(redListP, 5, 5, res.Width, 5);
            g.DrawLine(redListP, 5, res.Height - 5, res.Width, res.Height - 5);
        }
        return new(res, (int)(res.Height / 2));

    }
    private static BM_Middle? passPic = null;
    private static BM_Middle EmptyStatement() {
        if(passPic is BM_Middle bm && bm.Img.Height == txtHeight) {   return bm; }
        Bitmap img = new(passImg, (int)(passImg.Width / (passImg.Height / txtHeight)), txtHeight);
        passPic = new(img, (int)(img.Height / 2));
        return passPic!;
    }
    
    // todo: while / until / forever
    private BM_Middle WhileStatement(dynamic ast) {
        var condition = MakeImg(ast.Test).Img;
        var body = MakeImg(ast.Body).Img;
        Font bigFont = new(FontFamily.GenericMonospace, 30, FontStyle.Bold);
        var infWidth = MeasureWidth("∞", bigFont);
        Bitmap res = new(
                width: Max(condition.Width + infWidth, body.Width + indentW),
                height: condition.Height + body.Height + 14
            );
        using(var g = Graphics.FromImage(res)) {
            g.DrawString(
                "∞", bigFont, keyOrangeB, 0,
                (int)(condition.Height / 2 - qHeight / 2) - 10
            );
            g.DrawImage(condition, infWidth, 0);
            g.DrawLine(blueOpaqeP, 1, 0, 1, res.Height - 5);
            g.DrawImage(body, indentW, condition.Height);
            g.DrawLine(blueDashed, 4, 1, res.Width, 1);
            g.DrawLine(blueDashed, 4, res.Height - 8, res.Width, res.Height - 8);
        }
        return new(res, (int)(res.Height/2));
    }
    private BM_Middle IfExpression(dynamic ast) {
        static Graphics JoinIfAndElse(Pen lastColor, ref Bitmap res, Bitmap img) {
            var (prevH, prevW) = (res.Height, res.Width);
            var prevImg = res;
            res = new(Max(res.Width, img.Width), res.Height + img.Height);
            var resG = Graphics.FromImage(res);
            resG.DrawImage(prevImg, 0, 0);
            resG.DrawLine(lastColor, 4, prevH + 1, prevW, prevH + 1);
            resG.DrawImage(img, 0, prevH);
            return resG;
        }

        static Bitmap MakeIfOrElif(Bitmap condition, Bitmap body, Pen pen) {
            Bitmap res = new(
                width: Max(condition.Width + qWidth, body.Width + indentW),
                height: condition.Height + body.Height + 5
            );
            var g = Graphics.FromImage(res);
            g.DrawString("¿", boldFont, keyOrangeB, 0, (int)(condition.Height / 2 - qHeight / 2));
            g.DrawImage(condition, upSideDownW, 0);
            g.DrawString(
                "?", boldFont, keyOrangeB, 
                condition.Width + upSideDownW, 
                (int)(condition.Height / 2 - qHeight / 2)
            );
            g.DrawLine(pen, 1, 0, 1, res.Height);
            g.DrawImage(body, indentW, condition.Height);
            return res;
        }
        
        var mainIf = ast.Tests[0];
        Pen lastColor = greenDashed;
        var ifCond = MakeImg(mainIf.Test).Img;
        var ifBody = MakeImg(mainIf.Body).Img;
        var res = MakeIfOrElif(ifCond, ifBody, greenOpaqeP);
        var resG = Graphics.FromImage(res);
        resG.DrawLine(lastColor, 4, 1, res.Width, 1);

        if(ast.Tests.Length > 1) {
            lastColor = orangeDashed;
            for(int i = 1; i < ast.Tests.Length; i++) {
                var item = ast.Tests[i];
                var cond = MakeImg(item.Test).Img;
                var body = MakeImg(item.Body).Img;
                var img = MakeIfOrElif(cond, body, orangeOpaqeP);
                resG = JoinIfAndElse(lastColor, ref res, img);
            }
        }

        if(ast.ElseStatement is not null) {
            var elseBody = MakeImg(ast.ElseStatement).Img;
            Bitmap elseImg = new(
                width: elseBody.Width + indentW,
                height: elseBody.Height + 7
            );
            using(var eg = Graphics.FromImage(elseImg)) {
                eg.DrawLine(redOpaqeP, 1, 0, 1, elseImg.Height);
                eg.DrawImage(elseBody, indentW, 0);
            }
            resG = JoinIfAndElse(lastColor, ref res, elseImg);
            lastColor = redDashed;
        }
        resG.DrawLine(lastColor, 4, res.Height - 3, res.Width, res.Height - 3);
        Bitmap addPad = new(res.Width, res.Height + 5);
        using(var g = Graphics.FromImage(addPad)) {
            g.DrawImage(res, 0, 0);
        }
        return new(addPad, (int)(res.Height/2));
    }
    private BM_Middle? FunctionCall(dynamic ast) {
        if(ast.Target.Name == "sqrt" && ast.Target.Target.Name == "math") {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Img;
            Bitmap res = new(
                width: inside.Width + 30, 
                height: inside.Height + 15
            );
            using(var g = Graphics.FromImage(res)) {
                g.DrawImage(inside, 20, 15);
                g.DrawLine(mathPurpleP, 15, 5, res.Width, 5);
                g.DrawLine(mathPurpleP, 15, 5, 15, res.Height);
                g.DrawLine(mathPurpleP, 5, res.Height - 15, 15, res.Height);
            }
            return new(res, (int)(res.Height / 2));
        }
        else if(ast.Target.Name == "sum") {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Img;
            Bitmap sum = new(sumImg, sumImg.Width / (sumImg.Height / inside.Height), inside.Height);
            Bitmap res = new(
                width: inside.Width + sum.Width + 10,
                height: inside.Height
            );
            using(var g = Graphics.FromImage(res)) {
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 255, 255, 255)), 0, 0, res.Width, res.Height);
                g.DrawImage(sum, 5, 0);
                g.DrawImage(inside, sum.Width + 5, 0);
            }
            return new(res, (int)(res.Height / 2));
        }
        else if(ast.Target.Name == "len") {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Img;
            Bitmap ruler = new(rulerImg, rulerImg.Width / (rulerImg.Height / inside.Height), inside.Height);
            Bitmap res = new(
                width: inside.Width + ruler.Width + 10,
                height: inside.Height
            );
            using(var g = Graphics.FromImage(res)) {
                g.FillRectangle(new SolidBrush(Color.FromArgb(100, 215, 206, 180)), 5 + ruler.Width, 0, res.Width, res.Height);
                g.DrawImage(ruler, 5, 0);
                g.DrawImage(inside, ruler.Width + 5, 0);
            }
            return new(res, (int)(res.Height / 2));
        }
        else if(ast.Target.Name == "abs") {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Img;
            Bitmap res = new(
                width: inside.Width + 30,
                height: inside.Height + 10
            );
            using(var g = Graphics.FromImage(res)) {
                g.DrawLine(mathPurpleP, 10, 0, 10, res.Height);
                g.DrawLine(mathPurpleP, res.Width - 5, 0, res.Width - 5, res.Height);
                g.DrawImage(inside, 15, 5);
            }
            return new(res, (int)(res.Height / 2));
        }
        return null;
    }
    private BM_Middle UnaryExpression(dynamic ast) {
        var op = PythonOperatorToString(ast.Op);
        
        int gap = 5;
        var opWidth = MeasureWidth(op, boldFont);
        var opHeight = MeasureHeight(op, boldFont);
        var img = MakeImg(ast.Expression);
        (Bitmap bmap, int middle) = (img.Img, img.Middle);
        var top = middle;
        Bitmap res = new(
            width: bmap.Width + opWidth + gap,
            height: bmap.Height
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawString(op, boldFont, mathPurpleB, x: gap, y: (int)(middle - opHeight / 2));
            g.DrawImage(bmap, x: opWidth + gap, y: (int)(middle - top));
        }
        return new(res, middle);
    }
    private BM_Middle Comparison(dynamic ast) {
        var op = PythonOperatorToString(ast.Operator);
        
        int gap = 5;
        var opWidth = MeasureWidth(op, boldFont) + gap * 2;
        var opHeight = MeasureHeight(op, boldFont);
        var l = MakeImg(ast.Left);
        (Bitmap left, int lmiddle) = (l.Img, l.Middle);
        var r = MakeImg(ast.Right);
        (Bitmap right, int rmiddle) = (r.Img, r.Middle);
        switch(op){
            case "**":
                return Power(l, r);
            case "/":
                return Divide(right, left);
            case "//":
                return FloorDivide(right, left);
        }
        var rBottom = right.Height - rmiddle;
        var rTop = rmiddle;
        var lBottom = left.Height - lmiddle;
        var lTop = lmiddle;
        Bitmap res = new(
            width: left.Width + right.Width + opWidth,
            height: Max(lTop, rTop) + Max(lBottom, rBottom)
        );
        var resMiddle = Max(lTop, rTop);
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(left, 0, y: (int)(resMiddle - lTop));
            g.DrawString(op, boldFont, mathPurpleB, x: left.Width + gap, y: (int)(resMiddle - opHeight / 2));
            g.DrawImage(right, x: left.Width + opWidth, y: (int)(resMiddle - rTop));
        }
        return new(res, resMiddle);
    }
    private BM_Middle Operator(dynamic ast) {
        #region OperatorSwitch
        var op = PythonOperatorToString(ast.Operator);
        #endregion
        
        int gap = 5;
        var opWidth = MeasureWidth(op, boldFont) + gap * 2;
        var opHeight = MeasureHeight(op, boldFont);
        var l = MakeImg(ast.Left);
        (Bitmap left, int lmiddle) = (l.Img, l.Middle);
        var r = MakeImg(ast.Right);
        (Bitmap right, int rmiddle) = (r.Img, r.Middle);
        switch(op){
            case "**":
                return Power(l, r);
            case "/":
                return Divide(right, left);
            case "//":
                return FloorDivide(right, left);
        }
        var rBottom = right.Height - rmiddle;
        var rTop = rmiddle;
        var lBottom = left.Height - lmiddle;
        var lTop = lmiddle;
        Bitmap res = new(
            width: left.Width + right.Width + opWidth,
            height: Max(lTop, rTop) + Max(lBottom, rBottom)
        );
        var resMiddle = Max(lTop, rTop);
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(left, 0, y: (int)(resMiddle - lTop));
            g.DrawString(op, boldFont, mathPurpleB, x: left.Width + gap, y: (int)(resMiddle - opHeight / 2));
            g.DrawImage(right, x: left.Width + opWidth, y: (int)(resMiddle - rTop));
        }
        return new(res, resMiddle);
    }
    private static BM_Middle Power(BM_Middle bottom, BM_Middle top) {
        int topGap = top.Img.Height - top.Middle;
        Bitmap res = new(
            width: top.Img.Width + bottom.Img.Width - 5,
            height: Max(topGap, bottom.Img.Height) + top.Middle
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(bottom.Img, 0, y: res.Height - bottom.Img.Height);
            g.DrawImage(top.Img, x: bottom.Img.Width - 5, y: 0);
        }
        return new(res, res.Height - (int)(bottom.Img.Height / 2));
    }
    private static BM_Middle FloorDivide(Bitmap bottom, Bitmap top) {
        Bitmap res = new(
            width: Max(top.Width, bottom.Width),
            height: top.Height + bottom.Height + 22
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(top, x: (int)((res.Width - top.Width) / 2), y: 0);
            g.DrawLine(mathPurpleP, 5, top.Height + 5, res.Width, top.Height + 5);
            g.DrawLine(mathPurpleP, 5, top.Height + 11, res.Width, top.Height + 11);
            g.DrawImage(bottom, x: (int)((res.Width - bottom.Width) / 2), y: top.Height + 22);
        }
        return new(res, top.Height + 11);
    }
    private static BM_Middle Divide(Bitmap bottom, Bitmap top) {
        Bitmap res = new(
            width: Max(top.Width, bottom.Width),
            height: top.Height + bottom.Height + 15
        );
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(top, x: (int)((res.Width - top.Width) / 2), y: 0);
            g.DrawLine(mathPurpleP, 5, top.Height + 5, res.Width, top.Height + 5);
            g.DrawImage(bottom, x: (int)((res.Width - bottom.Width) / 2), y: top.Height + 15);
        }
        return new(res, top.Height + 7);
    }
    private BM_Middle Literal(dynamic ast) =>
        MakeTxtBM(ast.Type.Name.Equals("String") ? $"'{ast.Value}'": ast.Value.ToString(), 
            ast.Type.Name switch {
                "String" => stringBrush,
                "Int32" or "Double" or "BigInteger" => intBrush,
                _ => textBrush
            }
        );
    private BM_Middle SuiteStatement(dynamic ast) {
        List<Bitmap> resses = new();
        var (height, width) = (0, 0);
        foreach(var statement in ast.Statements) {
            try {
                resses.Add(MakeImg(statement).Img);
            } catch(Exception) {
                // put just the text in this line
                throw;
            }
            height += resses[^1].Size.Height;
            width = Max(width, resses[^1].Size.Width);
        }
        var end = 20;
        var res = new Bitmap(width, height + end);
        using(var g = Graphics.FromImage(res)) {
            foreach(var item in resses) {
                g.DrawImage(item, 0, end);
                end += item.Height;
            }
        }
        return new(res, (int)(res.Height / 2));
    }
    private BM_Middle MainModule(dynamic ast) {
        List<Bitmap> resses = new();
        var (height, width) = (0, 0);
        var statements = ast.Body.Statements;
        for(int j = 0; j < statements.Length; j++) {
            var statement = statements[j];
            (int line, int col) startI, endI;
            if(j == 0) {
                startI = (0, 0);
            } else {
                var start = statements[j - 1].End;
                startI = (start.Line - 1, start.Column - 1);
            }
            if(j == statements.Length - 1) {
                endI = (linesText.Count - 1, linesText[^1].Length);
                for(int i = 1; i <= linesText.Count; i++) {
                    endI = (linesText.Count - i, linesText[^i].Length);
                    if(endI.col != 0) {
                        break;
                    }
                }
            } else {
                var start = statements[j + 1].Start;
                endI = (start.Line - 1, start.Column - 1);
            }
            try {
                var img = MakeImg(statement).Img;
                if(img is null) {
                    throw new Exception();
                }
                resses.Add(img);
            } catch(Exception) {
                StringBuilder sb = new();
                for(int i = startI.line; i <= endI.line; i++) {
                    if(i == startI.line) {
                        if(i == endI.line) {
                            if(startI.col != endI.col) {
                                sb.Append(linesText[i].AsSpan(startI.col, endI.col - startI.col));
                            }
                        } else {
                            sb.Append(linesText[i].AsSpan(startI.col));
                        }
                    } else if (i == endI.line) {
                        if(endI.col != 0) {
                            sb.Append(linesText[i].AsSpan(0, endI.col));
                        }
                    } else {
                        sb.Append(linesText[i]);
                    }
                    if(i != endI.line) {    sb.Append('\n'); }
                }
                var st = sb.ToString();
                var len = MeasureWidth(st, boldFont);
                var lenH = MeasureHeight(st, boldFont);
                Bitmap bm = new(len, lenH);
                using(var bg = Graphics.FromImage(bm)) {
                    bg.DrawString(ReplaceTabs(st), boldFont, textBrush, 0, 0);
                }
                resses.Add(bm);
            }
            height += resses[^1].Size.Height;
            width = Max(width, resses[^1].Size.Width);
        }
        var end = 20;
        var res = new Bitmap(width, height + end);
        using(var g = Graphics.FromImage(res)) {
            foreach(var item in resses) {
                g.DrawImage(item, 0, end);
                end += item.Height;
            }
        }
        return new(res, (int)(res.Height / 2));
    }
    private BM_Middle PrintStatement(dynamic ast) {
        List<Bitmap> resses = new();
        var (width, height) = (0, 0);
        foreach(var statement in ast.Expressions) {
            if(statement.NodeName == "TupleExpression") {
                resses.Add(MakeImg(statement).Img);
            } else {
                resses.Add(MakeImg(statement.Expression).Img);
            }
            width += resses[^1].Width;
            height = Max(height, resses[^1].Height);
        }
        var printer = new Bitmap(printerImg, new Size(printerImg.Width / (printerImg.Height / (height + 20)), height + 20));
        var res = new Bitmap(width + printer.Width + 20, printer.Height);
        using(var g = Graphics.FromImage(res)) {
            g.DrawImage(printer, 0, 0);
            var end = printer.Width;
            g.DrawRectangle(new Pen(Color.White, 5), end, 2.5f, res.Width - printer.Width - 10, res.Height - 5);

            end += 5;
            foreach(var item in resses) {
                g.DrawImage(item, end, 12);
                end += item.Width;
            }
        }
        return new(res, (int)(res.Height / 2));
    }
    private BM_Middle ParenthesisExpression(dynamic ast) {
        Bitmap inside = MakeImg(ast.Expression).Img;
        var width = inside.Width;
        var height = inside.Height;
        var parHeight = height + 10;
        var parWidth = parHeight / 5;
        Bitmap res = new(
            width + 20 + (parWidth + 5) * 2,
            height + 20
        );

        using(var g = Graphics.FromImage(res)) {
            var end = 10;
            g.DrawArc(mathPurpleP, new Rectangle(end, 5, parWidth, parHeight), 90, 180);
            end += parWidth + 5;
            g.DrawImage(inside, end, 10);
            g.DrawArc(mathPurpleP, new Rectangle(
                end + width + 5, 5, parWidth, parHeight
            ), 270, 180);
        }
        return new(res, (int)(res.Height / 2));
    }
    private BM_Middle TupleExpression(dynamic ast) {
        List<Bitmap> resses = new();
        var (width, height) = (0, 0);
        foreach(var statement in ast.Items) {
            resses.Add(MakeImg(statement).Img);
            width += resses[^1].Width;
            height = Max(height, resses[^1].Height);
        }
        int commaLen = MeasureWidth(",", boldFont);
        var res = new Bitmap(width + commaLen * (resses.Count - 1), height);

        using(var g = Graphics.FromImage(res)) {
            bool putComma = false;
            var end = 0;
            foreach(var item in resses) {
                if(putComma) {
                    g.DrawString(",", boldFont, textBrush, end, 0);
                    end += commaLen;
                } else { putComma = true; }
                g.DrawImage(item, end, 0);
                end += item.Width;
            }
        }
        return new(res, (int)(res.Height / 2));
    }
    //todo cleanup
    private BM_Middle AssignmentStatement(dynamic ast) {
        List<Bitmap> assignmentNames = new();
        var (h1, w1) = (0, 0);
        int lineWidth = 5;
        int gap = 5;

        if(ast.Left.Length != 1) {
            throw new Exception("It's not len 1??");
        }
        var leftVal = ast.Left[0];
        if(leftVal.NodeName == "TupleExpression") {
            leftVal = leftVal.Items;
        } else {
            leftVal = new List<dynamic> { leftVal };
        }
        foreach(var item in leftVal) {
            assignmentNames.Add(MakeImg(item).Img);
            if(h1 != 0) { h1 += gap; }
            h1 += assignmentNames[^1].Height;
            w1 = Max(w1, assignmentNames[^1].Width);
        }
        Bitmap valueName = MakeImg(ast.Right).Img;
        var (h2, w2) = (valueName.Height, valueName.Width);

        var height = Max(h1, h2) + lineWidth * 2 + gap * 2;
        var width = w1 + w2 + lineWidth * 3 + gap * 4;
        Bitmap res = new(width, height);

        using(var g = Graphics.FromImage(res)){
            var end = (height - h1) / 2  - lineWidth;
            foreach(var item in assignmentNames) {
                g.DrawImage(
                    item,
                    x: lineWidth + gap,
                    y: end,
                    item.Width, item.Height
                );
                end += item.Height + gap;
            }

            g.DrawImage(
                image: valueName,
                x: w1 + lineWidth * 2 + gap * 3,
                y: (height - h2) / 2 - lineWidth,
                valueName.Width, valueName.Height
            );

            g.DrawRectangle(
                new(Color.WhiteSmoke, lineWidth), 0, 0,
                width: w1 + gap * 2 + lineWidth * 2,
                height: height - lineWidth * 2
            );
            g.DrawRectangle(
                new(Color.WhiteSmoke, lineWidth), 0, 0,
                width: w1 + w2 + gap * 4 + lineWidth * 3,
                height: height - lineWidth * 2
            );
        }
        return new(res, (int)(res.Height / 2));
    }
    private BM_Middle AugmentedAssignStatement(dynamic ast) {
        int lineWidth = 4, gap = 4;
        string op = PythonOperatorToString(ast.Operator);
        int opHeight = MeasureHeight(op, boldFont);
        int opWidth = MeasureWidth(op, boldFont);
        var leftImg = MakeImg(ast.Left).Img;
        var h1 = leftImg.Height;
        var w1 = leftImg.Width;
        
        Bitmap valueName = MakeImg(ast.Right).Img;
        var (h2, w2) = (valueName.Height, valueName.Width);

        var height = Max(h1, h2) + lineWidth * 2 + gap * 2;
        var width = w1 + w2 + lineWidth * 3 + gap * 4 + opWidth;
        Bitmap res = new(width, height);

        using(var g = Graphics.FromImage(res)){
            g.DrawRectangle(
                new Pen(Color.WhiteSmoke, lineWidth), 
                lineWidth/2, lineWidth/2,
                width: (int)(w1 + gap * 2 + lineWidth + opWidth/2),
                height: height - lineWidth
            );

            int opX = (int)(w1 + gap * 2 + lineWidth);
            int opY = (int)(res.Height / 2 - opHeight / 2);
            g.FillRectangle(blackBrush, opX, opY, opWidth, opHeight);
            g.DrawString(op, boldFont, mathPurpleB, opX, opY);

            g.DrawRectangle(
                new Pen(Color.WhiteSmoke, lineWidth), 
                lineWidth/2, lineWidth/2,
                width: res.Width - lineWidth,
                height: res.Height - lineWidth
            );
            g.DrawImage(
                leftImg,
                lineWidth + gap,
                lineWidth + gap,
                leftImg.Width, leftImg.Height
            );

            g.DrawImage(
                image: valueName,
                x: w1 + lineWidth + gap * 3 + opWidth,
                y: lineWidth + gap,
                width: valueName.Width, height: valueName.Height
            );

        }
        return new(res, (int)(res.Height / 2));
    }
    #endregion

    #region Tabs
    private void AddTabEvent(object? sender, EventArgs e){
        MakeNewTab();
    }

    private void AddTab(string name, (int width, int height) size, (int x, int y) pos, bool isFirst=false) {
        Function func = new() {    Name = name };
        if(isFirst) { curFunc = func; }
        nameToFunc.Add(name, func);
        Window window = new(func) { Size = size, Pos = pos };
        curWindow = window;
        windows.Add(window);
        Button btn = new() {
            Name = name,
            Text = name,
            BackColor = Color.LightGray,
            Location = new(tabButtonEnd, 0),
            Size = new(TAB_WIDTH, TAB_HEIGHT),
            Font = tabFont
        };
        func.DisplayImage = new(screenWidth, screenHeight);
        func.Button = btn;
        ChangeTab(btn);
        btn.Click += new EventHandler(ChangeTab!);
        tabButtons.Add(btn);
        Controls.Add(btn);
        tabButtonEnd += btn.Width + 10;
    }
    private void ChangeTab(object sender, EventArgs e) =>
        ChangeTab((Button)sender, select: true);
    private void ChangeTab(Button btn, bool select=false) {
        var func = nameToFunc[btn.Name];
        if(select && windows.Find((x) => x.Function.Equals(func)) is not null) {
            return;
        }

        if(selectedLine is not null) {
            if(!isShiftPressed()) {
                selectedLine = null;
            }
        }
        curFunc.Button.BackColor = Color.LightGray;
        curFunc.CurLine = CursorPos.Line;
        curFunc.CurCol = CursorPos.Col;
        if(!isPic) {    DrawPicScreen(); }
        if(!isPic) {
            try {
                DrawTextScreen(false); Invalidate();
            } catch(Exception) { }
        }
        curFunc.isPic = isPic;
        func.Button.BackColor = Color.WhiteSmoke;
        curFunc = func;
        curWindow.Function = func;
        linesText = func.LinesText;
        CursorPos.ChangeLine(func.CurLine);
        CursorPos.ChangeCol(func.CurCol);
        isPic = func.isPic;
        skipDrawNewScreen = false;
        textBox.Focus();
        
        if(isPic && false) {
            isPic = false; // cuz DrawPicScreen reverses `isPic`
            DrawPicScreen();
        } else {
            DrawTextScreen();
            Invalidate();
        }
    }
    private void MakeNewTab() {
        TextBox textBox = new(){
            Multiline = true,
            Size = new(500, 40),
            Font = boldFont,
        };
        textBox.Location = new(
            (int)(Width / 2 - textBox.Width / 2),
            (int)(Height / 2 - textBox.Height / 2)
        );
        textBox.KeyDown += new KeyEventHandler(EnterNewTab!);
        Controls.Add(textBox);
        textBox.Focus();
    }
    private void EnterNewTab(object sender, KeyEventArgs e) {
        if(e.KeyCode == Keys.Enter) {
            Controls.Remove((TextBox)sender);
            windows = windows.FindAll((x) => x.Pos.x != 0);
            AddTab(((TextBox)sender).Text, size:(screenWidth, screenHeight), pos: (0, TAB_HEIGHT));
        } else if(e.KeyCode == Keys.Escape) {
            Controls.Remove((TextBox)sender);
        }
    }
    #endregion

    #region DrawScreen
    private static bool isPic = false, skipDrawNewScreen = false;
    private void DrawPicScreen() {
        if(isPic) {
            DrawTextScreen();
            Invalidate();
            isPic = false;
        } else {
            try {
                var bm = MakeImg(ToAST());
                if(bm is not null) {
                    #region resize to fit screen
                    if(bm.Img.Height > curWindow.Size.height || bm.Img.Width > curWindow.Size.width){
                        var (newWidth, newHeight) = (bm.Img.Width, bm.Img.Height);
                        if(newWidth > curWindow.Size.width){
                            newWidth = (int)curWindow.Size.width;
                            newHeight = newHeight / (newWidth / newWidth);
                        }
                        if(newHeight > curWindow.Size.height){
                            newHeight = (int)curWindow.Size.height;
                            newWidth = newWidth / (newHeight / newHeight);
                        }
                        bm = new(new(bm.Img, newWidth, newHeight), 0);
                    }
                    #endregion
                    curFunc.DisplayImage = bm.Img;
                    skipDrawNewScreen = true;
                    Invalidate();
                    isPic = true;
                }
            } catch (Exception){}
        }
    }
    private static void DrawTextScreen(bool withCurser = true) {
        if(skipDrawNewScreen) {
            skipDrawNewScreen = false;
            return;
        }
        isPic = false;

        List<Bitmap> bitmaps = new();
        int totalWidth = 0;
        int end = 0;
        for(int i = 0; i < linesText.Count; i++) {
            var lineText = ReplaceTabs(linesText[i]);
            int width = MeasureWidth(lineText, boldFont);
            totalWidth = Max(totalWidth, width);

            Bitmap bm = new(width, txtHeight);
            var g = Graphics.FromImage(bm);
            g.DrawString(lineText, boldFont, textBrush, 0, 0);

            if(i == CursorPos.Line && withCurser) {
                var before = CursorPos.Col == -1 ? "": ReplaceTabs(linesText[i][..(CursorPos.Col + 1)]);
                g.FillRectangle(
                    curserBrush,
                    MeasureWidth(before, boldFont) - 3,
                    5, 1, txtHeight - 10
                );
            }

            if(selectedLine is (int, int) sl && withCurser) {
                if((i < sl.line && i > CursorPos.Line) || (i > sl.line && i < CursorPos.Line)) {
                    g.FillRectangle(
                        selectBrush, 0, 0,
                        MeasureWidth(lineText, boldFont), LINE_HEIGHT
                    );
                } else if(i == sl.line || i == CursorPos.Line) {
                    int cCol = CursorPos.Col, sCol = sl.col;
                    if(i == sl.line) {
                        cCol = i == CursorPos.Line ? CursorPos.Col : (i > CursorPos.Line ? -1 : lineText.Length - 1);
                    } else {
                        sCol = i > sl.line ? -1 : lineText.Length - 1;
                    }
                    var (smaller, bigger) = cCol < sCol ? (cCol, sCol) : (sCol, cCol);
                    var startS = smaller == -1 ? 0 : MeasureWidth(ReplaceTabs(linesText[i][..(smaller + 1)]), boldFont);

                    var endS = MeasureWidth(ReplaceTabs(linesText[i][..Min(linesText[i].Length, bigger + 1)]), boldFont);
                    g.FillRectangle(selectBrush, 0 + startS, 0, endS - startS, LINE_HEIGHT);
                }
            }
            end += bm.Height;
            bitmaps.Add(bm);
        }
        Bitmap newBitMap = new(Min(totalWidth, screenWidth), end);
        var gr = Graphics.FromImage(newBitMap);
        end = 0;
        foreach(var item in bitmaps) {
            gr.DrawImage(item, 0, end);
            end += item.Height;
        }
        curFunc.DisplayImage = newBitMap;
    }
    private static void OpenErrLink(object? sender, EventArgs e) {
        if(errLink is not null) {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {errLink}") { CreateNoWindow = true });
        }
    }
    private static Bitmap screen_bm = new(1, 1);
    private void Form1_Paint(object? sender, PaintEventArgs e) {
        foreach(var item in windows) {
            // make back black
            e.Graphics.FillRectangle(blackBrush, item.Pos.x, item.Pos.y, item.Size.width, item.Size.height);
            //draw the img
            Bitmap bm = new((int)item.Size.width, (int)item.Size.height);
            Graphics.FromImage(bm).DrawImage(item.Function.DisplayImage!, 0, item.Offset);
            e.Graphics.DrawImage(bm, item.Pos.x, item.Pos.y);
            //draw frame
            e.Graphics.DrawRectangle(new(Color.White, 2), item.Pos.x-2, item.Pos.y-2, item.Size.width+2, item.Size.height+2);
        }
        // tab bar
        e.Graphics.FillRectangle(tabBarBrush, 0, 0, Width, TAB_HEIGHT);
        
        if(isConsoleVisible) {
            e.Graphics.FillRectangle(blackBrush, console.Pos.x, console.Pos.y, console.Size.width, console.Size.height);
            e.Graphics.DrawImage(console.Function.DisplayImage!, console.Pos.x, console.Pos.y);
            e.Graphics.FillRectangle(whiteBrush, 0, console.Pos.y - 2, console.Size.width, 2);
        }
    }
    #endregion

    #region Console

    private void ShowConsole() {
        int idx = Controls.IndexOf(openConsoleBtn);
        if(idx != -1) {
            buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(openConsoleBtn!)));
            Controls[idx].Dispose();
        }
        openConsoleBtn = null;

        int consolePos = Height - (Height / 4);
        console.Pos.y = consolePos;
        console.Size.height = Height - consolePos;
        console.Size.width = Width;
        Bitmap img = new(Width - 20, Height - consolePos - 45);
        using(var g = Graphics.FromImage(img)) {
            if(consoleTxt.typ == ConsoleTxtType.text) {
                g.DrawString(consoleTxt.txt, boldFont, textBrush, 0, 5);
                int end = 5 + MeasureHeight(consoleTxt.txt, boldFont);
                int h = MeasureHeight(executedTime, tabFont);
                int w = MeasureWidth(executedTime, tabFont);
                g.DrawString(executedTime, tabFont, timeBrush, img.Width - w, img.Height - h);
                if(errOpenButton is not null) {
                    buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(buttonsOnScreen)));
                    Controls[Controls.IndexOf(errOpenButton)].Dispose();
                    errOpenButton = null;
                }
            } else {
                g.DrawString(consoleTxt.txt, boldFont, redBrush, 0, 5);
                if(errOpenButton is null) {
                    errOpenButton = new() {
                        Size = new(20, 20),
                        Location = new((int)(console.Pos.x + console.Size.width) - 40, (int)console.Pos.y + 35),
                        BackColor = Color.Transparent,
                        BackgroundImage = searchImg,
                        FlatStyle = FlatStyle.Flat,
                        BackgroundImageLayout = ImageLayout.Zoom,
                    };
                    errOpenButton.FlatAppearance.BorderSize = 0;
                    errOpenButton.FlatAppearance.BorderColor = Color.White;
                    errOpenButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 200, 200, 200);
                    errOpenButton.Click += new EventHandler(OpenErrLink!);
                    Controls.Add(errOpenButton);
                    buttonsOnScreen.Add((errOpenButton, (_)=>((int)(console.Pos.x + console.Size.width) - 40, (int)console.Pos.y + 35)));
                }
            }
        }
        console.Function.DisplayImage = img;
        isConsoleVisible = true;

        if(closeConsoleBtn is null) {
            closeConsoleBtn = new() {
                Size = new(20, 20),
                Location = new((int)(console.Pos.x + console.Size.width) - 40, (int)console.Pos.y + 5),
                BackColor = Color.Transparent,
                BackgroundImage = xImg,
                FlatStyle = FlatStyle.Flat,
                BackgroundImageLayout = ImageLayout.Zoom,
            };
            closeConsoleBtn.FlatAppearance.BorderSize = 0;
            closeConsoleBtn.FlatAppearance.BorderColor = Color.White;
            closeConsoleBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 200, 200, 200);
            closeConsoleBtn.Click += new EventHandler(HideConsole!);
            Controls.Add(closeConsoleBtn);
            buttonsOnScreen.Add((closeConsoleBtn, (size)=>((int)(console.Pos.x + console.Size.width) - 40, (int)console.Pos.y + 5)));
        }
        Invalidate();
    }
    private void HideConsole(object? sender, EventArgs e) {
        isConsoleVisible = false;
        int idx = Controls.IndexOf(closeConsoleBtn);
        if(idx != -1) {
            buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(closeConsoleBtn!)));
            Controls[idx].Dispose();
        }
        closeConsoleBtn = null;
        idx = Controls.IndexOf(errOpenButton);
        if(idx != -1) {
            buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(errOpenButton!)));
            Controls[idx].Dispose();
        }
        errOpenButton = null;

        if(openConsoleBtn is null) {
            MakeOpenConsoleBtn();
        }
        Invalidate();
    }
    private void MakeOpenConsoleBtn() {
        openConsoleBtn = new() {
            Size = new(30, 30),
            Location = new(Width - 60, Height - 80),
            BackColor = Color.Transparent,
            BackgroundImage = consoleImg,
            FlatStyle = FlatStyle.Flat,
            BackgroundImageLayout = ImageLayout.Zoom,
        };
        openConsoleBtn.FlatAppearance.BorderSize = 0;
        openConsoleBtn.FlatAppearance.BorderColor = Color.White;
        openConsoleBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 200, 200, 200);
        openConsoleBtn.Click += new EventHandler((object? s, EventArgs e) => ShowConsole());
        Controls.Add(openConsoleBtn);
        buttonsOnScreen.Add((openConsoleBtn, (size) => (size.w - 60, size.h - 80)));
    }
    private void ToggleConsole() {
        if(isConsoleVisible) { HideConsole(null, new()); } else { ShowConsole(); }
    }
    private void RefreshConsole() {
        if(isConsoleVisible) { ShowConsole(); } else { HideConsole(null, new()); }
    }

    #endregion

    #region Python
    private static PythonAst ToAST() {
        StringBuilder theScript = new();
        foreach(var line in linesText) {
            theScript.AppendLine(line);
        }
        var engine = Python.CreateEngine();

        ScriptSource source = engine.CreateScriptSourceFromString(
            theScript.ToString(),
            SourceCodeKind.InteractiveCode
        );

        SourceUnit unit = HostingHelpers.GetSourceUnit(source);
        Parser p = Parser.CreateParser(
            new CompilerContext(unit, new PythonCompilerOptions(), ErrorSink.Default),
            new PythonOptions()
        );

        return p.ParseFile(false);
    }
    private void ExecuteBtn(object from, EventArgs e) {
        Execute();
        textBox.Focus();
    }
    private void Execute() {
        StringBuilder theScript = new(), res = new(), errs = new();
        foreach(var line in linesText) {
            theScript.AppendLine(line);
        }

        var engine = Python.CreateEngine();
        MemoryStream ms = new();
        EventRaisingStreamWriter outputWr = new(ms);
        outputWr.StringWritten += new EventHandler<MyEvtArgs<string>>(sWr_StringWritten!);

        MemoryStream ems = new();
        EventRaisingStreamWriter errOutputWr = new(ems);
        errOutputWr.StringWritten += new EventHandler<MyEvtArgs<string>>(errSWr_StringWritten!);


        engine.Runtime.IO.SetOutput(ms, outputWr);
        engine.Runtime.IO.SetErrorOutput(ems, errOutputWr);
        try {
            var source = engine.CreateScriptSourceFromString(theScript.ToString(), SourceCodeKind.File);
            Stopwatch sw = new();
            sw.Start();
            source.Execute();
            sw.Stop();
            executedTime = $"Execute Time: { sw.ElapsedMilliseconds} ms";
            consoleTxt = (res.ToString(), ConsoleTxtType.text);
            ShowConsole();
        } catch(Exception err) {
            try {
                consoleTxt = (
                    $"Error in line {((dynamic)err).Line}:\n{(err.Message.Equals("unexpected EOF while parsing") ? "unclosed bracket\\parentheses\\quote" : err.Message)}",
                    ConsoleTxtType.error
                );
            }catch(RuntimeBinderException) {
                consoleTxt = (
                    err.Message.Equals("unexpected EOF while parsing") ? "unclosed bracket\\parentheses\\quote" : err.Message,
                    ConsoleTxtType.error
                );
            }

            errLink = @"https://www.google.com/search?q=Python" + WebUtility.UrlEncode(" " + err.Message);
            
            ShowConsole();
        }

        void sWr_StringWritten(object sender, MyEvtArgs<string> e) =>
            res.Append(e.Value);
        void errSWr_StringWritten(object sender, MyEvtArgs<string> e) =>
            errs.Append(e.Value);
    }

    #endregion

    #region THE EVENTS
    private void Resize_Event(object sender, EventArgs e){
        try{
            var (changeH, changeW) = ((float)(prevHeight-TAB_HEIGHT) / (Height-TAB_HEIGHT), (float)prevWidth / Width); 
            foreach(var window in windows) {
                window.Size.height /= changeH;
                window.Size.width /= changeW;
                window.Pos.x /= changeW;
                window.Pos.y = TAB_HEIGHT + ((window.Pos.y - TAB_HEIGHT) / changeH);
            }
            RefreshConsole();
            var WHTuple = (Width, Height);
            foreach(var button in buttonsOnScreen) {
                var newPos = button.calcPos(WHTuple);
                button.btn.Location = new(newPos.x, newPos.y);
            }

            (prevHeight, prevWidth) = (Height, Width);
            Invalidate();
        }catch(Exception){}
    }
    private void Form1_KeyDown(object? sender, KeyEventArgs e) {
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 0;
        lastPressed = e.KeyCode;
        bool isShift = isShiftPressed();
        if(selectedLine is null && isShift) {
            selectedLine = (CursorPos.Line, CursorPos.Col);
        }
        bool isAltl = isAltlPressed();
        bool isCtrl = isCtrlPressed();
        var refresh = true;
        ((Action)(lastPressed switch {
            Keys.CapsLock => () => refresh = false, // todo display that caps is pressed \ not pressed
            Keys.Insert => () => DrawPicScreen(),
            Keys.End => () => EndKey(isShift, isCtrl),
            Keys.Home => () => HomeKey(isShift, isCtrl),
            Keys.Up => () => UpKey(isShift, isAltl),
            Keys.Down => () => DownKey(isShift, isAltl),
            Keys.Right => () => RightKey(isShift, isAltl, isCtrl),
            Keys.Left => () => LeftKey(isShift, isAltl, isCtrl),
            Keys.F1 => () => OpenErrLink(null, new()),
            _ => () => {lastCol = null; refresh = false;}
        }))();
        
        if(refresh){
            DrawTextScreen();
            Invalidate();
        }
    }

    private void TextBox_TextChanged(object? sender, EventArgs e) {
        if(iChanged) {
            iChanged = false;
            return;
        }
        ReadOnlySpan<char> change = null;
        try {
            change = textBox.Text.AsSpan(1, textBox.Text.Length - 2);
        } catch(IndexOutOfRangeException) { } catch(ArgumentOutOfRangeException) { }
        iChanged = true;
        textBox.Text = "()";
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 0;

        if(selectedLine == (CursorPos.Line, CursorPos.Col)) {
            selectedLine = null;
        }
        var changeSt = change.ToString();
        ((Action)(lastPressed switch {
            Keys.Back => () => BackSpaceKey(),
            Keys.Delete => () => DeleteKey(),
            Keys.Enter => () => EnterKey(),
            _ => () => CharKey(changeSt)
        }))();
        DrawTextScreen();
        Invalidate();
    }
    
    protected override void OnMouseWheel(MouseEventArgs e) {
        if(curWindow.Function.DisplayImage!.Height > 40) {
            ChangeOffsetTo(curWindow.Offset + e.Delta / 10);
        }
        Invalidate();
        base.OnMouseWheel(e);
    }
    protected override void OnMouseDown(MouseEventArgs e) {
        dragging = true;
        Drag(e);
        base.OnMouseDown(e);
    }
    protected override void OnMouseUp(MouseEventArgs e) {
        dragging = false;
        base.OnMouseUp(e);
    }
    protected override void OnMouseDoubleClick(MouseEventArgs e) {
        dragging = false;
        doubleClick = true;

        GoInDirCtrl(GetNextL, isAltlPressed());
        selectedLine = (CursorPos.Line, CursorPos.Col);
        GoInDirCtrl(GetNextR, isAltlPressed());

        DrawTextScreen();
        Invalidate();
        base.OnMouseDoubleClick(e);
    }
    private void ClickedSelected((int line, int col) pos, (int,int) sel) {
        var newSelectedLine = (CursorPos.Line, -1);
        var newCurCol = linesText[CursorPos.Line].Length - 1;
        if(newCurCol == pos.col && newSelectedLine == sel) {
            GoInDirCtrl(GetNextL, isAltlPressed());
            selectedLine = (CursorPos.Line, CursorPos.Col);
            GoInDirCtrl(GetNextR, isAltlPressed());
        } else {
            CursorPos.ChangeCol(newCurCol);
            selectedLine = newSelectedLine;
        }
        DrawTextScreen();
        Invalidate();
    }
    
    async void Drag(MouseEventArgs e) {
        (int line, int col)? tempSelectedLine = null;
        if(e.Button == MouseButtons.Left) {
            (int x, int y) mousePos = (Cursor.Position.X, Cursor.Position.Y);
            foreach(var window in windows) {
                bool inX = mousePos.x >= window.Pos.x && mousePos.x <= window.Pos.x + window.Size.width;
                bool inY = mousePos.y >= window.Pos.y && mousePos.y <= window.Pos.y + window.Size.height;
                if(inX && inY) {
                    if(window.Function.Equals(curFunc)) {
                        var prev = (CursorPos.Line, CursorPos.Col);
                        var prevSel = selectedLine;
                        BtnClick(refresh: false);
                        if(prevSel is (int, int) ps && InBetween((CursorPos.Line, CursorPos.Col), prev, ps)) {
                            ClickedSelected(prev, ps);
                            return;
                        }
                        tempSelectedLine = (CursorPos.Line, CursorPos.Col);
                        break;
                    }
                    curWindow = window;
                    (screenWidth, screenHeight) = ((int, int))window.Size;
                    ChangeTab(window.Function.Button);
                    break;
                }
            }
        } else if(e.Button == MouseButtons.Middle) {
            Execute();
            return;
        } else if(e.Button == MouseButtons.Right) {
            // TODO todo 
            return;
        }

        for(int i = 0; i < 10; i++) {
            await Task.Delay(1);
            if(!dragging) {
                if(doubleClick) {   doubleClick = false; } 
                else            {   BtnClick(); }
                return; 
            }
        }
        if(!isShiftPressed() || selectedLine is null) {
            selectedLine = tempSelectedLine!;
        }
        while(dragging) {
            BtnClick();
            await Task.Delay(1);
        }
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
        var keyCode = (Keys) (msg.WParam.ToInt32() & Convert.ToInt32(Keys.KeyCode));
        if(msg.Msg == WM_KEYDOWN && ModifierKeys == Keys.Control) {
            bool isAltlKeyPressed = isAltlPressed();
            bool isShift = isShiftPressed();
            bool refresh = true;
            ((Action)(keyCode switch {
                Keys.Delete => () => DeleteKey(isAltlKeyPressed, true),
                Keys.Back => () => BackSpaceKey(isAltlKeyPressed, true),
                Keys.Enter => () => EnterKey(true),
                Keys.End => () => EndKey(isShift, true),
                Keys.Home => () => HomeKey(isShift, true),
                Keys.Up => () => ChangeOffsetTo(curWindow.Offset + txtHeight),
                Keys.Down => () => ChangeOffsetTo(curWindow.Offset - txtHeight),
                Keys.Right => () => RightKey(isShift, isAltlKeyPressed, true),
                Keys.Left => () => LeftKey(isShift, isAltlKeyPressed, true),
                Keys.C => () => Copy(isAltlKeyPressed),
                Keys.V => () => Paste(),
                Keys.X => () => Cut(isAltlKeyPressed),
                Keys.D => () => Duplicate(isAltlKeyPressed),
                Keys.A => () => SelectAll(),
                Keys.N => () => MakeNewTab(),
                Keys.Tab => () => CtrlTab(),
                Keys.Oemtilde => () => ToggleConsole(),
                Keys.Oemplus => () => ChangeFontSize((int)boldFont.Size + 1),
                Keys.OemMinus => () => ChangeFontSize((int)boldFont.Size - 1),
                _ => () => refresh = false
            }))();
            if(refresh){
                DrawTextScreen();
                Invalidate();
            }
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    #endregion

    #region Helpers
    private static void ChangeOffsetTo(int i){
        curWindow.Offset = Math.Clamp(
            i, txtHeight - curWindow.Function.DisplayImage!.Height, 0
        );
    }
    private void ChangeFontSize(int size){
        boldFont = new(FontFamily.GenericMonospace, size, FontStyle.Bold);
        indentW = MeasureWidth("    ", boldFont);
        qWidth = MeasureWidth("¿ ?", boldFont);
        qHeight = MeasureHeight("¿?", boldFont);
        upSideDownW = MeasureWidth("¿", boldFont);
        txtHeight = MeasureHeight(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789`~!@#$%^&*()-_=+[]{};:'\"\\/?", 
            boldFont
        );
        Invalidate();
    }
    private static string PythonOperatorToString(PythonOperator po) => po switch{
        PythonOperator.In => "in",
        PythonOperator.Is => "is",
        PythonOperator.IsNot => "is not",
        PythonOperator.Not => "not",
        PythonOperator.NotIn => "not in",
        PythonOperator.LessThan => "<",
        PythonOperator.GreaterThan => ">",
        PythonOperator.LessThanOrEqual => "<=",
        PythonOperator.GreaterThanOrEqual => ">=",
        PythonOperator.Equal => "==",
        PythonOperator.NotEqual => "!=",
        PythonOperator.Add => "+",
        PythonOperator.Subtract => "-",
        PythonOperator.Multiply => "*",
        PythonOperator.Divide => "/",
        PythonOperator.FloorDivide => "//",
        PythonOperator.Power => "**",
        PythonOperator.Mod => "%",
        PythonOperator.BitwiseAnd => "&",
        PythonOperator.BitwiseOr => "|",
        PythonOperator.Xor => "^",
        PythonOperator.LeftShift => "<<",
        PythonOperator.RightShift => ">>",
        PythonOperator.Negate => "-",
        PythonOperator.None => "None",
        PythonOperator.Invert => "~",
        PythonOperator.Pos => "+",
        PythonOperator.TrueDivide => "???? what is true divide???",
        _ => "??I missed one???"
    };
    private static string ReplaceTabs(string st) => st.Replace("\t", "    ");
    private static BM_Middle MakeTxtBM(string txt, Brush? brush = null) {
        var width = MeasureWidth(txt, boldFont);
        var height = MeasureHeight(txt, boldFont);
        var res = new Bitmap(width, height);
        brush = txt switch {
            "True" => greenBrush,
            "False" => redBrush,
            _ => brush ?? textBrush
        };
        var g = Graphics.FromImage(res);
        g.DrawString(txt, boldFont, brush, 0, 0);
        return new(res, (int)(height / 2));
    }
    private static bool IsNumeric(char val) => val == '_' || char.IsLetter(val) || char.IsDigit(val);
    private static bool IsAltNumeric(char val) => char.IsLower(val) || char.IsDigit(val);
    private static int MeasureWidth(string st, Font ft) {
        if(st.Contains('\n', StringComparison.Ordinal)) {
            return st.Split("\n").Select(
                (line) => MeasureWidth(line, boldFont)
            ).Max();
        }
        // used || so that trailing\leading spaces get included too
        // you might wonder why theres an "a" in here... me too... it just doesn't work without it...
        st = ReplaceTabs($"|a{st}|");
        return (int)(nullGraphics.MeasureString(st, ft).Width - CachedWidthOfPipes(ft));
    }
    private static float CachedWidthOfPipes(Font ft){
        if(fontToPipeSize.TryGetValue(ft, out float res)){
            return res;
        }
        float w = nullGraphics.MeasureString("|", ft).Width * 2;
        fontToPipeSize[ft] = w; 
        return w;
    }
    private static int MeasureHeight(string st, Font ft) => (int)nullGraphics.MeasureString(st, ft).Height;

    private static bool isShiftPressed() => (ModifierKeys & Keys.Shift) == Keys.Shift;
    private static bool isCapsPressed() => (ModifierKeys & Keys.CapsLock) == Keys.CapsLock;
    private static bool isAltlPressed() => (ModifierKeys & Keys.Alt) == Keys.Alt;
    private static bool isCtrlPressed() => (ModifierKeys & Keys.Control) == Keys.Control;

    private static bool IsBefore((int line, int col) one, (int line, int col) two) {
        return one.line < two.line || (one.line == two.line && one.col < two.col);
    }
    private static bool InBetween((int line, int col) cur, (int line, int col) one, (int line, int col) two) {
        var (first, last) = IsBefore(one, two) ? (one, two) : (two, one);
        return IsBefore(cur, last) && IsBefore(first, cur);
    }
    #endregion

    #region Math
    private static float Abs(float num) => num > 0 ? num : -num;
    private static float Max(float num1, float num2) => num1 > num2 ? num1 : num2;
    private static int Max(int num1, int num2) => num1 > num2 ? num1 : num2;
    private static float Min(float num1, float num2) => num1 < num2 ? num1 : num2;
    private static int Min(int num1, int num2) => num1 < num2 ? num1 : num2;
    private static (float, float) MaxMin(float num1, float num2) => num1 > num2 ? (num1, num2) : (num2, num1);
    private static (int, int) MaxMin(int num1, int num2) => num1 > num2 ? (num1, num2) : (num2, num1);
    #endregion

    #region Keys
    private void LeftKey(bool isShift, bool isAltlKeyPressed, bool isCtrlKeyPressed) {
        if(!isShift) {
            if(selectedLine is not null &&
                (selectedLine.Value.line < CursorPos.Line ||
                    (selectedLine.Value.line == CursorPos.Line && selectedLine.Value.col < CursorPos.Col)
                )
            ) {
                CursorPos.ChangeCol(selectedLine.Value.col);
                CursorPos.ChangeLine(selectedLine.Value.line);
                lastCol = null;
                return;
            }
            selectedLine = null;
        }
        if(isCtrlKeyPressed) {
            GoInDirCtrl(GetNextL, isAltlKeyPressed);
            lastCol = null;
            return;
        }
        if(CursorPos.Col == -1) {
            if(CursorPos.Line != 0) {
                CursorPos.ChangeLine(CursorPos.Line-1);
                CursorPos.ChangeCol(linesText[CursorPos.Line].Length - 1);
            }
        } else {
            CursorPos.ChangeCol(CursorPos.Col-1);
        }
        lastCol = null;
    }
    private void RightKey(bool isShift, bool isAltlKeyPressed, bool isCtrlKeyPressed) {
        if(!isShift) {
            if(selectedLine is not null &&
                (selectedLine.Value.line > CursorPos.Line ||
                    (selectedLine.Value.line == CursorPos.Line && selectedLine.Value.col > CursorPos.Col)
                )
            ) {
                CursorPos.ChangeLine(selectedLine.Value.line); 
                CursorPos.ChangeCol(selectedLine.Value.col);
                lastCol = null;
                return;
            }
            selectedLine = null;
        }
        if(isCtrlKeyPressed) {
            GoInDirCtrl(GetNextR, isAltlKeyPressed);
            lastCol = null;
            return;
        }
        if(linesText[CursorPos.Line].Length == CursorPos.Col + 1) {
            if(linesText.Count > CursorPos.Line + 1) {
                CursorPos.ChangeLine(CursorPos.Line+1);
                CursorPos.ChangeCol(-1);
            }
        } else {
            CursorPos.ChangeCol(CursorPos.Col+1);
        }
        lastCol = null;
    }
    private static void DownKey(bool isShift, bool isAltlKeyPressed) {
        if(isAltlKeyPressed) {
            if(selectedLine is (int, int) sl) {
                var (maxLine, minLine) = MaxMin(sl.line, CursorPos.Line);
                if(maxLine == linesText.Count - 1) { return; }
                var prev = linesText[maxLine + 1];
                for(int i = maxLine; i >= minLine; i--) {
                    linesText[i + 1] = linesText[i];
                }
                linesText[minLine] = prev;
                selectedLine = (sl.line + 1, sl.col);
            } else {
                if(CursorPos.Line == linesText.Count - 1) { return; }
                (linesText[CursorPos.Line], linesText[CursorPos.Line + 1]) = (linesText[CursorPos.Line + 1], linesText[CursorPos.Line]);
            }
            CursorPos.ChangeLine(CursorPos.Line + 1);
            return;
        }
        if(!isShift) {
            if(selectedLine is not null && selectedLine.Value.line > CursorPos.Line) {
                CursorPos.ChangeLine(selectedLine.Value.line);
            }
            selectedLine = null;
        }
        if(CursorPos.Line == linesText.Count - 1) {
            CursorPos.ChangeCol(linesText[^1].Length - 1);
        } else {
            CursorPos.ChangeLine(CursorPos.Line+1);
            GetClosestForCaret();
        }
    }
    private static void UpKey(bool isShift, bool isAltlKeyPressed) {
        if(isAltlKeyPressed) {
            if(selectedLine is (int, int) sl) {
                var (maxLine, minLine) = MaxMin(sl.line, CursorPos.Line);
                if(minLine == 0) { return; }
                var prev = linesText[minLine - 1];
                for(int i = minLine; i <= maxLine; i++) {
                    linesText[i - 1] = linesText[i];
                }
                linesText[maxLine] = prev;
                selectedLine = (sl.line - 1, sl.col);
            } else {
                if(CursorPos.Line == 0) {  return; }
                (linesText[CursorPos.Line], linesText[CursorPos.Line - 1]) = (linesText[CursorPos.Line - 1], linesText[CursorPos.Line]);
            }
            CursorPos.ChangeLine(CursorPos.Line - 1);
            return;
        }

        if(!isShift) {
            if(selectedLine is not null && selectedLine.Value.line < CursorPos.Line) {
                CursorPos.ChangeLine(selectedLine.Value.line);
            }
            selectedLine = null;
        }

        if(CursorPos.Line == 0) {
            CursorPos.ChangeCol(-1);
        } else {
            CursorPos.ChangeLine(CursorPos.Line-1);
            GetClosestForCaret();
        }
    }
    private static void HomeKey(bool isShift, bool isCtrlKeyPressed) {
        if(!isShift) { selectedLine = null; }
        if(isCtrlKeyPressed) {
            CursorPos.ChangeLine(0);
            CursorPos.ChangeCol(-1);
        } else {
            int spaces = linesText[CursorPos.Line].Length - linesText[CursorPos.Line].TrimStart().Length;
            if(CursorPos.Col == spaces - 1) {
                CursorPos.ChangeCol(-1);
            } else {
                CursorPos.ChangeCol(spaces - 1);
            }
        }
    }
    private static void EndKey(bool isShift, bool isCtrlKeyPressed) {
        if(!isShift) { selectedLine = null; }
        if(isCtrlKeyPressed) { CursorPos.ChangeLine(linesText.Count - 1); }
        CursorPos.ChangeCol(linesText[CursorPos.Line].Length - 1);
    }

    private static void CharKey(ReadOnlySpan<char> change) {
        if(change == null) { throw new Exception("input is null?"); }
        if(selectedLine is not null) {
            DeleteSelection();
            selectedLine = null;
        }
        CursorPos.ChangeBoth(AddString(change, (CursorPos.Line, CursorPos.Col)));
    }
    private static void EnterKey(bool isCtrl=false) {
        if(isCtrl) {
            CursorPos.ChangeCol(-1);
            selectedLine = null;
        }
        if(selectedLine is not null) {
            DeleteSelection();
            selectedLine = null;
        }
        if(CursorPos.Col == linesText[CursorPos.Line].Length - 1) {
            linesText.Insert(CursorPos.Line + 1, "");
        } else {
            linesText.Insert(CursorPos.Line + 1, linesText[CursorPos.Line][(CursorPos.Col + 1)..]);
            linesText[CursorPos.Line] = linesText[CursorPos.Line][..(CursorPos.Col + 1)];
        }
        if(!isCtrl) {
            CursorPos.ChangeLine(CursorPos.Line+1);
            CursorPos.ChangeCol(-1);
        }
    }
    private void DeleteKey(bool isAlt=false, bool isCtrl=false) {
        if(isCtrl) {
            if(selectedLine is not null) {
                DeleteSelection();
            }
            selectedLine = (CursorPos.Line, CursorPos.Col);
            GoInDirCtrl(GetNextR, isAlt);
        }
        if(selectedLine is not null) {
            DeleteSelection();
            return;
        }
        var thisline = linesText[CursorPos.Line];
        if(CursorPos.Col == thisline.Length - 1) {
            if(CursorPos.Line != linesText.Count - 1) {
                var text = linesText[CursorPos.Line+1];
                linesText.RemoveAt(CursorPos.Line + 1);
                linesText[CursorPos.Line] += text;
            }
        } else {
            linesText[CursorPos.Line] = string.Concat(thisline.AsSpan(0, CursorPos.Col + 1), thisline.AsSpan(CursorPos.Col + 2));
        }
    }
    private void BackSpaceKey(bool isAlt=false, bool isCtrl=false) {
        if(isCtrl) {
            if(selectedLine is not null) {
                DeleteSelection();
            }
            selectedLine = (CursorPos.Line, CursorPos.Col);
            GoInDirCtrl(GetNextL, isAlt);
        }
        if(selectedLine is not null) {
            DeleteSelection();
            return;
        }
        var thisline = linesText[CursorPos.Line];
        if(thisline.Length == 0) {
            if(CursorPos.Line != 0) {
                linesText.RemoveAt(CursorPos.Line);
                CursorPos.ChangeLine(CursorPos.Line-1);
                CursorPos.ChangeCol(linesText[CursorPos.Line].Length - 1);
            }

        } else if(CursorPos.Col == -1) {
            if(CursorPos.Line != 0) {
                var text = linesText[CursorPos.Line];
                linesText.RemoveAt(CursorPos.Line);
                CursorPos.ChangeLine(CursorPos.Line-1);
                CursorPos.ChangeCol(linesText[CursorPos.Line].Length - 1);
                linesText[CursorPos.Line] += text;
            }
        } else {
            linesText[CursorPos.Line] = string.Concat(thisline.AsSpan(0, CursorPos.Col), thisline.AsSpan(CursorPos.Col + 1));
            CursorPos.ChangeCol(CursorPos.Col - 1);
        }
    }
    #endregion

    #region ShortCuts
    private void CtrlTab() {
        for(int i = 0; i < windows.Count; i++) {
            var item = windows[i];
            if(item.Function.Equals(curFunc)) {
                curWindow = windows[(i + 1) % windows.Count];
                (screenWidth, screenHeight) = ((int, int))curWindow.Size;
                ChangeTab(curWindow.Function.Button);
                return;
            }
        }
    }
    private void SelectAll() {
        selectedLine = (0, -1);
        CursorPos.ChangeLine(linesText.Count - 1);
        CursorPos.ChangeCol(linesText[^1].Length - 1);
    }
    private void Duplicate(bool isAltlKeyPressed) {
        if(selectedLine is null) {
            var txt = "\r\n" + linesText[CursorPos.Line];
            if(isAltlKeyPressed) { txt = txt.Trim(); }
            AddString(txt, (CursorPos.Line, linesText[CursorPos.Line].Length - 1));
        } else {
            var caretPos = (CursorPos.Line, CursorPos.Col);
            if(selectedLine.Value.line > CursorPos.Line ||
                    (selectedLine.Value.line == CursorPos.Line && selectedLine.Value.col > CursorPos.Col)
                ) {
                caretPos = selectedLine.Value;
            }
            var txt = GetSelectedText();
            if(isAltlKeyPressed) { txt = txt.Trim(); }
            CursorPos.ChangeBoth(AddString(txt, caretPos));
        }
    }
    private void Cut(bool isAltlKeyPressed) {
        string txt;
        if(selectedLine is null) {
            txt = linesText[CursorPos.Line];
            linesText.RemoveAt(CursorPos.Line);
            GetClosestForCaret();
        } else {
            var select = selectedLine;
            txt = GetSelectedText();
            selectedLine = select;
            DeleteSelection();
        }
        if(isAltlKeyPressed) { txt = txt.Trim(); }
        if(!txt.Equals("")) {
            Clipboard.SetText(txt);
        }
    }
    private void Paste() {
        if(selectedLine is not null) {
            DeleteSelection();
            selectedLine = null;
        }
        CursorPos.ChangeBoth(AddString(Clipboard.GetText(), (CursorPos.Line, CursorPos.Col)));
    }
    private void Copy(bool isAltlKeyPressed) {
        string txt;
        if(selectedLine is null) { txt = linesText[CursorPos.Line]; } else { txt = GetSelectedText(); }
        if(isAltlKeyPressed) { txt = txt.Trim(); }
        if(!txt.Equals("")) {
            Clipboard.SetText(txt);
        }
    }
    #endregion
    
    #region SetTabSize
    private const int EM_SETTABSTOPS = 0x00CB;
    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr h, int msg, int wParam, int[] lParam);
    public static void SetTabWidth(TextBox textbox, int tabWidth) {
        Graphics graphics = textbox.CreateGraphics();
        var characterWidth = (int)graphics.MeasureString("M", textbox.Font).Width;
        SendMessage(textbox.Handle, EM_SETTABSTOPS, 1, new int[] { tabWidth * characterWidth });
    }
    #endregion

    #region Miscelaneuos
    private static void GetClosestForCaret() {
        if(lastCol is not null) {
            CursorPos.ChangeCol(Min((int)lastCol, linesText[CursorPos.Line].Length - 1));
        } else {
            lastCol = CursorPos.Col;
            CursorPos.ChangeCol(Min(CursorPos.Col, linesText[CursorPos.Line].Length - 1));
        }
    }
    private void BtnClick(bool refresh=true) {
        if(selectedLine is not null) {
            if(!isShiftPressed() && !dragging) {
                selectedLine = null;
            }
        }
        CursorPos.ChangeLine(GetClickRow());
        CursorPos.ChangeCol(BinarySearch(linesText[CursorPos.Line].Length, Cursor.Position.X - curWindow.Pos.x, GetDistW));
        textBox.Focus();
        if(refresh) {
            DrawTextScreen();
            Invalidate();
        }
    }
    private static float GetDistW(int i) {
        return MeasureWidth(linesText[CursorPos.Line][..(i + 1)], boldFont);
    }
    private int GetClickRow() {
        float topBar = this.RectangleToScreen(this.ClientRectangle).Top - this.Top;
        double mouse = Cursor.Position.Y - (curWindow.Pos.y + curWindow.Offset + topBar);
        int i = (int)Math.Floor(mouse / txtHeight);
        return Max(0, Min(linesText.Count - 1, i));
    }
    public static int BinarySearch(int len, float item, Func<int, float> Get) {
        if(len == 0) { return -1; }
        int first = 0, mid;
        int last = len - 1;
        do {
            mid = first + (last - first) / 2;
            var pos = Get(mid);
            if(item > pos)  {   first = mid + 1;    } 
            else            {   last = mid - 1;     }
            if(pos == item) {   return mid;         }
        } while(first <= last);

        var cur = Abs(item - Get(mid));
        if(mid > -1) {
            if(Abs(item - Get(mid - 1)) < cur) {
                return mid - 1;
            }
        }
        if(mid < len - 1) {
            if(Abs(item - Get(mid + 1)) < cur) {
                return mid + 1;
            }
        }
        return mid;
    }
    private static void DeleteSelection() {
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(CursorPos.Line == selectedLine_.line) {
            (int bigger, int smaller) = MaxMin(CursorPos.Col, selectedLine_.col);
                
            linesText[CursorPos.Line] = string.Concat(
                linesText[CursorPos.Line].AsSpan(0, smaller + 1),
                linesText[CursorPos.Line].AsSpan(bigger + 1)
            );
            CursorPos.ChangeCol(smaller);
        } else {
            ((int line, int col) smaller, (int line, int col) bigger) 
                = CursorPos.Line > selectedLine_.line 
                    ? (selectedLine_, (CursorPos.Line, CursorPos.Col))
                    : ((CursorPos.Line, CursorPos.Col), selectedLine_);
            linesText[smaller.line] = string.Concat(
                linesText[smaller.line].AsSpan(0, smaller.col + 1),
                linesText[bigger.line].AsSpan(bigger.col + 1));
            for(int i = smaller.line + 1; i <= bigger.line; i++) {
                linesText.RemoveAt(smaller.line + 1);
            }
            CursorPos.ChangeBoth(smaller);
        }
    }
    private (int line, int col, char val)? GetNextR() {
        if(CursorPos.Col != linesText[CursorPos.Line].Length - 1) {
            return (CursorPos.Line, CursorPos.Col + 1, linesText[CursorPos.Line][CursorPos.Col + 1]);
        }
        if(CursorPos.Line == linesText.Count - 1) {
            return null;
        }
        return (CursorPos.Line + 1, -1, '\n');
    }
    private (int line, int col, char val)? GetNextL() {
        if(CursorPos.Col != - 1) {
            return (CursorPos.Line, CursorPos.Col - 1, linesText[CursorPos.Line][CursorPos.Col]);
        }
        if(CursorPos.Line == 0) {
            return null;
        }
        return (CursorPos.Line - 1, linesText[CursorPos.Line - 1].Length - 1, '\n');
    }
    private static void GoInDirCtrl(Func<(int line, int col, char val)?> GetNext, bool isAlt) {
        var next = GetNext();
        char? cur = next?.val;
        if(cur is null) { } 
        else if(" \n\t".Contains(cur.Value)) {
            Move(() => " \n\t".Contains(next!.Value.val));
        } else if(IsNumeric(cur!.Value)) {
            if(isAlt) {
                Move(() => IsAltNumeric(next!.Value.val));
            } else {
                Move(() => IsNumeric(next!.Value.val));
            }
        } else {
            Move(() => !" \n\t".Contains(next!.Value.val) && !IsNumeric(next!.Value.val));
            while(next is not null && " \n\t".Contains(next.Value.val)) {
                CursorPos.ChangeBoth((next!.Value.line, next!.Value.col));
                next = GetNext();
            }
        }
        void Move(Func<bool> Condition) {
            do {
                CursorPos.ChangeBoth((next!.Value.line, next!.Value.col));
                next = GetNext();
            } while(
                next is not null && Condition()
            );
        }
    }
    private static string GetSelectedText() {
        if(selectedLine is null) {  return ""; }
        var res = new StringBuilder();
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(CursorPos.Line == selectedLine_.line) {
            (int bigger, int smaller) = MaxMin(CursorPos.Col, selectedLine_.col);
            return linesText[CursorPos.Line].Substring(smaller + 1, bigger - smaller);
        } else {
            ((int line, int col) smaller, (int line, int col) bigger)
                = CursorPos.Line > selectedLine_.line
                    ? (selectedLine_, (CursorPos.Line, CursorPos.Col))
                    : ((CursorPos.Line, CursorPos.Col), selectedLine_);
            res.AppendLine(linesText[smaller.line][(smaller.col + 1)..]);
            for(int i = smaller.line + 1; i < bigger.line; i++) {
                res.AppendLine(linesText[i]);
            }
            res.Append(linesText[bigger.line].AsSpan(0, bigger.col + 1));
        }
        return res.ToString();
    }
    private static (int, int) AddString(ReadOnlySpan<char> change, (int line, int col) pos) {
        if(change.Contains("\r\n", StringComparison.Ordinal)) { // todo but not the litteral
            var newLines = change.ToString().Split("\r\n");
            var newCol = newLines[^1].Length - 1;
            if(pos.col != linesText[pos.line].Length - 1) {
                newLines[^1] = string.Concat(newLines[^1], linesText[pos.line].AsSpan(pos.col + 1));    
            }
            linesText[pos.line] = string.Concat(linesText[pos.line].AsSpan(0, pos.col + 1), newLines[0]);
            for(int i = 1; i < newLines.Length; i++) {
                linesText.Insert(pos.line + 1, newLines[i]);
                pos.line++;
            }
            pos.col = newCol;
        } else {
            var line = linesText[pos.line];
            var start = pos.col == -1 ? "" : line.AsSpan(0, pos.col+1);
            linesText[pos.line] = $"{start}{change}{line.AsSpan(pos.col + 1)}";
            pos.col += change.Length;
        }
        return (pos.line, pos.col);
    }
    
    #endregion
}

enum ConsoleTxtType {   text, error }
record class BM_Middle(Bitmap Img, int Middle);
static class CursorPos {
    public static int Line{ get; private set; } = 0;
    public static int Col{ get; private set; } = -1;
    private static void RealignWondow(){
        int txtPos = CursorPos.Line * Form1.txtHeight;
        int pos = txtPos + Form1.curWindow.Offset; 
        if(pos < 0){
            Form1.curWindow.Offset = - txtPos;
        } else if(pos > Form1.curWindow.Size.height){
            Form1.curWindow.Offset = - (txtPos - (int)Form1.curWindow.Size.height);
        }
        // TODO for col too
    }
    public static void ChangeLine(int i){
        CursorPos.Line = i;
        RealignWondow();
    }
    public static void ChangeCol(int i){
        CursorPos.Col = i;
        RealignWondow();
    }
    public static void ChangeBoth((int line, int col) val){
        (CursorPos.Line, CursorPos.Col) = val;
        RealignWondow();
    }
}
public class Window {
    public Function Function;
    public (float width, float height) Size;
    public (float x, float y) Pos;
    public int Offset = 0;
    public Window(Function func) {  Function = func; }
}
public class Function {
    public readonly List<string> LinesText = new(){ "" };
    public Bitmap? DisplayImage;
    public string Name = null!;
    public int CurLine = 0;
    public int CurCol = -1;
    public Button Button = null!;
    public bool isPic = false;
}
public class MyEvtArgs<T>: EventArgs {
    public T Value { get; private set; }
    public MyEvtArgs(T value) {
        this.Value = value;
    }
}
public class EventRaisingStreamWriter: StreamWriter {
    public event EventHandler<MyEvtArgs<string>> StringWritten = null!;
    public EventRaisingStreamWriter(Stream s) : base(s) { }
    private void LaunchEvent(string txtWritten) {
        // invoke just calls it. so this checks if its null and then calls it
        StringWritten?.Invoke(this, new MyEvtArgs<string>(txtWritten));
    }
    public override void Write(string? value) {
        base.Write(value);
        LaunchEvent(value!);
    }
    public override void Write(bool value) {
        base.Write(value);
        LaunchEvent(value.ToString());
    }
    // here override all writing methods...
}