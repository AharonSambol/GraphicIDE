using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Net;


using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.MyMath;
using static GraphicIDE.MyImages;
using static GraphicIDE.Helpers;
using static GraphicIDE.DrawScreen;
using static GraphicIDE.Start;
using static GraphicIDE.Tabs;
using static GraphicIDE.Console;
using static GraphicIDE.KeyInput;
using static GraphicIDE.History;
using static GraphicIDE.Settings;

// todo git
// todo add cheat sheat
// todo group words in ctrl z
// todo add in settings to change `history amount` (for ctrl+Z)
// todo let them make custom img for functions
// todo cache some of the textline images
// todo only draw visable lines
// todo add little icon to list/set.. comprehension to signal which type it is
// todo have file system on right so dont need to have all tabs open
// todo capslock shortcuts
// todo scroll horizontal
// todo comments w drawing
// todo when renaming func rename all calls too
// todo save/open (file explorer)
// todo syntax highlighting
// todo drag selection
// todo change `scaled images` when text size changes (just set them to null)
// todo when changing font size need to change pen sizes as well / just resize img
// todo function args
// todo print and exception
// todo add / move / resize windows
// ? del, global, *, assert, yield\yeild from, with, formatStr, finally, for-else
// ? dict + generator(+comprehension)
// ? indexing + slicing
// ? import from\as
// ? classes(+attributes)
// ? try except raise
// ? annotations
// ? break, continue
// ? lambda 
#region built in funcs
// aiter() anext() breakpoint() bytearray() bytes() callable() classmethod() compile() dir() frozenset() memoryview() property() repr() staticmethod() super() vars()
// dict() set()
// chr() ord()
// range() enumerate() zip()
// all() any()
// map()
// type()
// reversed() sorted()
// help()
// open()
// next()
// eval() exec()
// globals() locals()
// bin()
// id()
// ascii()
// iter()
// slice()
// object()
// oct() hex()
// complex()
// delattr() getattr() hasattr() setattr()
// format()
// isinstance() issubclass()
#endregion

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
    public static HashSet<Menu> visibleMenues = new();
    public static bool dragging = false, doubleClick = false;
    // todo convert to linked list
    public static List<(Button btn, Func<(int w, int h), Point> calcPos)> buttonsOnScreen = new();
    public static Point screenPos;
    public static Form1 nonStatic = null!;
    public static ToolTip toolTip = new ToolTip(){
        AutoPopDelay = 5000,
        InitialDelay = 1000,
        ReshowDelay = 500,
        ShowAlways = true,
        BackColor = Color.WhiteSmoke
    };
    #endregion

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
        var func2 = NewFunc("Main2()", isfirst: true);
        var window2 = MakeNewWindow(func2, size: (windowWidth, windowHeight), pos: (windowWidth, 0));
        curWindow = mainWindow;
        curFunc = mainFunc;
        ChangeTab(curFunc.name, prevWindow: window2);

        var run = AddRunBtn();
        toolTip.SetToolTip(run, "run");
        var debug = AddDebugBtn();
        toolTip.SetToolTip(debug, "debug");
        var tab = AddTabBtn();
        toolTip.SetToolTip(tab, "new function\\tab");
        var rename = RenameTabBtn();
        toolTip.SetToolTip(rename, "rename function");
        var settings = SettingsBtn();
        toolTip.SetToolTip(settings, "settings");

        AddConsole();
        var execTime = MakeExecTimeDisplay();
        toolTip.SetToolTip(execTime, "Executed time");

        DrawTextScreen();
        Invalidate();

        this.WindowState = FormWindowState.Maximized;

        FocusTB();
    }

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
            window.size.height /= changeH;
            window.size.width /= changeW;
            window.pos.x /= changeW;
            window.pos.y = window.pos.y / changeH;
            for (int i=0; i < window.tabButtons.Count; i++) {
                var btn = window.tabButtons[i];
                btn.Location = new((int)window.pos.x + (TAB_WIDTH + 10) * i, (int)window.pos.y);
                var a = btn.Location;
            }
        }
        RefreshConsole();
        foreach(var button in buttonsOnScreen) {
            button.btn.Location = button.calcPos(WHTuple);
        }

        if(visablePrompt is Prompt p){
            p.tb.Location = new(
                (int)(screenWidth / 2 - p.tb.Width / 2),
                (int)(screenHeight / 2 - p.tb.Height / 2)
            );
        }
        if(rightClickMenu is not null){
            DisposeMenu(ref rightClickMenu);
        }

        (prevHeight, prevWidth) = (screenHeight, screenWidth);
        Invalidate();
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
        var keyCode = (Keys) (msg.WParam.ToInt32() & Convert.ToInt32(Keys.KeyCode));
        if(msg.Msg == WM_KEYDOWN && ModifierKeys == Keys.Control) {
            bool isAltlKeyPressed = IsAltlPressed();
            bool isShift = IsShiftPressed();
            bool refresh = true;
            ((Action)(keyCode switch {
                Keys.Delete => () => DeleteKey(isAltlKeyPressed, true),
                Keys.Back => () => BackSpaceKey(isAltlKeyPressed, true),
                Keys.Enter => () => EnterKey(true),
                Keys.End => () => EndKey(isShift, true),
                Keys.Home => () => HomeKey(isShift, true),
                Keys.Up => () => ChangeOffsetTo(curWindow.offset + txtHeight),
                Keys.Down => () => ChangeOffsetTo(curWindow.offset - txtHeight),
                Keys.Right => () => RightKey(isShift, isAltlKeyPressed, true),
                Keys.Left => () => LeftKey(isShift, isAltlKeyPressed, true),
                Keys.C => () => Copy(isAltlKeyPressed),
                Keys.V => () => Paste(),
                Keys.X => () => Cut(isAltlKeyPressed),
                Keys.D => () => Duplicate(isAltlKeyPressed),
                Keys.A => () => SelectAll(),
                Keys.N => () => PromptMakeNewTab(),
                Keys.R => () => PromptRenameTab(),
                Keys.Z => () => CtrlZ(),
                Keys.Y => () => CtrlY(),
                Keys.S => () => Save(),
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
    public static Menu? rightClickMenu;
    private static Menu? staticRightClickMenu;
    public static void RightClick(){
        int size = 80, gap = 10;

        bool addToControls = true;
        if(staticRightClickMenu is null){   
            MakeFirstRCMenu(); 
            addToControls = false;
        }

        (int x, int y) mouse = (
            Cursor.Position.X - screenPos.X,
            Cursor.Position.Y - screenPos.Y
        );
        (int X, int Y) location = (mouse.x, mouse.y);
        
        int sizeH = size * 2 + gap * 3;
        int sizeW = size * 3 + gap * 4;
        Rectangle bgPos = new(location.X, location.Y, sizeW, sizeH);

        foreach(var button in staticRightClickMenu!.Value.buttons) {
            button.btn.Location = button.getPos(location);
            if(addToControls){
                nonStatic.Controls.Add(button.btn);
            }
            button.btn.BringToFront();
        }

        rightClickMenu = new(bgPos, staticRightClickMenu!.Value.bgColor, staticRightClickMenu.Value.buttons);
        visibleMenues.Add((Menu)rightClickMenu);
        BlockMouse(staticRightClickMenu!.Value.buttons[0].btn, rightClickMenu);
        nonStatic.Invalidate();
    }
    private static void MakeFirstRCMenu(){
        int size = 80, gap = 10;

        Button bg = MakeButton(40, 40, new(1, 1), streatch: false);
        bg.FlatAppearance.MouseOverBackColor = Color.Transparent;
        bg.MouseDown += new MouseEventHandler((object? _, MouseEventArgs e) => {
            if(e.Button == MouseButtons.Right){
                DisposeMenu(ref rightClickMenu);
                RightClick();
            } else {
                DisposeMenu(ref rightClickMenu);
            }
        });

        Button copy = MakeButton(size, size, copyImg, streatch: false);
        Func<(int X, int Y), Point> copyGetPos = (pos) => new(pos.X + gap, pos.Y + gap);
        copy.Click += new EventHandler((_,_) => {
            DisposeMenu(ref rightClickMenu);
            Copy(IsAltlPressed());
        });
        toolTip.SetToolTip(copy, "copy");

        Button paste = MakeButton(size, size, pasteImg, streatch: false);
        Func<(int X, int Y), Point> pasteGetPos = (pos) => new(pos.X + 2 * gap + 1 * size, pos.Y + gap);
        paste.Click += new EventHandler((_,_) => {
            DisposeMenu(ref rightClickMenu);
            Paste();
            DrawTextScreen();
            nonStatic.Invalidate();
        });
        toolTip.SetToolTip(paste, "paste");

        Button search = MakeButton(size, size, searchImg, streatch: false);
        Func<(int X, int Y), Point> searchGetPos = (pos) => new(pos.X + gap, pos.Y + 2 * gap + size);
        search.Click += new EventHandler((_,_) => {
            DisposeMenu(ref rightClickMenu);
            string txt = (
                selectedLine is null 
                ? linesText[CursorPos.line] 
                : GetSelectedText()
            ).Trim();
            if(!txt.Equals("")) {
                Process.Start(
                    new ProcessStartInfo(
                        "cmd", 
                        $"/c start https://www.google.com/search?q=Python{ WebUtility.UrlEncode(" " + txt) }"
                    ) { CreateNoWindow = true }
                );
            }
        });
        toolTip.SetToolTip(search, "Google selected");

        Button terminal = MakeButton(size, size, consoleImg, streatch: false);
        Func<(int X, int Y), Point> terminalGetPos = (pos) => new(pos.X + 2 * gap + size, pos.Y + 2 * gap + size);
        terminal.Click += new EventHandler((_,_) => {
            DisposeMenu(ref rightClickMenu);
            Process.Start("CMD.exe");
        });
        toolTip.SetToolTip(terminal, "open cmd");

        Button run = MakeButton(size, size, play3dImg, streatch: false);
        Func<(int X, int Y), Point> runGetPos = (pos) => new(pos.X + 3 * gap + 2 * size, pos.Y + gap);
        run.Click += new EventHandler((_,_) => {
            DisposeMenu(ref rightClickMenu);
            PythonFuncs.Execute();
        });
        toolTip.SetToolTip(run, "run");

        Button rename = MakeButton(size, size, renameImg, streatch: false);
        Func<(int X, int Y), Point> renameGetPos = (pos) => new(pos.X + 3 * gap + 2 * size, pos.Y + 2 * gap + size);
        rename.Click += new EventHandler((_,_) => {
            DisposeMenu(ref rightClickMenu);
            // Todo
        });
        toolTip.SetToolTip(rename, "rename");

        staticRightClickMenu = new(new(0,0,1,1), sandyBrush, new(){ 
            (bg, (_)=>new(0,0)),
            (copy, copyGetPos), (paste, pasteGetPos), (search, searchGetPos), 
            (terminal, terminalGetPos), (run, runGetPos), (rename, renameGetPos)
        });
    }
    public static async void BlockMouse(Button btn, Menu? screen){
        int w = btn.Size.Width / 2, h = btn.Size.Height / 2;
        while(screen is not null){
            await Task.Delay(10);
            btn.Location = new(Cursor.Position.X - screenPos.X - w, Cursor.Position.Y - screenPos.Y- h);
        }
    }
    public static void DisposeMenu(ref Menu? menu){
        if(menu is Menu m){
            foreach(var item in m.buttons) {
                nonStatic.Controls.Remove(item.btn);
            }
            visibleMenues.Remove(m);
        }
        menu = null;
        nonStatic.Invalidate();
    }
    protected override void OnMouseWheel(MouseEventArgs e) {
        if(curWindow.function.displayImage!.Height > 40) {
            ChangeOffsetTo(curWindow.offset + e.Delta / 10);
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

        GoInDirCtrl(GetNextL, IsAltlPressed());
        selectedLine = (CursorPos.line, CursorPos.col);
        GoInDirCtrl(GetNextR, IsAltlPressed());

        DrawTextScreen();
        Invalidate();
        base.OnMouseDoubleClick(e);
    }
    public void ClickedSelected((int line, int col) pos, (int,int) sel) {
        var newSelectedLine = (CursorPos.line, -1);
        var newCurCol = linesText[CursorPos.line].Length - 1;
        if(newCurCol == pos.col && newSelectedLine == sel) {
            GoInDirCtrl(GetNextL, IsAltlPressed());
            selectedLine = (CursorPos.line, CursorPos.col);
            GoInDirCtrl(GetNextR, IsAltlPressed());
        } else {
            CursorPos.ChangeCol(newCurCol);
            selectedLine = newSelectedLine;
        }
        DrawTextScreen();
        Invalidate();
    }
    async void Drag(MouseEventArgs e) {
        if(rightClickMenu is not null){
            DisposeMenu(ref rightClickMenu);
        }
        (int line, int col)? tempSelectedLine = null;
        if(e.Button == MouseButtons.Left) {
            (int x, int y) mousePos = (
                Cursor.Position.X - screenPos.X, 
                Cursor.Position.Y - screenPos.Y
            );
            foreach(var window in windows) {
                bool inX = mousePos.x >= window.pos.x && mousePos.x <= window.pos.x + window.size.width;
                bool inY = mousePos.y >= window.pos.y && mousePos.y <= window.pos.y + window.size.height;
                if(inX && inY) {
                    if(window.Equals(curWindow)) {
                        var prev = (CursorPos.line, CursorPos.col);
                        var prevSel = selectedLine;
                        MouseBtnClick(refresh: false);
                        if(prevSel is (int, int) ps && InBetween((CursorPos.line, CursorPos.col), prev, ps)) {
                            ClickedSelected(prev, ps);
                            return;
                        }
                        tempSelectedLine = (CursorPos.line, CursorPos.col);
                        break;
                    }
                    bool dontDraw = curWindow.asPlainText;
                    var prevWindow = curWindow;
                    curWindow = window;
                    ChangeTab(window.function.name, prevWindow, dontDraw: dontDraw);
                    break;
                }
            }
        } else if(e.Button == MouseButtons.Middle) {
            PythonFuncs.Execute();
            return;
        } else if(e.Button == MouseButtons.Right) {
            RightClick();
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
        if(!IsShiftPressed() || selectedLine is null) {
            selectedLine = tempSelectedLine!;
        }
        while(dragging) {
            MouseBtnClick();
            await Task.Delay(1);
        }
    }
    public static void MouseBtnClick(bool refresh=true) {
        if(selectedLine is not null) {
            if(!IsShiftPressed() && !dragging) {
                selectedLine = null;
            }
        }
        CursorPos.ChangeLine(GetClickRow());
        CursorPos.ChangeCol(BinarySearch(
            linesText[CursorPos.line].Length, 
            Cursor.Position.X - curWindow.pos.x - screenPos.X, 
            GetDistW
        ));
        textBox.Focus();
        if(refresh) {
            DrawTextScreen();
            nonStatic.Invalidate();
        }
    }
    public static int GetClickRow() {
        double mouse = Cursor.Position.Y - (curWindow.pos.y + curWindow.offset) - screenPos.Y - TAB_HEIGHT;
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
        return MeasureWidth(linesText[CursorPos.line][..(i + 1)], boldFont);
    }
    public static (int line, int col, char val)? GetNextR() {
        if(CursorPos.col != linesText[CursorPos.line].Length - 1) {
            return (CursorPos.line, CursorPos.col + 1, linesText[CursorPos.line][CursorPos.col + 1]);
        }
        if(CursorPos.line == linesText.Count - 1) {
            return null;
        }
        return (CursorPos.line + 1, -1, '\n');
    }
    public static (int line, int col, char val)? GetNextL() {
        if(CursorPos.col != - 1) {
            return (CursorPos.line, CursorPos.col - 1, linesText[CursorPos.line][CursorPos.col]);
        }
        if(CursorPos.line == 0) {
            return null;
        }
        return (CursorPos.line - 1, linesText[CursorPos.line - 1].Length - 1, '\n');
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

public record struct BM_Middle(Bitmap Img, int Middle);
public record struct Menu(Rectangle bgPos, SolidBrush bgColor, List<(Button btn, Func<(int X, int Y), Point> getPos)> buttons);
static class CursorPos {
    public static int line{ get; private set; } = 0;
    public static int col{ get; private set; } = -1;
    private static void RealignWondow(){
        int txtPos = CursorPos.line * Form1.txtHeight;
        int pos = txtPos + curWindow.offset; 
        if(pos < 0){
            curWindow.offset = - txtPos;
        } else if(pos >= (int)curWindow.size.height - TAB_HEIGHT - (int)(Form1.txtHeight/0.8)){
            curWindow.offset = - (txtPos + Form1.txtHeight - (int)curWindow.size.height + TAB_HEIGHT);
        }
        // TODO for col too
    }
    public static void ChangeLine(int i){
        CursorPos.line = i;
        RealignWondow();
    }
    public static void ChangeCol(int i){
        CursorPos.col = i;
        RealignWondow();
    }
    public static void ChangeBoth((int line, int col) val){
        (CursorPos.line, CursorPos.col) = val;
        RealignWondow();
    }
    public static (int line, int col) ToTuple(){
        return (CursorPos.line, CursorPos.col);
    }
}
public class Window {
    public Function function;
    public (float width, float height) size;
    public (float x, float y) pos;
    public int offset = 0;
    public bool isPic = false;
    public int tabsEnd = 0;
    public readonly List<Button> tabButtons = new();
    public Button? selectedTab;
    public bool asPlainText = false;
    public SolidBrush txtBrush = textBrush;
    public Window(Function func) {  
        function = func; 
    }
}
public class Function {
    public List<string> linesText = new(){ "" };
    public Bitmap? displayImage;
    public int curLine = 0;
    public int curCol = -1;
    public bool isPic = false;
    public string name;
    public Function(string name){
        this.name = name;
    }
}
