using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.Helpers;
using static GraphicIDE.DrawScreen;

namespace GraphicIDE;

public static class Tabs {
    public static readonly List<Button> tabButtons = new();
    public static int tabButtonEnd = 0;
    public static readonly Dictionary<string, Function> nameToFunc = new();
    public static Function curFunc = null!;
    public static Window curWindow = null!;
    public static void AddTabEvent(object? sender, EventArgs e){
        MakeNewTab();
    }

    public static void AddTab(string name, (int width, int height) size, (int x, int y) pos, bool isFirst=false) {
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
        nonStatic.Controls.Add(btn);
        tabButtonEnd += btn.Width + 10;
    }
    public static void ChangeTab(object sender, EventArgs e) =>
        ChangeTab((Button)sender, select: true);
    public static void ChangeTab(Button btn, bool select=false, bool dontDraw=false) {
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
        if(!isPic && !dontDraw) {    DrawPicScreen(); }
        if(!isPic && !dontDraw) {
            try {
                DrawTextScreen(false); nonStatic.Invalidate();
            } catch(Exception) { }
        }
        curTextBrush = curWindow.txtBrush;
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
            nonStatic.Invalidate();
        }
    }
    public static void MakeNewTab() {
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
            windows = windows.FindAll((x) => x.Pos.x != 0);
            AddTab(((TextBox)sender).Text, size:(screenWidth, screenHeight), pos: (0, TAB_HEIGHT));
        } else if(e.KeyCode == Keys.Escape) {
            nonStatic.Controls.Remove((TextBox)sender);
        }
    }
}