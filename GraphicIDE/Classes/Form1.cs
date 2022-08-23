using System.Text;
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
// todo each window should have its own tab buttons
// todo print('1\n2\n3\n4\n5\n6\n7\n8\n9\n') and then changeing focus to console and returning results in print taking up more than screen
// todo when changing font size need to change pen sizes as well 
// todo https://stackoverflow.com/questions/1264406/how-do-i-get-the-taskbars-position-and-size
// todo cache some of the textline images
// todo capslock shortcuts
// todo copy paste from whatsapp not working
// todo "make new tab" closes console badly
// todo mouse click not working when not full screen

namespace GraphicIDE;

public partial class Form1: Form {
    #region vars
    public static (int line, int col)? selectedLine = null;
    public static int? lastCol = null;
    public static List<string> linesText = null!;
    public static readonly TextBox textBox = new();
    public static readonly StringFormat stringFormat = new();
    public static Font boldFont = null!;
    public const int WM_KEYDOWN = 0x100, TAB_HEIGHT = 25;
    public static int indentW, qWidth, qHeight, upSideDownW, txtHeight;
    public static int screenWidth = 0, screenHeight = 0, prevHeight, prevWidth;
    public static List<Window> windows = new();
    public static bool dragging = false, doubleClick = false;
    public static List<(Button btn, Func<(int w, int h), Point> calcPos)> buttonsOnScreen = new();
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
            screenHeight - TAB_HEIGHT, 
            screenWidth / 2
        );

        AddTab(".Main", size: (windowWidth, windowHeight), pos: (0, TAB_HEIGHT), isFirst: true);
        AddTab("Main2", size: (windowWidth, windowHeight), pos: (windowWidth, TAB_HEIGHT));
        curWindow = windows[1];

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
    public void Resize_Event(object sender, EventArgs e){
        (int Width, int Height) WHTuple = (screenWidth, screenHeight) = (GetWidth(), GetHeight());
        if(screenHeight == 0 || screenWidth == 0){  return; }
        var (changeH, changeW) = (
            (float)(prevHeight - TAB_HEIGHT) / (screenHeight - TAB_HEIGHT),
            (float)prevWidth / screenWidth
        ); 
        
        foreach(var window in windows) {
            window.Size.height /= changeH;
            window.Size.width /= changeW;
            window.Pos.x /= changeW;
            window.Pos.y = TAB_HEIGHT + ((window.Pos.y - TAB_HEIGHT) / changeH);
        }
        RefreshConsole();
        foreach(var button in buttonsOnScreen) {
            button.btn.Location = button.calcPos(WHTuple);
        }

        (prevHeight, prevWidth) = (screenHeight, screenWidth);
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
            (int x, int y) mousePos = (Cursor.Position.X, Cursor.Position.Y);
            foreach(var window in windows) {
                bool inX = mousePos.x >= window.Pos.x && mousePos.x <= window.Pos.x + window.Size.width;
                bool inY = mousePos.y >= window.Pos.y && mousePos.y <= window.Pos.y + window.Size.height;
                if(inX && inY) {
                    if(window.Function.Equals(curFunc)) {
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
                    ChangeTab(window.Function.Button, dontDraw: dontDraw);
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
    public static void GetClosestForCaret() {
        if(lastCol is not null) {
            CursorPos.ChangeCol(Min((int)lastCol, linesText[CursorPos.Line].Length - 1));
        } else {
            lastCol = CursorPos.Col;
            CursorPos.ChangeCol(Min(CursorPos.Col, linesText[CursorPos.Line].Length - 1));
        }
    }
    public static void MouseBtnClick(bool refresh=true) {
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
            nonStatic.Invalidate();
        }
    }
    public static float GetDistW(int i) {
        return MeasureWidth(linesText[CursorPos.Line][..(i + 1)], boldFont);
    }
    public static int GetClickRow() {
        float topBar = nonStatic.RectangleToScreen(nonStatic.ClientRectangle).Top - nonStatic.Top;
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
    public static void DeleteSelection() {
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
    public static string GetSelectedText() {
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
    public static string GetSelectedLines() {
        if(selectedLine is null) {  return ""; }
        var res = new StringBuilder();
        var selectedLine_ = ((int line, int col))selectedLine!;
        selectedLine = null;
        if(CursorPos.Line == selectedLine_.line) {
            return linesText[CursorPos.Line];
        } 
        (int bigger, int smaller) = MaxMin(CursorPos.Line, selectedLine_.line);
        for(int i = smaller; i < bigger; i++) {
            res.AppendLine(linesText[i]);
        }
        res.Append(linesText[bigger]);
        return res.ToString();
    }
    public static (int, int) AddString(ReadOnlySpan<char> change, (int line, int col) pos) {
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

public record class BM_Middle(Bitmap Img, int Middle);
static class CursorPos {
    public static int Line{ get; private set; } = 0;
    public static int Col{ get; private set; } = -1;
    private static void RealignWondow(){
        int txtPos = CursorPos.Line * Form1.txtHeight;
        int pos = txtPos + curWindow.Offset; 
        if(pos < 0){
            curWindow.Offset = - txtPos;
        } else if(pos >= (int)curWindow.Size.height){
            curWindow.Offset = - (txtPos + Form1.txtHeight - (int)curWindow.Size.height);
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
    public bool AsPlainText = false;
    public SolidBrush txtBrush = textBrush;
    public Window(Function func) {  Function = func; }
}
public class Function {
    public List<string> LinesText = new(){ "" };
    public Bitmap? DisplayImage;
    public string Name = null!;
    public int CurLine = 0;
    public int CurCol = -1;
    public Button Button = null!;
    public bool isPic = false;
}
