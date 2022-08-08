using IronPython;
using System.Text;
using IronPython.Hosting;
using Microsoft.Scripting;
using IronPython.Compiler;
using GraphicIDE.Properties;
using IronPython.Compiler.Ast;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Hosting;
using System.Runtime.InteropServices;
using Microsoft.Scripting.Hosting.Providers;
using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GraphicIDE;



/*
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  MAKE CUSTOM "TABS" with buttons
 *  cuz the flickering is annying 
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 */







public partial class Form1: Form {
    private static readonly List<Button> linesButton = new();
    private static List<string> linesText = null!;
    private Bitmap screen;
    private static int curLine = 0, curCol = -1;
    private static readonly TextBox textBox = new();
    private static readonly StringFormat stringFormat = new();
    private static Keys lastPressed;
    private static int? lastCol = null;
    private static (int line, int col)? selectedLine = null;
    private static readonly Font boldFont = new(FontFamily.GenericMonospace, 15, FontStyle.Bold);
    private static bool iChanged = false;
    private const int LINE_HEIGHT = 30, WM_KEYDOWN = 0x100, TAB_HEIGHT = 20, TAB_WIDTH = 80;
    private static readonly Graphics nullGraphics = Graphics.FromImage(new Bitmap(1,1));
    private static readonly int INDENT = MeasureWidth(nullGraphics, "    ", boldFont);
    private static readonly int 
        qWidth = MeasureWidth(nullGraphics, "¿ ?", boldFont),
        qHeight = MeasureHeight(nullGraphics, "¿?", boldFont),
        upSideDownW = MeasureWidth(nullGraphics, "¿", boldFont),
        txtHeight = MeasureHeight(nullGraphics, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", boldFont);
    
    #region Tabs
    private static int currentTab = 0;
    private static readonly List<Button> tabButtons = new();
    private static int tabButtonEnd = 0;
    private static readonly Dictionary<string, Function> nameToFunc = new();
    private static Function curFunc = null!;
    #endregion

    #region BrusesAndPens
    static readonly float[] dashes = new[] { 5f, 2f };
    static readonly Color
        listRed = Color.FromArgb(255, 157, 59, 52),
        redOpaqe = Color.FromArgb(100, 255, 000, 000),
        blueOpaqe = Color.FromArgb(100, 75, 180, 245),
        keyOrange = Color.FromArgb(255, 245, 190, 80),
        greenOpaqe = Color.FromArgb(100, 000, 255, 000),
        mathPurple = Color.FromArgb(255, 164, 128, 207),
        orangeOpaqe = Color.FromArgb(100, 250, 200, 93);
    private static readonly SolidBrush
        keyOrangeB = new(keyOrange),
        whiteBrush = new(Color.White),
        mathPurpleB = new(mathPurple),
        curserBrush = new(Color.WhiteSmoke),
        redBrush = new(Color.FromArgb(255, 200, 49, 45)),
        intBrush = new(Color.FromArgb(255, 207, 255, 182)),
        textBrush = new(Color.FromArgb(255, 160, 160, 160)),
        greenBrush = new(Color.FromArgb(255, 110, 255, 130)),
        selectBrush = new(Color.FromArgb(100, 000, 100, 255)),
        stringBrush = new(Color.FromArgb(255, 255, 204, 116)),
        parenthesiesBrush = new(Color.FromArgb(255, 76, 175, 104));
    static readonly Pen
        redDashed = new(redOpaqe, 5){ DashPattern = dashes },
        blueDashed = new(blueOpaqe, 5){ DashPattern = dashes },
        greenDashed = new(greenOpaqe, 5){ DashPattern = dashes },
        orangeDashed = new(orangeOpaqe, 5){ DashPattern = dashes },
        redListP = new(listRed, 5),
        redOpaqeP = new(redOpaqe, 5),
        blueOpaqeP = new(blueOpaqe, 5),
        keyOrangeP = new(keyOrange, 5),
        greenOpaqeP = new(greenOpaqe, 5),
        mathPurpleP = new(mathPurple, 5),
        orangeOpaqeP = new(orangeOpaqe, 5);
    #endregion

    #region Images
    private static readonly Bitmap 
        printerImg = new(Resources.printer),
        rulerImg = new(Resources.ruler),
        passImg = new(Resources.pass),
        sumImg = new(Resources.sum),
        emptyListImg = new(Resources.emptyList);
    #endregion

    #region Start
    public Form1() {
        InitializeComponent();
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.Black;
        this.DoubleBuffered = true;
        this.screen = new Bitmap(this.Width, this.Height);

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

        Controls.Add(textBox);
        textBox.TextChanged += new EventHandler(textBox_TextChanged!);
        textBox.KeyDown += new KeyEventHandler(Form1_KeyDown!);

        stringFormat.SetTabStops(0, new float[] { 4 });
        ResumeLayout(false);
        PerformLayout();
        Start();
    }
    async void Start() {
        await Task.Delay(10);
        AddTab("Main", isFirst: true);
        DrawNewScreen();
        Refresh();
    }
    private void Form1_Paint(object sender, PaintEventArgs e) {
        e.Graphics.DrawImage(screen, 0, TAB_HEIGHT);
    }
    #endregion

    #region AST
    Dictionary<dynamic, ((int line, int col) Start, (int line, int col) End)> nodeToPos = new();

    private (Bitmap? img, int middle) MakeImg(dynamic ast) {
        try {
            return ((Func<(Bitmap? img, int middle)>)(ast.NodeName switch {
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

        _ => () => (null, 0)
            }))();
        } catch(Exception) { 
            return (null, 0);
        }
    }
    
    private static (Bitmap? img, int middle) emptyListScaled = (null, 0);
    private (Bitmap img, int middle) ListExpression(dynamic ast) {
        if(ast.Items.Length == 0) {
            if(emptyListScaled.img is not null) {
                return emptyListScaled;
            }
            Bitmap emptyList = new(emptyListImg, (int)(emptyListImg.Width / (emptyListImg.Height / (txtHeight + 15))), txtHeight + 15);
            Bitmap padded = new(emptyList.Width + 5, emptyList.Height);
            var pg = Graphics.FromImage(padded);
            pg.DrawImage(emptyList, 5, 0);
            emptyListScaled = (padded, (int)(padded.Height / 2));
            return emptyListScaled;
        }
        int lineLen = 5;
        int gap = 5;
        List<(Bitmap img, int middle)> elements = new();
        var (width, heightT, heightB) = (lineLen + gap, 0, 0);
        foreach(var item in ast.Items) {
            var img = MakeImg(item);
            var middle = img.Item2;
            elements.Add(((Bitmap, int)) img);
            width += lineLen + img.Item1.Width + 2 * gap;
            heightT = Max(heightT, middle);
            heightB = Max(heightB, img.Item1.Height - middle);
        }
        Bitmap res = new(width, heightB + heightT + 20);
        var g = Graphics.FromImage(res);
        var end = 2;
        foreach(var (img, _) in elements) {
            end += gap;
            g.DrawLine(redListP, end, 5, end, res.Height - 5);
            end += lineLen;
            g.DrawImage(img, end, (int)(res.Height/2 - img.Height/2));
            end += img.Width + gap;
        }
        end += gap;
        g.DrawLine(redListP, end, 5, end, res.Height - 5);
        g.DrawLine(redListP, 5, 5, res.Width, 5);
        g.DrawLine(redListP, 5, res.Height - 5, res.Width, res.Height - 5);
        return (res, (int)(res.Height / 2));

    }
    private static (Bitmap? img, int middle) passPic = (null, 0);
    private static (Bitmap img, int middle) EmptyStatement() {
        if(passPic.img is not null) {   return passPic; }
        Bitmap img = new(passImg, (int)(passImg.Width / (passImg.Height / txtHeight)), txtHeight);
        passPic = (img, (int)(img.Height / 2));
        return passPic;
    }
    
    // todo: while / until / forever
    private (Bitmap img, int middle) WhileStatement(dynamic ast) {
        var condition = MakeImg(ast.Test).Item1;
        var body = MakeImg(ast.Body).Item1;
        Font bigFont = new(FontFamily.GenericMonospace, 30, FontStyle.Bold);
        var infWidth = MeasureWidth(nullGraphics, "∞", bigFont);
        Bitmap res = new(
                width: Max(condition.Width + infWidth, body.Width + INDENT),
                height: condition.Height + body.Height + 14
            );
        var g = Graphics.FromImage(res);
        g.DrawString(
            "∞", bigFont, keyOrangeB, 0, 
            (int)(condition.Height / 2 - qHeight / 2) - 10
        );
        g.DrawImage(condition, infWidth, 0);
        g.DrawLine(blueOpaqeP, 1, 0, 1, res.Height - 5);
        g.DrawImage(body, INDENT, condition.Height);
        g.DrawLine(blueDashed, 4, 1, res.Width, 1);
        g.DrawLine(blueDashed, 4, res.Height - 8, res.Width, res.Height - 8);
        return (res, (int)(res.Height/2));
    }
    private (Bitmap img, int middle) IfExpression(dynamic ast) {
        static Graphics JoinIfAndElse(Pen lastColor, ref Bitmap res, Bitmap img) {
            var (prevH, prevW) = (res.Height, res.Width);
            var prevImg = res;
            res = new(Max(res.Width, img.Width), res.Height + img.Height);
            Graphics resG = Graphics.FromImage(res);
            resG.DrawImage(prevImg, 0, 0);
            resG.DrawLine(lastColor, 4, prevH + 1, prevW, prevH + 1);
            resG.DrawImage(img, 0, prevH);
            return resG;
        }

        static Bitmap MakeIfOrElif(Bitmap condition, Bitmap body, Pen pen) {
            Bitmap res = new(
                width: Max(condition.Width + qWidth, body.Width + INDENT),
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
            g.DrawImage(body, INDENT, condition.Height);
            return res;
        }
        
        var mainIf = ast.Tests[0];
        Pen lastColor = greenDashed;
        var ifCond = MakeImg(mainIf.Test).Item1;
        var ifBody = MakeImg(mainIf.Body).Item1;
        var res = MakeIfOrElif(ifCond, ifBody, greenOpaqeP);
        var resG = Graphics.FromImage(res);
        resG.DrawLine(lastColor, 4, 1, res.Width, 1);

        if(ast.Tests.Length > 1) {
            lastColor = orangeDashed;
            for(int i = 1; i < ast.Tests.Length; i++) {
                var item = ast.Tests[i];
                var cond = MakeImg(item.Test).Item1;
                var body = MakeImg(item.Body).Item1;
                var img = MakeIfOrElif(cond, body, orangeOpaqeP);
                resG = JoinIfAndElse(lastColor, ref res, img);
            }
        }

        if(ast.ElseStatement is not null) {
            var elseBody = MakeImg(ast.ElseStatement).Item1;
            Bitmap elseImg = new(
                width: elseBody.Width + INDENT,
                height: elseBody.Height + 7
            );
            var eg = Graphics.FromImage(elseImg);
            eg.DrawLine(redOpaqeP, 1, 0, 1, elseImg.Height);
            eg.DrawImage(elseBody, INDENT, 0);
            resG = JoinIfAndElse(lastColor, ref res, elseImg);
            lastColor = redDashed;
        }
        resG.DrawLine(lastColor, 4, res.Height - 3, res.Width, res.Height - 3);
        Bitmap addPad = new(res.Width, res.Height + 5);
        var g = Graphics.FromImage(addPad);
        g.DrawImage(res, 0, 0);
        return (addPad, (int)(res.Height/2));
    }
    private (Bitmap? img, int middle) FunctionCall(dynamic ast) {
        if(ast.Target.Name == "sqrt" && ast.Target.Target.Name == "math") {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Item1;
            Bitmap res = new(
                width: inside.Width + 30, 
                height: inside.Height + 15
            );
            var g = Graphics.FromImage(res);
            g.DrawImage(inside, 20, 15);
            g.DrawLine(mathPurpleP, 15, 5, res.Width, 5);
            g.DrawLine(mathPurpleP, 15, 5, 15, res.Height);
            g.DrawLine(mathPurpleP, 5, res.Height - 15, 15, res.Height);
            return (res, (int)(res.Height / 2));
        }
        else if(ast.Target.Name == "sum") {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Item1;
            Bitmap sum = new(sumImg, sumImg.Width / (sumImg.Height / inside.Height), inside.Height);
            Bitmap res = new(
                width: inside.Width + sum.Width + 10,
                height: inside.Height
            );
            var g = Graphics.FromImage(res);
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, 255, 255, 255)), 0, 0, res.Width, res.Height);
            g.DrawImage(sum, 5, 0);
            g.DrawImage(inside, sum.Width + 5, 0);
            return (res, (int)(res.Height / 2));
        }
        else if(ast.Target.Name == "len") {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Item1;
            Bitmap ruler = new(rulerImg, rulerImg.Width / (rulerImg.Height / inside.Height), inside.Height);
            Bitmap res = new(
                width: inside.Width + ruler.Width + 10,
                height: inside.Height
            );
            var g = Graphics.FromImage(res);
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, 215, 206, 180)), 5 + ruler.Width, 0, res.Width, res.Height);
            g.DrawImage(ruler, 5, 0);
            g.DrawImage(inside, ruler.Width + 5, 0);
            return (res, (int)(res.Height / 2));
        }
        else if(ast.Target.Name == "abs") {
            var val = ast.Args[0].Expression;
            var inside = MakeImg(val).Item1;
            Bitmap res = new(
                width: inside.Width + 30,
                height: inside.Height + 10
            );
            var g = Graphics.FromImage(res);
            g.DrawLine(mathPurpleP, 10, 0, 10, res.Height);
            g.DrawLine(mathPurpleP, res.Width - 5, 0, res.Width - 5, res.Height);
            g.DrawImage(inside, 15, 5);
            return (res, (int)(res.Height / 2));
        }
        return (null, 0);
    }
    private (Bitmap img, int middle) UnaryExpression(dynamic ast) {
         var op = ast.Op switch {
            PythonOperator.Not => "not",
            PythonOperator.Negate => "-",
            PythonOperator.Invert => "~",
            PythonOperator.Pos => "+",
            PythonOperator.TrueDivide => "???? what is true divide???",
            _ => "??I missed one???"
        };
        
        int gap = 5;
        var opWidth = MeasureWidth(nullGraphics, op, boldFont);
        var opHeight = MeasureHeight(nullGraphics, op, boldFont);
        var img = MakeImg(ast.Expression);
        (Bitmap bmap, int middle) = (img.Item1, img.Item2);
        var top = middle;
        Bitmap res = new(
            width: bmap.Width + opWidth + gap,
            height: bmap.Height
        );
        var g = Graphics.FromImage(res);
        
        g.DrawString(op, boldFont, mathPurpleB, x: gap, y: (int)(middle - opHeight / 2));
        g.DrawImage(bmap, x: opWidth + gap, y: (int)(middle - top));
        return (res, middle);
    }
    private (Bitmap img, int middle) Comparison(dynamic ast) {
        var op = ast.Operator switch {
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
            PythonOperator.TrueDivide => "???? what is true divide???",
            _ => "??I missed one???"
        };
        
        int gap = 5;
        var opWidth = MeasureWidth(nullGraphics, op, boldFont) + gap * 2;
        var opHeight = MeasureHeight(nullGraphics, op, boldFont);
        var l = MakeImg(ast.Left);
        (Bitmap left, int lmiddle) = (l.Item1, l.Item2);
        var r = MakeImg(ast.Right);
        (Bitmap right, int rmiddle) = (r.Item1, r.Item2);
        switch(op){
            case "**":
                return Power(left, right);
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
        var g = Graphics.FromImage(res);
        
        g.DrawImage(left, 0, y: (int)(resMiddle - lTop));
        g.DrawString(op, boldFont, mathPurpleB, x: left.Width + gap, y: (int)(resMiddle - opHeight / 2));
        g.DrawImage(right, x: left.Width + opWidth, y: (int)(resMiddle - rTop));
        return (res, resMiddle);
    }
    private (Bitmap img, int middle) Operator(dynamic ast) {
        #region OperatorSwitch
        var op = ast.Operator switch {
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
            PythonOperator.TrueDivide => "???? what is true divide???",
            _ => "??I missed one???"
        };
        #endregion
        
        int gap = 5;
        var opWidth = MeasureWidth(nullGraphics, op, boldFont) + gap * 2;
        var opHeight = MeasureHeight(nullGraphics, op, boldFont);
        var l = MakeImg(ast.Left);
        (Bitmap left, int lmiddle) = (l.Item1, l.Item2);
        var r = MakeImg(ast.Right);
        (Bitmap right, int rmiddle) = (r.Item1, r.Item2);
        switch(op){
            case "**":
                return Power(left, right);
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
        var g = Graphics.FromImage(res);
        
        g.DrawImage(left, 0, y: (int)(resMiddle - lTop));
        g.DrawString(op, boldFont, mathPurpleB, x: left.Width + gap, y: (int)(resMiddle - opHeight / 2));
        g.DrawImage(right, x: left.Width + opWidth, y: (int)(resMiddle - rTop));
        return (res, resMiddle);
    }
    private static (Bitmap img, int middle) Power(Bitmap bottom, Bitmap top) {
        int bottomGap =  (int)(bottom.Height / 2);
        Bitmap res = new(
            width: top.Width + bottom.Width - 5,
            height: Max(bottomGap, top.Height) + bottomGap
        );
        var g = Graphics.FromImage(res);
        g.DrawImage(bottom, 0, y: res.Height - bottom.Height);
        g.DrawImage(top, x: bottom.Width - 5, y: 0);
        return (res, res.Height - bottomGap);
    }
    private static (Bitmap img, int middle) FloorDivide(Bitmap bottom, Bitmap top) {
        Bitmap res = new(
            width: Max(top.Width, bottom.Width),
            height: top.Height + bottom.Height + 22
        );
        var g = Graphics.FromImage(res);
        g.DrawImage(top, x: (int)((res.Width - top.Width) / 2), y: 0);
        g.DrawLine(mathPurpleP, 5, top.Height + 5, res.Width, top.Height + 5);
        g.DrawLine(mathPurpleP, 5, top.Height + 11, res.Width, top.Height + 11);
        g.DrawImage(bottom, x: (int)((res.Width - bottom.Width) / 2), y: top.Height + 22);
        return (res, top.Height + 11);
    }
    private static (Bitmap img, int middle) Divide(Bitmap bottom, Bitmap top) {
        Bitmap res = new(
            width: Max(top.Width, bottom.Width),
            height: top.Height + bottom.Height + 15
        );
        var g = Graphics.FromImage(res);
        g.DrawImage(top, x: (int)((res.Width - top.Width) / 2), y: 0);
        g.DrawLine(mathPurpleP, 5, top.Height + 5, res.Width, top.Height + 5);
        g.DrawImage(bottom, x: (int)((res.Width - bottom.Width) / 2), y: top.Height + 15);
        return (res, top.Height + 7);
    }
    private (Bitmap img, int middle) Literal(dynamic ast) =>
        MakeTxtBM(ast.Value.ToString(), ast.Type.Name switch {
            "String" => stringBrush,
            "Int32" or "Double" => intBrush,
            _ => textBrush
        });
    private (Bitmap img, int middle) SuiteStatement(dynamic ast) {
        List<Bitmap> resses = new();
        var (height, width) = (0, 0);
        foreach(var statement in ast.Statements) {
            try {
                resses.Add(MakeImg(statement).Item1);
            } catch(Exception) {
                // put just the text in this line
                throw;
            }
            height += resses[^1].Size.Height;
            width = Max(width, resses[^1].Size.Width);
        }
        var end = 20;
        var res = new Bitmap(width, height + end);
        var g = Graphics.FromImage(res);
        foreach(var item in resses) {
            g.DrawImage(item, 0, end);
            end += item.Height;
        }
        return (res, (int)(res.Height / 2));
    }

/*    private (Bitmap img, int middle) MainModuleSTOP(dynamic ast) {
        List<(int line, int col, Color color)> posses = new();
        Dictionary<(int, int), List<string>> hm = new();
        foreach(var item in ast.Body.Statements) {
            if(hm.ContainsKey((item.Start.Line - 1, item.Start.Column - 1))) {
                hm[(item.Start.Line - 1, item.Start.Column - 1)].Add("^");
            } else {
                hm.Add((item.Start.Line - 1, item.Start.Column - 1), new() { "^" });
            }
            if(hm.ContainsKey((item.End.Line - 1, item.End.Column - 1))) {
                hm[(item.End.Line - 1, item.End.Column - 1)].Add("$");
            } else {
                hm.Add((item.End.Line - 1, item.End.Column - 1), new() { "$" });
            }
        }
        StringBuilder res = new();
        for(int line = 0; line < linesText.Count; line++) {
            for(int col = 0; col < linesText[line].Length; col++) {
                if(hm.ContainsKey((line, col))) {
                    foreach(var color in hm[(line, col)]) {
                        res.Append(color);
                    }
                }
                res.Append(linesText[line][col]);

            }
            if(hm.ContainsKey((line, linesText[line].Length))) {
                foreach(var color in hm[(line, linesText[line].Length)]) {
                    res.Append(color);
                }
            }
            res.Append('\n');
        }
        var s = res.ToString();
        var height = MeasureHeight(nullGraphics, s, boldFont);
        var width = MeasureWidth(nullGraphics, s, boldFont);
        Bitmap bm = new(width, height);
        var g = Graphics.FromImage(bm);
        g.DrawString(s.Replace("\t", "    "), boldFont, textBrush, 0, 0);

        return (bm, 1);
    }
*/
    private (Bitmap img, int middle) MainModule(dynamic ast) {
        List<int> toRem = new();
        for(int i = 0; i < ast.Body.Statements.Length; i++) {
            var item = ast.Body.Statements[i];
            try {
                if(item.Expression.NodeName == "ErrorExpression") {
                    toRem.Add(i);
                }
            } catch(Exception) { }
        }
        toRem.Reverse();
        var statements = new List<dynamic>(ast.Body.Statements);
        foreach(var i in toRem) {
            statements.RemoveAt(i);
        }

        List<Bitmap> resses = new();
        var (height, width) = (0, 0);
        for(int j = 0; j < statements.Count; j++) {
            var statement = statements[j];
            (int line, int col) startI, endI;
            if(j == 0) {
                startI = (0, 0);
            } else {
                var start = statements[j - 1].End;
                startI = (start.Line - 1, start.Column - 1);
            }
            if(j == statements.Count - 1) {
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
                var isAfterStart = startI.line < curLine || (startI.line == curLine && startI.col < curCol + 1);
                var isBeforeEnd = endI.line > curLine || (endI.line == curLine && endI.col > curCol);
                if(isAfterStart && isBeforeEnd) {
                    throw new Exception();
                }
                var img = MakeImg(statement).Item1;
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
                var len = MeasureWidth(nullGraphics, st, boldFont);
                var lenH = MeasureHeight(nullGraphics, st, boldFont);
                Bitmap bm = new(len, lenH);
                var bg = Graphics.FromImage(bm);
                bg.DrawString(st.Replace("\t", "    "), boldFont, textBrush, 0, 0);
                resses.Add(bm);
            }
            height += resses[^1].Size.Height;
            width = Max(width, resses[^1].Size.Width);
        }
        var end = 20;
        var res = new Bitmap(width, height + end);
        var g = Graphics.FromImage(res);
        foreach(var item in resses) {
            g.DrawImage(item, 0, end);
            end += item.Height;
        }
        return (res, (int)(res.Height / 2));
    }
    private (Bitmap img, int middle) PrintStatement(dynamic ast) {
        List<Bitmap> resses = new();
        var (width, height) = (0, 0);
        foreach(var statement in ast.Expressions) {
            if(statement.NodeName == "TupleExpression") {
                resses.Add(MakeImg(statement).Item1);
            } else {
                resses.Add(MakeImg(statement.Expression).Item1);
            }
            width += resses[^1].Width;
            height = Max(height, resses[^1].Height);
        }
        var printer = new Bitmap(printerImg, new Size(printerImg.Width / (printerImg.Height / (height + 20)), height + 20));
        var res = new Bitmap(width + printer.Width + 20, printer.Height);
        var g = Graphics.FromImage(res);
        g.DrawImage(printer, 0, 0);
        var end = printer.Width;
        g.DrawRectangle(new Pen(Color.White, 5), end, 2.5f, res.Width - printer.Width - 10, res.Height - 5);

        end += 5;
        foreach(var item in resses) {
            g.DrawImage(item, end, 12);
            end += item.Width;
        }
        return (res, (int)(res.Height / 2));
    }
    private (Bitmap img, int middle) ParenthesisExpression(dynamic ast) {
        Bitmap inside = MakeImg(ast.Expression).Item1;
        var width = inside.Width;
        var height = inside.Height;
        var parHeight = height + 10;
        var parWidth = parHeight / 5;
        Bitmap res = new(
            width + 20 + (parWidth + 5) * 2,
            height + 20
        );

        var g = Graphics.FromImage(res);
        var end = 10;
        g.DrawArc(mathPurpleP, new Rectangle(end, 5, parWidth, parHeight), 90, 180);
        end += parWidth + 5;
        g.DrawImage(inside, end, 10);
        g.DrawArc(mathPurpleP, new Rectangle(
            end + width + 5, 5, parWidth, parHeight
        ), 270, 180);
        return (res, (int)(res.Height / 2));
    }
    private (Bitmap img, int middle) TupleExpression(dynamic ast) {
        List<Bitmap> resses = new();
        var (width, height) = (0, 0);
        foreach(var statement in ast.Items) {
            resses.Add(MakeImg(statement).Item1);
            width += resses[^1].Width;
            height = Max(height, resses[^1].Height);
        }
        int commaLen = MeasureWidth(nullGraphics, ",", boldFont);
        var res = new Bitmap(width + commaLen * (resses.Count - 1), height);

        var g = Graphics.FromImage(res);
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
        return (res, (int)(res.Height / 2));
    }
    private (Bitmap img, int middle) AssignmentStatement(dynamic ast) {
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
            assignmentNames.Add(MakeImg(item).Item1);
            if(h1 != 0) { h1 += gap; }
            h1 += assignmentNames[^1].Height;
            w1 = Max(w1, assignmentNames[^1].Width);
        }
        Bitmap valueName = MakeImg(ast.Right).Item1;
        var (h2, w2) = (valueName.Height, valueName.Width);

        var height = Max(h1, h2) + lineWidth * 2 + gap * 2;
        var width = w1 + w2 + lineWidth * 3 + gap * 4;
        Bitmap res = new(width, height);

        var g = Graphics.FromImage(res);
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
        return (res, (int)(res.Height / 2));
    }
    #endregion

    #region Tabs

    private void AddTab(string name, bool isFirst=false) {
        Function func = new() {    Name = name };
        if(isFirst) { curFunc = func; }
        nameToFunc.Add(name, func);
        Button btn = new() {
            Name = name,
            Text = name,
            BackColor = Color.WhiteSmoke,
            Location = new(tabButtonEnd, 0),
            Size = new(TAB_WIDTH, TAB_HEIGHT),
        };
        ChangeTab(btn);
        btn.Click += new EventHandler(ChangeTab!);
        tabButtons.Add(btn);
        Controls.Add(btn);
        tabButtonEnd += btn.Width + 10;
    }
    private void ChangeTab(object sender, EventArgs e) =>
        ChangeTab((Button)sender);
    private void ChangeTab(Button btn) {
        curFunc.CurLine = curLine;
        curFunc.CurCol = curCol;
        var func = nameToFunc[btn.Name];
        curFunc = func;
        linesText = func.linesText;
        curLine = func.CurLine;
        curCol = func.CurCol;
        textBox.Focus();
        DrawNewScreen();
        Refresh();
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
            AddTab(((TextBox)sender).Text);
        }
    }
    #endregion

    private void DrawNewScreen() {
        foreach(var b in linesButton) { // not efficient
            this.Controls.Remove(b);
        }
        linesButton.Clear();

        /*var img = MakeImg(ToAST()).img;
        if(img != null) {
            screen = img;
            return;
        }*/

        List<Bitmap> bitmaps = new();
        int totalWidth = 0;
        int end = 0;
        for(int i = 0; i < linesText.Count; i++) {
            var lineText = linesText[i];

            lineText = lineText.Replace("\t", "    ");
            int width = MeasureWidth(nullGraphics, lineText, boldFont);
            totalWidth = Max(totalWidth, width);

            Bitmap bm = new(width, txtHeight);
            var g = Graphics.FromImage(bm);
            g.DrawString(lineText, boldFont, textBrush, 0, 0);

            if(i == curLine) {
                var before = curCol == -1 ? "": lineText[..(curCol + 1)];
                g.FillRectangle(
                    curserBrush,
                    MeasureWidth(nullGraphics, before, boldFont) - 3,
                    5, 1, txtHeight - 10
                );
            }

            NewButton(end, i, bm); // not efficient

            if(selectedLine is not null) {
                (int line, int col) = ((int, int))selectedLine;
                if((i < line && i > curLine) || (i > line && i < curLine)) {
                    g.FillRectangle(
                        selectBrush, 0, 0,
                        MeasureWidth(g, lineText, boldFont), LINE_HEIGHT
                    );
                } else if(i == line || i == curLine) {
                    int cCol = curCol, sCol = col;
                    if(i == line) {
                        cCol = i == curLine ? curCol : (i > curLine ? -1 : lineText.Length - 1);
                    } else {
                        sCol = i > line ? -1 : lineText.Length - 1;
                    }
                    var (smaller, bigger) = cCol < sCol ? (cCol, sCol) : (sCol, cCol);
                    var startS = MeasureWidth(g, lineText[..(smaller + 1)], boldFont);
                    var endS = MeasureWidth(g, lineText[..(bigger + 1)], boldFont);
                    g.FillRectangle(selectBrush, 0 + startS, 0, endS - startS, LINE_HEIGHT);
                }
            }
            end += bm.Height;
            bitmaps.Add(bm);
        }
        Bitmap newBitMap = new(totalWidth, end);
        var gr = Graphics.FromImage(newBitMap);
        end = 0;
        foreach(var item in bitmaps) {
            gr.DrawImage(item, 0, end);
            end += item.Height;
        }
        screen = newBitMap;
    }
    
    private static void GetClosestForCaret() {
        if(lastCol is not null) {
            curCol = Min((int)lastCol, linesText[curLine].Length - 1);
        } else {
            lastCol = curCol;
            curCol = Min(curCol, linesText[curLine].Length - 1);
        }
    }
    private Bitmap MakeEmptyLine(int? width = null) => new(width ?? this.Width, LINE_HEIGHT);
    private void BtnClick(int i) {
        curLine = i;
        curCol = BinarySearch(linesText[curLine].Length, Cursor.Position.X);
        textBox.Focus();
        DrawNewScreen();
        Refresh();
    }
    private static float GetDist(Graphics g, int i) {
        return MeasureWidth(g, linesText[curLine][..(i + 1)], boldFont);
    }
    public static int BinarySearch(int len, float item) {
        if(len == 0) { return -1; }
        int first = 0, mid;
        int last = len - 1;
        do {
            mid = first + (last - first) / 2;
            var pos = GetDist(nullGraphics, mid);
            if(item > pos)  {   first = mid + 1;    } 
            else            {   last = mid - 1;     }
            if(pos == item) {   return mid;         }
        } while(first <= last);

        var cur = Abs(item - GetDist(nullGraphics, mid));
        if(mid > -1) {
            if(Abs(item - GetDist(nullGraphics, mid - 1)) < cur) {
                return mid - 1;
            }
        }
        if(mid < len - 1) {
            if(Abs(item - GetDist(nullGraphics, mid + 1)) < cur) {
                return mid + 1;
            }
        }
        return mid;
    }
    private void NewButton(int end, int i, Bitmap line) {
        Button b = new() {
            Location = new Point(0, end + TAB_HEIGHT),
            Size = line.Size,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.Transparent,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Color.Transparent;
        b.FlatAppearance.MouseDownBackColor = Color.Transparent;
        b.FlatAppearance.BorderColor = Color.Black;
        b.MouseClick += new MouseEventHandler(((object sender, MouseEventArgs e) => BtnClick(i))!);
        this.Controls.Add(b);
        linesButton.Add(b);
    }
    private static void DeleteSelection() {
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(curLine == selectedLine_.line) {
            (int bigger, int smaller) = MaxMin(curCol, selectedLine_.col);
                
            linesText[curLine] = string.Concat(
                linesText[curLine].AsSpan(0, smaller + 1),
                linesText[curLine].AsSpan(bigger + 1)
            );
            curCol = smaller;
        } else {
            ((int line, int col) smaller, (int line, int col) bigger) 
                = curLine > selectedLine_.line 
                    ? (selectedLine_, (curLine, curCol))
                    : ((curLine, curCol), selectedLine_);
            linesText[smaller.line] = string.Concat(
                linesText[smaller.line].AsSpan(0, smaller.col + 1),
                linesText[bigger.line].AsSpan(bigger.col + 1));
            for(int i = smaller.line + 1; i <= bigger.line; i++) {
                linesText.RemoveAt(smaller.line + 1);
            }
            (curLine, curCol) = smaller;
        }
    }
    private (int line, int col, char val)? GetNextR() {
        if(curCol != linesText[curLine].Length - 1) {
            return (curLine, curCol + 1, linesText[curLine][curCol + 1]);
        }
        if(curLine == linesText.Count - 1) {
            return null;
        }
        return (curLine + 1, -1, '\n');
    }
    private (int line, int col, char val)? GetNextL() {
        if(curCol != - 1) {
            return (curLine, curCol - 1, linesText[curLine][curCol]);
        }
        if(curLine == 0) {
            return null;
        }
        return (curLine - 1, linesText[curLine - 1].Length - 1, '\n');
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
                (curLine, curCol, _) = next!.Value;
                next = GetNext();
            }
        }
        void Move(Func<bool> Condition) {
            do {
                (curLine, curCol, _) = next!.Value;
                next = GetNext();
            } while(
                next is not null && Condition()
            );
        }
    }
    private static string GetSelectedText() {
        var res = new StringBuilder();
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(curLine == selectedLine_.line) {
            (int bigger, int smaller) = MaxMin(curCol, selectedLine_.col);
            return linesText[curLine].Substring(smaller + 1, bigger - smaller);
        } else {
            ((int line, int col) smaller, (int line, int col) bigger)
                = curLine > selectedLine_.line
                    ? (selectedLine_, (curLine, curCol))
                    : ((curLine, curCol), selectedLine_);
            res.AppendLine(linesText[smaller.line][(smaller.col + 1)..]);
            for(int i = smaller.line + 1; i < bigger.line; i++) {
                res.AppendLine(linesText[i]);
            }
            res.Append(linesText[bigger.line].AsSpan(0, bigger.col + 1));
        }
        return res.ToString();
    }
    private (int, int) AddString(ReadOnlySpan<char> change, (int line, int col) pos) {
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
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
        var keyCode = (Keys) (msg.WParam.ToInt32() & Convert.ToInt32(Keys.KeyCode));
        if(msg.Msg == WM_KEYDOWN && ModifierKeys == Keys.Control) {
            bool isAltlKeyPressed = (ModifierKeys & Keys.Alt) == Keys.Alt;
            bool isShift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            ((Action)(keyCode switch {
                Keys.End => () => EndKey(isShift, isAltlKeyPressed, true),
                Keys.Home => () => HomeKey(isShift, isAltlKeyPressed, true),
                Keys.Up => () => UpKey(isShift, isAltlKeyPressed, true),
                Keys.Down => () => DownKey(isShift, isAltlKeyPressed, true),
                Keys.Right => () => RightKey(isShift, isAltlKeyPressed, true),
                Keys.Left => () => LeftKey(isShift, isAltlKeyPressed, true),
                Keys.C => () => Copy(isAltlKeyPressed),
                Keys.V => () => Paste(),
                Keys.X => () => Cut(isAltlKeyPressed),
                Keys.D => () => Duplicate(isAltlKeyPressed),
                Keys.A => () => SelectAll(),
                Keys.N => () => MakeNewTab(),
                _ => () => _=1
            }))();
            DrawNewScreen();
            Refresh();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    #region Python
    private PythonAst ToAST() {
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
            new CompilerContext(unit, new PythonCompilerOptions(), ErrorSink.Null),
            new PythonOptions()
            );
        
        return p.ParseFile(false);
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
            engine.CreateScriptSourceFromString(theScript.ToString()).Execute();
            MessageBox.Show(res.ToString());
        } catch(Exception err) {
            MessageBox.Show(err.Message);
        }

        void sWr_StringWritten(object sender, MyEvtArgs<string> e) =>
            res.AppendLine(e.Value);
        void errSWr_StringWritten(object sender, MyEvtArgs<string> e) =>
            errs.AppendLine(e.Value);
    }
    #endregion

    #region THE EVENTS
    private void Form1_KeyDown(object sender, KeyEventArgs e) {
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 0;
        lastPressed = e.KeyCode;
        bool isShift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
        if(selectedLine is null && isShift) {
            selectedLine = (curLine, curCol);
        }
        bool isAltl = (ModifierKeys & Keys.Alt) == Keys.Alt;
        bool isCtrl = (ModifierKeys & Keys.Control) == Keys.Control;
        ((Action)(lastPressed switch {
            Keys.CapsLock => () => _=1, // todo display that caps is pressed \ not pressed
            Keys.Insert => () => Execute(),
            Keys.End => () => EndKey(isShift, isAltl, isCtrl),
            Keys.Home => () => HomeKey(isShift, isAltl, isCtrl),
            Keys.Up => () => UpKey(isShift, isAltl, isCtrl),
            Keys.Down => () => DownKey(isShift, isAltl, isCtrl),
            Keys.Right => () => RightKey(isShift, isAltl, isCtrl),
            Keys.Left => () => LeftKey(isShift, isAltl, isCtrl),
            _ => () => lastCol = null
        }))();
        DrawNewScreen();
        Refresh();
    }

    private void textBox_TextChanged(object sender, EventArgs e) {
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

        if(selectedLine == (curLine, curCol)) {
            selectedLine = null;
        }
        var changeSt = change.ToString();
        ((Action)(lastPressed switch {
            Keys.Back => () => BackSpaceKey(),
            Keys.Delete => () => DeleteKey(),
            Keys.Enter => () => EnterKey(),
            _ => () => CharKey(changeSt)
        }))();
        DrawNewScreen();
        Refresh();
    }
    #endregion

    #region Helpers
    private static (Bitmap img, int middle) MakeTxtBM(string txt, Brush? brush=null) {
        var width = MeasureWidth(nullGraphics, txt, boldFont);
        var height = MeasureHeight(nullGraphics, txt, boldFont);
        var res = new Bitmap(width, height);
        brush = txt switch {
            "True" => greenBrush,
            "False" => redBrush,
            _ => brush ?? textBrush
        };
        var g = Graphics.FromImage(res);
        g.DrawString(txt, boldFont, brush, 0, 0);
        return (res, (int)(height / 2));
    }
    private static bool IsNumeric(char val) => val == '_' || char.IsLetter(val) || char.IsDigit(val);
    private static bool IsAltNumeric(char val) => char.IsLower(val) || char.IsDigit(val);
    private static int MeasureWidth(Graphics g, string st, Font ft) {
        if(st.Contains('\n', StringComparison.Ordinal)) {
            return st.Split("\n").Select(
                (line) => MeasureWidth(g, line, boldFont)
            ).Max();
        }
        // used || so that trailing\leading spaces get included too
        // you might wonder why theres an "a" in here... me too... it just doesn't work without it...
        st = $"|a{st}|".Replace("\t", "    ");
        return (int) (g.MeasureString(st, ft).Width - g.MeasureString("|", ft).Width * 2);
    }
    private static int MeasureHeight(Graphics g, string st, Font ft) => (int)g.MeasureString(st, ft).Height;

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
                (selectedLine.Value.line < curLine ||
                    (selectedLine.Value.line == curLine && selectedLine.Value.col < curCol)
                )
            ) {
                (curLine, curCol) = selectedLine.Value;
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
        if(curCol == -1) {
            if(curLine != 0) {
                curLine--;
                curCol = linesText[curLine].Length - 1;
            }
        } else {
            curCol--;
        }
        lastCol = null;
    }
    private void RightKey(bool isShift, bool isAltlKeyPressed, bool isCtrlKeyPressed) {
        if(!isShift) {
            if(selectedLine is not null &&
                (selectedLine.Value.line > curLine ||
                    (selectedLine.Value.line == curLine && selectedLine.Value.col > curCol)
                )
            ) {
                (curLine, curCol) = selectedLine.Value;
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
        if(linesText[curLine].Length == curCol + 1) {
            if(linesText.Count > curLine + 1) {
                curLine++;
                curCol = -1;
            }
        } else {
            curCol++;
        }
        lastCol = null;
    }
    // todo alt
    private void DownKey(bool isShift, bool isAltlKeyPressed, bool isCtrlKeyPressed) {
        if(!isShift) {
            if(selectedLine is not null && selectedLine.Value.line > curLine) {
                curLine = selectedLine.Value.line;
            }
            selectedLine = null;
        }
        if(isCtrlKeyPressed) {
            // TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO 
        }
        if(curLine == linesText.Count - 1) {
            curCol = linesText[^1].Length - 1;
        } else {
            curLine++;
            GetClosestForCaret();
        }
    }
    // todo alt
    private void UpKey(bool isShift, bool isAltlKeyPressed, bool isCtrlKeyPressed) {
        if(!isShift) {
            if(selectedLine is not null && selectedLine.Value.line < curLine) {
                curLine = selectedLine.Value.line;
            }
            selectedLine = null;
        }
        if(isCtrlKeyPressed) {
            // TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO TODO 
        }
        if(curLine == 0) {
            curCol = -1;
        } else {
            curLine--;
            GetClosestForCaret();
        }
    }
    //todo alt
    private void HomeKey(bool isShift, bool isAltlKeyPressed, bool isCtrlKeyPressed) {
        if(!isShift) { selectedLine = null; }
        if(isCtrlKeyPressed) {
            (curLine, curCol) = (0, -1);
        } else {
            int spaces = linesText[curLine].Length - linesText[curLine].TrimStart().Length;
            if(curCol == spaces - 1) {
                curCol = -1;
            } else {
                curCol = spaces - 1;
            }
        }
    }
    //todo alt
    private void EndKey(bool isShift, bool isAltlKeyPressed, bool isCtrlKeyPressed) {
        if(!isShift) { selectedLine = null; }
        if(isCtrlKeyPressed) { curLine = linesText.Count - 1; }
        curCol = linesText[curLine].Length - 1;
    }

    private void CharKey(ReadOnlySpan<char> change) {
        if(change == null) { throw new Exception("input is null?"); }
        if(selectedLine is not null) {
            DeleteSelection();
            selectedLine = null;
        }
        (curLine, curCol) = AddString(change, (curLine, curCol));
    }
    private void EnterKey() {
        if(selectedLine is not null) {
            DeleteSelection();
            selectedLine = null;
            return;
        }
        if(curCol == linesText[curLine].Length - 1) {
            var map = MakeEmptyLine();
            linesText.Insert(curLine + 1, "");
        } else {
            var map = MakeEmptyLine(); // todo
            linesText.Insert(curLine + 1, linesText[curLine][(curCol + 1)..]);
            linesText[curLine] = linesText[curLine][..(curCol + 1)];
        }
        curLine++;
        curCol = -1;
    }
    // todo ctrl
    private void DeleteKey() {
        if(selectedLine is not null) {
            DeleteSelection();
            return;
        }
        var thisline = linesText[curLine];
        if(curCol == thisline.Length - 1) {
            if(curLine != linesText.Count - 1) {
                var text = linesText[curLine+1];
                linesText.RemoveAt(curLine + 1);
                linesText[curLine] += text;
            }
        } else {
            linesText[curLine] = string.Concat(thisline.AsSpan(0, curCol + 1), thisline.AsSpan(curCol + 2));
        }
    }
    // todo ctrl
    private void BackSpaceKey() {
        if(selectedLine is not null) {
            DeleteSelection();
            return;
        }
        var thisline = linesText[curLine];
        if(thisline.Length == 0) {
            if(curLine != 0) {
                linesText.RemoveAt(curLine);
                curLine--;
                curCol = linesText[curLine].Length - 1;
            }

        } else if(curCol == -1) {
            if(curLine != 0) {
                var text = linesText[curLine];
                linesText.RemoveAt(curLine);
                curLine--;
                curCol = linesText[curLine].Length - 1;
                linesText[curLine] += text;
            }
        } else {
            linesText[curLine] = string.Concat(thisline.AsSpan(0, curCol), thisline.AsSpan(curCol + 1));
            curCol -= 1;
        }
    }
    #endregion

    #region ShortCuts
    private void SelectAll() {
        selectedLine = (0, -1);
        (curLine, curCol) = (linesText.Count - 1, linesText[^1].Length - 1);
    }
    private void Duplicate(bool isAltlKeyPressed) {
        if(selectedLine is null) {
            var txt = "\r\n" + linesText[curLine];
            if(isAltlKeyPressed) { txt = txt.Trim(); }
            AddString(txt, (curLine, linesText[curLine].Length - 1));
        } else {
            var caretPos = (curLine, curCol);
            if(selectedLine.Value.line > curLine ||
                    (selectedLine.Value.line == curLine && selectedLine.Value.col > curCol)
                ) {
                caretPos = selectedLine.Value;
            }
            var txt = GetSelectedText();
            if(isAltlKeyPressed) { txt = txt.Trim(); }
            (curLine, curCol) = AddString(txt, caretPos);
        }
    }
    private void Cut(bool isAltlKeyPressed) {
        string txt;
        if(selectedLine is null) {
            txt = linesText[curLine];
            linesText.RemoveAt(curLine);
            GetClosestForCaret();
        } else {
            var select = selectedLine;
            txt = GetSelectedText();
            selectedLine = select;
            DeleteSelection();
        }
        if(isAltlKeyPressed) { txt = txt.Trim(); }
        Clipboard.SetText(txt);
    }
    private void Paste() {
        if(selectedLine is not null) {
            DeleteSelection();
            selectedLine = null;
        }
        (curLine, curCol) = AddString(Clipboard.GetText(), (curLine, curCol));
    }
    private void Copy(bool isAltlKeyPressed) {
        string txt;
        if(selectedLine is null) { txt = linesText[curLine]; } else { txt = GetSelectedText(); }
        if(isAltlKeyPressed) { txt = txt.Trim(); }
        Clipboard.SetText(txt);
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
}



class Function {
    public readonly List<string> linesText = new(){ "" };
    public Bitmap? DisplayImage;
    public string Name = null!;
    public int CurLine = 0;
    public int CurCol = -1;
}

public class Walker: PythonWalker {
    readonly int mIndex;
    Stack<string>? mResult = null;
    public Walker(int index) {
        mIndex = index;
    }

    public Stack<string>? GetResult() {
        return mResult;
    }

    public override bool Walk(MemberExpression node) {
        if(mIndex == node.Span.End.Index) {
            mResult = new Stack<string>();
            MemberExpression curr = node;

            while(curr.Target is MemberExpression expression) {
                curr = expression;
                mResult.Push(curr.Name.ToString());
            }

            if(curr.Target is NameExpression expression1)
                mResult.Push(expression1.Name.ToString());

            return false;
        }

        return true;
    }
}

public class MyEvtArgs<T>: EventArgs {
    public T Value {
        get;
        private set;
    }
    public MyEvtArgs(T value) {
        this.Value = value;
    }
}

public class EventRaisingStreamWriter: StreamWriter {
    #region Event
    public event EventHandler<MyEvtArgs<string>> StringWritten;
    #endregion

    #region CTOR
    public EventRaisingStreamWriter(Stream s) : base(s) { }
    #endregion

    #region Private Methods
    private void LaunchEvent(string txtWritten) {
        // invoke just calls it. so this checks if its null and then calls it
        StringWritten?.Invoke(this, new MyEvtArgs<string>(txtWritten));
    }
    #endregion

    #region Overrides

    public override void Write(string? value) {
        base.Write(value);
        LaunchEvent(value!);
    }
    public override void Write(bool value) {
        base.Write(value);
        LaunchEvent(value.ToString());
    }
    // here override all writing methods...


    #endregion
}