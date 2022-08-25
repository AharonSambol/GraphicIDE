using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.MyImages;
using static GraphicIDE.Helpers;
using static GraphicIDE.Tabs;
using static GraphicIDE.Console;

namespace GraphicIDE;

public static class Start{
    public static void AddRunBtn() {
        int gap = 3;
        Button btn = MakeButton(TAB_HEIGHT, TAB_HEIGHT, play3dImg/*just temp*/, streatch: false);
        Bitmap b = new(btn.Size.Width, btn.Size.Height);
        using(var g = Graphics.FromImage(b)) {
            Bitmap scaled = new(play3dImg, btn.Size.Width - gap * 2, btn.Size.Height - gap * 2);
            g.DrawImage(scaled, gap, gap);
        }
        btn.BackgroundImage = b;
        btn.Click += new EventHandler(PythonFuncs.ExecuteBtn!);
        buttonsOnScreen.Add((btn, (size) => new(size.w - btn.Size.Width - 10, 0)));
    }
    public static void AddDebugBtn() {
        Button btn = MakeButton(TAB_HEIGHT, TAB_HEIGHT, debugImg, streatch: true); 
        btn.Click += new EventHandler(PythonFuncs.ExecuteBtn!);
        buttonsOnScreen.Add((btn, (size) => new(size.w - 2 * (btn.Size.Width + 10), 0)));
    }
    public static void AddTabBtn() {
        Button btn = MakeButton(TAB_HEIGHT, TAB_HEIGHT, plusImg, streatch: true); 
        btn.Click += new EventHandler(AddTabEvent!);
        buttonsOnScreen.Add((btn, (size) => new(size.w - 3 * (btn.Size.Width + 10), 0)));
    }
    public static void RenameTabBtn() {
        Button btn = MakeButton(TAB_HEIGHT, TAB_HEIGHT, renameImg, streatch: true);        
        btn.Click += new EventHandler((_,_) => PromptRenameTab());
        nonStatic.Controls.Add(btn);
        buttonsOnScreen.Add((btn, (size) => new(size.w - 4 * (btn.Size.Width + 10), 0)));
    }
    public static void AddConsole() {
        var (height, width) = (screenHeight, screenWidth);
        int consolePos = height - (height / 4);
        Bitmap img = new(width, height - consolePos);
        var func = new Function(".console") { 
            DisplayImage = img,
        };
        console = new Window(func) {
            Pos = (0, consolePos),
            Size = (width, height - consolePos),
            AsPlainText = true,
            txtBrush = redBrush,
        };
        nameToFunc[func.Name] = console.Function;
    }
    public static void MakeExecTimeDisplay(){
        var w = MeasureWidth(executedTime, boldFont); 
        var h = txtHeight; 
        Bitmap bm = new(w, h);
        using(var g = Graphics.FromImage(bm)){
            g.DrawString(executedTime, boldFont, timeBrush, 0, 0);
        }
        execTimeBtn = MakeButton(w, h, bm, streatch: false);
        execTimeBtn.Click += new EventHandler((object? s, EventArgs e) => ToggleTimeBtn());
        buttonsOnScreen.Add((execTimeBtn, ETBCalcPos));
    }
    public static Button MakeButton(int w, int h, Bitmap img, bool streatch){
        Button btn = new() {
            Size = new(w, h),
            BackColor = Color.Transparent,
            BackgroundImage = img,
            FlatStyle = FlatStyle.Flat,
            BackgroundImageLayout = streatch ? ImageLayout.Stretch : ImageLayout.Zoom,
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.BorderColor = Color.White;
        btn.FlatAppearance.MouseOverBackColor = lightGray;
        nonStatic.Controls.Add(btn);
        return btn;
    }
    
    public static async void FocusTB(){
        await Task.Delay(100);
        textBox.Focus();
    }
    
}