using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.MyImages;
using static GraphicIDE.Helpers;
using static GraphicIDE.Tabs;
using static GraphicIDE.KeyInput;
using static GraphicIDE.Console;

namespace GraphicIDE;

public static class Start{
    public static void AddRunBtn() {
        int gap = 5;
        Button run = new(){
            BackColor = tabBarColor,
            Size = new(TAB_HEIGHT, TAB_HEIGHT),
            BackgroundImageLayout = ImageLayout.None,
            FlatStyle = FlatStyle.Flat,
        };
        run.FlatAppearance.BorderSize = 0;
        run.FlatAppearance.BorderColor = Color.White;
        run.FlatAppearance.MouseOverBackColor = lightGray;
        Bitmap b = new(run.Size.Width, run.Size.Height);
        using(var g = Graphics.FromImage(b)) {
            Bitmap scaled = new(playImg, run.Size.Width - gap * 2, run.Size.Height - gap * 2);
            g.DrawImage(scaled, gap, gap);
        }
        run.BackgroundImage = b;
        run.Click += new EventHandler(PythonFuncs.ExecuteBtn!);
        nonStatic.Controls.Add(run);
        buttonsOnScreen.Add((run, (size) => new(size.w - run.Size.Width - 10, 0)));
    }
    public static void AddDebugBtn() {
        Button run = new(){
            BackColor = tabBarColor,
            Size = new(TAB_HEIGHT, TAB_HEIGHT),
            BackgroundImageLayout = ImageLayout.None,
            FlatStyle = FlatStyle.Flat,
        };
        run.FlatAppearance.BorderSize = 0;
        run.FlatAppearance.BorderColor = Color.White;
        run.FlatAppearance.MouseOverBackColor = lightGray;
        Bitmap b = new(run.Size.Width, run.Size.Height);
        using(var g = Graphics.FromImage(b)) {
            Bitmap scaled = new(debugImg, run.Size.Width, run.Size.Height);
            g.DrawImage(scaled, 0, 0);
        }
        run.BackgroundImage = b;
        run.Click += new EventHandler(PythonFuncs.ExecuteBtn!);
        nonStatic.Controls.Add(run);
        buttonsOnScreen.Add((run, (size) => new(size.w - 2 * run.Size.Width - 20, 0)));
    }
    public static  void AddTabBtn() {
        Button run = new(){
            BackColor = tabBarColor,
            Size = new(TAB_HEIGHT, TAB_HEIGHT),
            BackgroundImageLayout = ImageLayout.None,
            FlatStyle = FlatStyle.Flat,
        };
        run.FlatAppearance.BorderSize = 0;
        run.FlatAppearance.BorderColor = Color.White;
        run.FlatAppearance.MouseOverBackColor = lightGray;
        Bitmap b = new(run.Size.Width, run.Size.Height);
        using(var g = Graphics.FromImage(b)) {
            Bitmap scaled = new(plusImg, run.Size.Width, run.Size.Height);
            g.DrawImage(scaled, 0, 0);
        }
        run.BackgroundImage = b;
        run.Click += new EventHandler(AddTabEvent!);
        nonStatic.Controls.Add(run);
        buttonsOnScreen.Add((run, (size) => new(size.w - 3 * run.Size.Width - 30, 0)));
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
        execTimeBtn = new() {
            Size = new(w, h),
            BackColor = Color.Transparent,
            BackgroundImage = bm,
            FlatStyle = FlatStyle.Flat,
            BackgroundImageLayout = ImageLayout.Zoom,
        };
        execTimeBtn.FlatAppearance.BorderSize = 0;
        execTimeBtn.FlatAppearance.BorderColor = Color.White;
        execTimeBtn.FlatAppearance.MouseOverBackColor = lightGray;
        execTimeBtn.Click += new EventHandler((object? s, EventArgs e) => ToggleTimeBtn());
        nonStatic.Controls.Add(execTimeBtn);
        buttonsOnScreen.Add((execTimeBtn, ETBCalcPos));
    }
    
    public static async void FocusTB(){
        await Task.Delay(100);
        textBox.Focus();
        // CtrlTab();
    }
    
}