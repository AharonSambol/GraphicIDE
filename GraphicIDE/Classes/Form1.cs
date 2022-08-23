using System.Runtime.InteropServices;

using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.MyMath;
using static GraphicIDE.Helpers;
using static GraphicIDE.DrawScreen;
using static GraphicIDE.Start;
using static GraphicIDE.Tabs;
using static GraphicIDE.Console;
using static GraphicIDE.KeyInput;

// todo console button changing size??
// todo print and exception
// todo print('1\n2\n3\n4\n5\n6\n7\n8\n9\n') and then changeing focus to console and returning results in print taking up more than screen
// todo when changing font size need to change pen sizes as well 
// todo pic gets smushed when switch to console on laptop (maybe cuz size change and didnt redraw pic?)
// todo while >> until / forever
// todo cache some of the textline images
// todo drag selection
// todo right click
// todo capslock shortcuts
// todo import statements

namespace GraphicIDE;

public partial class Form1: Form {
    #region vars
    public static (int line, int col)? selectedLine = null;
    public static int? lastCol = null;
    public static List<string> linesText = null!;
    public static readonly TextBox textBox = new();
    public static readonly StringFormat stringFormat = new();
    public static Font boldFont = null!;
    public const int WM_KEYDOWN = 0x100;
    public static int indentW, qWidth, qHeight, upSideDownW, txtHeight;
    public static int screenWidth = 0, screenHeight = 0, prevHeight, prevWidth;
    public static List<Window> windows = new();
    public static bool dragging = false, doubleClick = false;
    public static List<(Button btn, Func<(int w, int h), Point> calcPos)> buttonsOnScreen = new();
    public static Point screenPos;
    public static Form1 nonStatic = null!;

    #endregion

    #region Start
    public Form1() {
        nonStatic = this;
        InitializeComponent();
        this.MinimumSize = new(100, 100);
        // this.FormBorderStyle = FormBorderStyle.None;
        
        this.BackColor = Color.Black;
        this.DoubleBuffered = true;
        screenPos = GetScreenPos();

        textBox.AcceptsReturn = true;
        textBox.AcceptsTab = true;
        textBox.Dock = DockStyle.None;
        textBox.Size = new Size(0, 0);

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
        
        (prevHeight, prevWidth) = (screenHeight, screenWidth) = (GetHeight(), GetWidth());

        var (windowHeight, windowWidth) = (
            screenHeight, 
            screenWidth / 2
        );

        var mainFunc = NewFunc(".Main", isfirst: true);
        var mainWindow = MakeNewWindow(mainFunc, size: (windowWidth, windowHeight), pos: (0, 0));
        var func2 = NewFunc(".Main2", isfirst: true);
        var window2 = MakeNewWindow(func2, size: (windowWidth, windowHeight), pos: (windowWidth, 0));
        curWindow = mainWindow;
        curFunc = mainFunc;
        ChangeTab(curFunc.Name);

        AddRunBtn();
        AddDebugBtn();
        AddTabBtn();

        AddConsole();
        MakeExecTimeDisplay();

        DrawTextScreen();
        Invalidate();

        this.WindowState = FormWindowState.Maximized;

        FocusTB();
    }
    #endregion

