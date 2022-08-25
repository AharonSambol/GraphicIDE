using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.Helpers;
using static GraphicIDE.MyMath;
using static GraphicIDE.DrawScreen;
using static GraphicIDE.Start;
using static GraphicIDE.MyImages;

namespace GraphicIDE;

public static class Tabs {
    public const int TAB_HEIGHT = 25, TAB_WIDTH = 80;
    public static Font tabFont = new(FontFamily.GenericMonospace, 10, FontStyle.Bold);
    public static readonly Dictionary<string, Function> nameToFunc = new();
    public static Prompt? visablePrompt;
    public static Function curFunc = null!;
    public static Window curWindow = null!;
    public static void AddTabEvent(object? sender, EventArgs e) => PromptMakeNewTab();
    private static Button AddTabToWindow(string name, Window window, bool isFirst=false){
        Button btn = new() {
            Name = name,
            Text = name,
            BackColor = lightGray,
            Location = new((int)window.pos.x + window. tabsEnd, (int)window.pos.y),
            Size = new(TAB_WIDTH, TAB_HEIGHT),
            Font = tabFont,
            FlatStyle = FlatStyle.Flat,
        };
        btn.FlatAppearance.BorderColor = lightGray;
        btn.Click += new EventHandler((object? sender, EventArgs e) => ChangeTab(sender!, window));

        window.tabButtons.Add(btn);
        nonStatic.Controls.Add(btn);
        window.tabsEnd += btn.Width + 10;
        if(window.Equals(curWindow) && !isFirst){
            if(window.selectedTab is not null){
                window.selectedTab.BackColor = window.selectedTab.FlatAppearance.BorderColor = lightGray;
            } 
            btn.BackColor = btn.FlatAppearance.BorderColor = Color.DarkGray;
            window.selectedTab = btn;
        }
        return btn;
    }
    private static void AddTabSelect(string name, bool isFirst=false){
        foreach(var window in windows) {
            if(window.function.name.Equals(".console")){    continue; }
            AddTabToWindow(name, window, isFirst);
        }
    }
    public static Window MakeNewWindow(Function func, (int width, int height) size, (int x, int y) pos){
        Window window = new(func) { 
            size = size, 
            pos = pos,
        };
        curWindow = window;
        foreach(var tab in nameToFunc.Keys) {
            var btn = AddTabToWindow(tab, window);
            if(tab.Equals(func.name)){
                window.selectedTab = btn;
                btn.BackColor = btn.FlatAppearance.BorderColor = Color.DarkGray;
            }

        }
        windows.Add(window);
        return window;
    }
    public static Function NewFunc(string name, bool isfirst=false) {
        Function func = new(name);
        nameToFunc.Add(name, func);
        func.displayImage = new(screenWidth, screenHeight);
        AddTabSelect(name, isfirst);
        if(!isfirst){   ChangeTab(name, curWindow); }
        return func;
    }
    public static void ChangeTab(object sender, Window window){
        if(window.selectedTab is not null){
            window.selectedTab.BackColor = window.selectedTab.FlatAppearance.BorderColor = lightGray;
        }
        var clickedBtn = (Button)sender;
        window.selectedTab = clickedBtn;
        clickedBtn.BackColor = clickedBtn.FlatAppearance.BorderColor = Color.DarkGray;

        var prevWindow = curWindow;
        curWindow = window;
        ChangeTab(clickedBtn.Name, prevWindow: prevWindow);
    }
    public static void ChangeTab(string funcName, Window prevWindow, bool dontDraw=false) {
        var func = nameToFunc[funcName];
        if(selectedLine is not null) {
            if(!IsShiftPressed()) {
                selectedLine = null;
            }
        }

        curFunc.curLine = CursorPos.line;
        curFunc.curCol = CursorPos.col;
        var actualWindow = curWindow;
        curWindow = prevWindow;
        if(!curFunc.isPic && !dontDraw) {    DrawPicScreen(); }
        if(!curFunc.isPic && !dontDraw) {
            try {
                DrawTextScreen(false); nonStatic.Invalidate();
            } catch(Exception) { }
        }
        curWindow = actualWindow;
        curTextBrush = curWindow.txtBrush;
        // curFunc.isPic = isPic;
        curFunc = func;
        curWindow.function = func;
        linesText = func.linesText;
        CursorPos.ChangeLine(func.curLine);
        CursorPos.ChangeCol(func.curCol);
        // curFunc.isPic = func.isPic;
        skipDrawNewScreen = false;
        textBox.Focus();
        
        DrawTextScreen();
        nonStatic.Invalidate();
    }
    public static Prompt? newTabPrompt;
    public static void PromptMakeNewTab() {
        if(visablePrompt is not null){   return; }
        if(curFunc.name.Equals(".console")){   return; }

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

        var title = "New function:";
        var titleW = MeasureWidth(title, boldFont);
        var titleH = MeasureHeight(title, boldFont);
        Bitmap bm = new(Max(textBox.Width, titleW) + 40, textBox.Height + titleH + 60);
        using(var g = Graphics.FromImage(bm)){
            g.Clear(Color.LightGray);
            g.DrawString(title, boldFont, blackBrush, (int)(bm.Width / 2 - titleW / 2), 20);
        }

        Func<(int w, int h), Point> getPos = (size) => new(
            (int)(screenWidth / 2 + bm.Width / 2 - 40), 
            (int)(screenHeight / 2 - bm.Height + textBox.Height / 2 + 30)
        );
        Button cancel = MakeButton(30, 30, xImg, streatch: true);
        cancel.Location = getPos((screenWidth, screenHeight));
        cancel.Click += new EventHandler((object? s, EventArgs e) => DisposePrompt(ref newTabPrompt));
        buttonsOnScreen.Add((cancel, getPos));

        visablePrompt = newTabPrompt = new(bm, textBox, cancel);
        nonStatic.Invalidate();
    }
    public static void EnterNewTab(object sender, KeyEventArgs e) {
        if(e.KeyCode == Keys.Enter) {
            var name = ((TextBox)sender).Text;
            var func = NewFunc(name);
            curWindow.function = func;
            DisposePrompt(ref newTabPrompt);
        } else if(e.KeyCode == Keys.Escape) {
            DisposePrompt(ref newTabPrompt);
        }
    }
    public static Prompt? renamePrompt;
    public static void PromptRenameTab(){
        if(visablePrompt is not null){   return; }
        if(curFunc.name.StartsWith('.')){   return; }
        
        TextBox textBox = new(){
            Multiline = true,
            Size = new(500, 40),
            Font = boldFont,
        };
        textBox.Location = new(
            (int)(screenWidth / 2 - textBox.Width / 2),
            (int)(screenHeight / 2 - textBox.Height / 2)
        );
        textBox.KeyDown += new KeyEventHandler(RenameTab!);
        nonStatic.Controls.Add(textBox);
        textBox.Focus();

        var title = $"Rename `{ curFunc.name }` to:";
        var titleW = MeasureWidth(title, boldFont);
        var titleH = MeasureHeight(title, boldFont);
        Bitmap bm = new(Max(textBox.Width, titleW) + 40, textBox.Height + titleH + 60);
        using(var g = Graphics.FromImage(bm)){
            g.Clear(Color.LightGray);
            g.DrawString(title, boldFont, blackBrush, (int)(bm.Width / 2 - titleW / 2), 20);
        }

        Func<(int w, int h), Point> getPos = (size) => new(
            (int)(screenWidth / 2 + bm.Width / 2 - 40), 
            (int)(screenHeight / 2 - bm.Height + textBox.Height / 2 + 30)
        );
        Button cancel = MakeButton(30, 30, xImg, streatch: true);
        cancel.Location = getPos((screenWidth, screenHeight));
        cancel.Click += new EventHandler((object? s, EventArgs e) => DisposePrompt(ref renamePrompt));
        buttonsOnScreen.Add((cancel, getPos));

        visablePrompt = renamePrompt = new(bm, textBox, cancel);
        nonStatic.Invalidate();
    }
    private static void DisposePrompt(ref Prompt? prompt){
        if(prompt is Prompt rp){
            nonStatic.Controls.Remove(rp.tb);
            nonStatic.Controls.Remove(rp.cancel);
            buttonsOnScreen.Remove(buttonsOnScreen.Find((x) => x.btn.Equals(rp.cancel)));
            rp.tb.Dispose();
            rp.bm.Dispose();
            rp.cancel.Dispose();
            prompt = visablePrompt = null;
            nonStatic.Invalidate();
        }
    }
    public static void RenameTab(object sender, KeyEventArgs e){
        if(e.KeyCode == Keys.Enter) {
            var name = renamePrompt!.Value.tb.Text;
            DisposePrompt(ref renamePrompt);
            nameToFunc.Remove(curFunc.name);
            nameToFunc[name] = curFunc;
            foreach(var window in windows) {
                foreach(var tab in window.tabButtons) {
                    if(tab.Name.Equals(curFunc.name)){
                        tab.Name = name;
                        tab.Text = name;
                    }
                }
            }
            curFunc.name = name;
        } else if(e.KeyCode == Keys.Escape) {
            DisposePrompt(ref renamePrompt);
        }
    }
}
public record struct Prompt(Bitmap bm, TextBox tb, Button cancel);