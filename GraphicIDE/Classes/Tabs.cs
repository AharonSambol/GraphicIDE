using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.Helpers;
using static GraphicIDE.DrawScreen;

namespace GraphicIDE;

public static class Tabs {
    public const int TAB_HEIGHT = 25;
    public static Font tabFont = new(FontFamily.GenericMonospace, 10, FontStyle.Bold);
    public const int TAB_WIDTH = 80;
    public static readonly List<Button> tabButtons = new();
    public static int tabButtonEnd = 0;
    public static readonly Dictionary<string, Function> nameToFunc = new();
    public static Function curFunc = null!;
    public static Window curWindow = null!;
    public static void AddTabEvent(object? sender, EventArgs e) => PromptMakeNewTab();
    private static void AddTabToWindow(string name, Window window){
        Button btn = new() {
            Name = name,
            Text = name,
            BackColor = Color.LightGray,
            Location = new((int)window.Pos.x + window. tabsEnd, (int)window.Pos.y),
            Size = new(TAB_WIDTH, TAB_HEIGHT),
            Font = tabFont
        };
        btn.Click += new EventHandler((object? sender, EventArgs e) => ChangeTab(sender!, window));

        window.tabButtons.Add(btn);
        nonStatic.Controls.Add(btn);
        window.tabsEnd += btn.Width + 10;
    }
    private static void AddTabSelect(string name){
        foreach(var window in windows) {
            AddTabToWindow(name, window);
        }
    }
    public static Window MakeNewWindow(Function func, (int width, int height) size, (int x, int y) pos){
        Window window = new(func) { 
            Size = size, 
            Pos = pos,
        };
        curWindow = window;
        foreach(var tab in nameToFunc.Keys) {
            AddTabToWindow(tab, window);
        }
        windows.Add(window);
        return window;
    }
    public static Function NewFunc(string name, bool isfirst=false) {
        Function func = new(name);
        nameToFunc.Add(name, func);
        func.DisplayImage = new(screenWidth, screenHeight);
        AddTabSelect(name);
        if(!isfirst){   ChangeTab(name); }
        return func;
    }
    public static void ChangeTab(object sender, Window window){
        curWindow = window;
        ChangeTab(((Button)sender).Name);
    }
    public static void ChangeTab(string funcName, bool select=false, bool dontDraw=false) {
        var func = nameToFunc[funcName];
        if(selectedLine is not null) {
            if(!isShiftPressed()) {
                selectedLine = null;
            }
        }
        // todo curFunc.Button.BackColor = Color.LightGray;
        curFunc.CurLine = CursorPos.Line;
        curFunc.CurCol = CursorPos.Col;
        if(!isPic && !dontDraw) {    DrawPicScreen(); }
        if(!isPic && !dontDraw) {
            try {
                DrawTextScreen(false); nonStatic.Invalidate();
            } catch(Exception) { }
        }
        curTextBrush = curWindow.txtBrush;
        curFunc.isPic = isPic;
        // todo func.Button.BackColor = Color.WhiteSmoke;
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
            nonStatic.Invalidate();
        }
    }
    public static void PromptMakeNewTab() {
        TextBox textBox = new(){
            Multiline = true,
            Size = new(500, 40),
            Font = boldFont,
        };
        textBox.Location = new(
            (int)(screenWidth / 2 - textBox.Width / 2),
            (int)(screenHeight / 2 - textBox.Height / 2)
        );
        textBox.KeyDown += new KeyEventHandler(EnterNewTab!);
        nonStatic.Controls.Add(textBox);
        textBox.Focus();
    }
    public static void EnterNewTab(object sender, KeyEventArgs e) {
        if(e.KeyCode == Keys.Enter) {
            nonStatic.Controls.Remove((TextBox)sender);
            var name = ((TextBox)sender).Text;
            var func = NewFunc(name);
            curWindow.Function = func;
        } else if(e.KeyCode == Keys.Escape) {
            nonStatic.Controls.Remove((TextBox)sender);
        }
    }
}