    #region THE EVENTS
    public void Move_Event(object sender, EventArgs e) => screenPos = GetScreenPos();
    public void Resize_Event(object sender, EventArgs e){
        (int Width, int Height) WHTuple = (screenWidth, screenHeight) = (GetWidth(), GetHeight());
        if(screenHeight == 0 || screenWidth == 0){  return; }
        var (changeH, changeW) = (
            (float)prevHeight / screenHeight,
            (float)prevWidth / screenWidth
        ); 
        
        foreach(var window in windows) {
            window.Size.height /= changeH;
            window.Size.width /= changeW;
            window.Pos.x /= changeW;
            window.Pos.y = window.Pos.y / changeH;
            for (int i=0; i < window.tabButtons.Count; i++) {
                var btn = window.tabButtons[i];
                btn.Location = new((int)window.Pos.x + (TAB_WIDTH + 10) * i, (int)window.Pos.y);
                var a = btn.Location;
            }
        }
        RefreshConsole();
        foreach(var button in buttonsOnScreen) {
            button.btn.Location = button.calcPos(WHTuple);
        }

        (prevHeight, prevWidth) = (screenHeight, screenWidth);
        Invalidate();
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
                Keys.N => () => PromptMakeNewTab(),
                Keys.Tab => () => CtrlTab(),
                Keys.Oemtilde => () => ToggleConsole(),
                Keys.Oemplus => () => ChangeFontSize((int)boldFont.Size + 1),
                Keys.OemMinus => () => ChangeFontSize((int)boldFont.Size - 1),
                Keys.Space => () => PythonFuncs.Execute(),
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

    #region Mouse
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
    public void ClickedSelected((int line, int col) pos, (int,int) sel) {
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
            (int x, int y) mousePos = (
                Cursor.Position.X - screenPos.X, 
                Cursor.Position.Y - screenPos.Y
            );
            foreach(var window in windows) {
                bool inX = mousePos.x >= window.Pos.x && mousePos.x <= window.Pos.x + window.Size.width;
                bool inY = mousePos.y >= window.Pos.y && mousePos.y <= window.Pos.y + window.Size.height;
                if(inX && inY) {
                    if(window.Equals(curWindow)) {
                        var prev = (CursorPos.Line, CursorPos.Col);
                        var prevSel = selectedLine;
                        MouseBtnClick(refresh: false);
                        if(prevSel is (int, int) ps && InBetween((CursorPos.Line, CursorPos.Col), prev, ps)) {
                            ClickedSelected(prev, ps);
                            return;
                        }
                        tempSelectedLine = (CursorPos.Line, CursorPos.Col);
                        break;
                    }
                    bool dontDraw = curWindow.AsPlainText;
                    curWindow = window;
                    ChangeTab(window.Function.Name, dontDraw: dontDraw);
                    break;
                }
            }
        } else if(e.Button == MouseButtons.Middle) {
            PythonFuncs.Execute();
            return;
        } else if(e.Button == MouseButtons.Right) {
            // TODO todo 
            return;
        }

        for(int i = 0; i < 10; i++) {
            await Task.Delay(1);
            if(!dragging) {
                if(doubleClick) {   doubleClick = false; } 
                else            {   MouseBtnClick(); }
                return; 
            }
        }
        if(!isShiftPressed() || selectedLine is null) {
            selectedLine = tempSelectedLine!;
        }
        while(dragging) {
            MouseBtnClick();
            await Task.Delay(1);
        }
    }
    public static void MouseBtnClick(bool refresh=true) {
        if(selectedLine is not null) {
            if(!isShiftPressed() && !dragging) {
                selectedLine = null;
            }
        }
        CursorPos.ChangeLine(GetClickRow());
        CursorPos.ChangeCol(BinarySearch(
            linesText[CursorPos.Line].Length, 
            Cursor.Position.X - curWindow.Pos.x - screenPos.X, 
            GetDistW
        ));
        textBox.Focus();
        if(refresh) {
            DrawTextScreen();
            nonStatic.Invalidate();
        }
    }
    public static int GetClickRow() {
        double mouse = Cursor.Position.Y - (curWindow.Pos.y + curWindow.Offset) - screenPos.Y;
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
    public static float GetDistW(int i) {
        return MeasureWidth(linesText[CursorPos.Line][..(i + 1)], boldFont);
    }
    public static (int line, int col, char val)? GetNextR() {
        if(CursorPos.Col != linesText[CursorPos.Line].Length - 1) {
            return (CursorPos.Line, CursorPos.Col + 1, linesText[CursorPos.Line][CursorPos.Col + 1]);
        }
        if(CursorPos.Line == linesText.Count - 1) {
            return null;
        }
        return (CursorPos.Line + 1, -1, '\n');
    }
    public static (int line, int col, char val)? GetNextL() {
        if(CursorPos.Col != - 1) {
            return (CursorPos.Line, CursorPos.Col - 1, linesText[CursorPos.Line][CursorPos.Col]);
        }
        if(CursorPos.Line == 0) {
            return null;
        }
        return (CursorPos.Line - 1, linesText[CursorPos.Line - 1].Length - 1, '\n');
    }
    public static void GoInDirCtrl(Func<(int line, int col, char val)?> GetNext, bool isAlt) {
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
    #endregion
}

public record class BM_Middle(Bitmap Img, int Middle);
static class CursorPos {
    public static int Line{ get; private set; } = 0;
    public static int Col{ get; private set; } = -1;
    private static void RealignWondow(){
        int txtPos = CursorPos.Line * Form1.txtHeight;
        int pos = txtPos + curWindow.Offset; 
        if(pos < 0){
            curWindow.Offset = - txtPos;
        } else if(pos >= (int)curWindow.Size.height - TAB_HEIGHT - (int)(Form1.txtHeight/0.8)){
            curWindow.Offset = - (txtPos + Form1.txtHeight - (int)curWindow.Size.height + TAB_HEIGHT);
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
    public int tabsEnd = 0;
    public readonly List<Button> tabButtons = new();
    public bool AsPlainText = false;
    public SolidBrush txtBrush = textBrush;
    public Window(Function func) {  
        Function = func; 
    }
}
public class Function {
    public List<string> LinesText = new(){ "" };
    public Bitmap? DisplayImage;
    public int CurLine = 0;
    public int CurCol = -1;
    public bool isPic = false;
    public readonly string Name;
    public Function(string name){
        Name = name;
    }
}
