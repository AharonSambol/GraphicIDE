using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.MyImages;
using static GraphicIDE.Helpers;
using static GraphicIDE.Tabs;
using static GraphicIDE.Console;
using static GraphicIDE.Settings;

namespace GraphicIDE;

public static class Start{
    public static Button RunBtn() {
        Button btn = MakeButton(TAB_HEIGHT - 6, TAB_HEIGHT - 6, play3dImg, streatch: false);
        btn.Click += new EventHandler(PythonFuncs.ExecuteBtn!);
        buttonsOnScreen.Add((btn, (size) => new(3 + size.w - TAB_HEIGHT - 10, 3)));
        toolTip.SetToolTip(btn, "run");
        return btn;
    }
    public static Button SettingsBtn() {
        Button btn = MakeButton(TAB_HEIGHT, TAB_HEIGHT, settingsImg, streatch: true);        
        btn.Click += new EventHandler((_,_) => ToggleSettings());
        nonStatic.Controls.Add(btn);
        buttonsOnScreen.Add((btn, (size) => new(size.w - 2 * (btn.Size.Width + 10), 0)));
        toolTip.SetToolTip(btn, "settings");
        return btn;
    }
    public static Button TabBtn() {
        Button btn = MakeButton(TAB_HEIGHT, TAB_HEIGHT, plusImg, streatch: true); 
        btn.Click += new EventHandler(AddTabEvent!);
        buttonsOnScreen.Add((btn, (size) => new(size.w - 3 * (btn.Size.Width + 10), 0)));
        toolTip.SetToolTip(btn, "new function\\tab");
        return btn;
    }
    public static Button RenameTabBtn() {
        Button btn = MakeButton(TAB_HEIGHT, TAB_HEIGHT, renameImg, streatch: true);        
        btn.Click += new EventHandler((_,_) => PromptRenameTab());
        nonStatic.Controls.Add(btn);
        buttonsOnScreen.Add((btn, (size) => new(size.w - 4 * (btn.Size.Width + 10), 0)));
        toolTip.SetToolTip(btn, "rename function");
        return btn;
    }
    public static Button TrashBtn() {
        Button btn = MakeButton(TAB_HEIGHT - 4, TAB_HEIGHT - 4, trashImg, streatch: true);        
        btn.Click += new EventHandler((_,_) => DeleteFunc(curFunc.name));
        nonStatic.Controls.Add(btn);
        buttonsOnScreen.Add((btn, (size) => new(2 + size.w - 5 * (TAB_HEIGHT + 10), 2)));
        toolTip.SetToolTip(btn, "delete function");
        return btn;
    }
    public static Button SaveBtn() {
        Button btn = MakeButton(TAB_HEIGHT - 4, TAB_HEIGHT - 4, saveImg, streatch: true);        
        btn.Click += new EventHandler((_,_) => Save(IsAltlPressed()));
        nonStatic.Controls.Add(btn);
        buttonsOnScreen.Add((btn, (size) => new(2 + size.w - 6 * (TAB_HEIGHT + 10), 2)));
        toolTip.SetToolTip(btn, "save file");
        return btn;
    }
    
    public static Button OpenBtn() {
        Button btn = MakeButton(TAB_HEIGHT, TAB_HEIGHT, openImg, streatch: true);        
        btn.Click += new EventHandler((_,_) => Open());
        nonStatic.Controls.Add(btn);
        buttonsOnScreen.Add((btn, (size) => new(size.w - 7 * (TAB_HEIGHT + 10), 0)));
        toolTip.SetToolTip(btn, "open file");
        return btn;
    }
    public static Button? unsavedButton;
    public static Button UnsavedButton() {
        unsavedButton = MakeButton(TAB_HEIGHT/2, TAB_HEIGHT/2, dotImg, streatch: true);
        buttonsOnScreen.Add((unsavedButton, (size) => new(5, size.h - unsavedButton.Size.Height - 5)));
        toolTip.SetToolTip(unsavedButton, "unsaved");
        return unsavedButton;
    }
    public static void AddConsole() {
        var (height, width) = (screenHeight, screenWidth);
        int consolePos = height - (height / 4);
        Bitmap img = new(width, height - consolePos);
        var func = new Function(".console") { 
            displayImage = img,
        };
        console = new Window(func) {
            pos = (0, consolePos),
            size = (width, height - consolePos),
            asPlainText = true,
            txtBrush = textBrush,
        };
        nameToFunc[func.name] = console.function;
    }
    public static Button MakeExecTimeDisplay(){
        var w = MeasureWidth(executedTime, boldFont); 
        var h = txtHeight; 
        Bitmap bm = new(w, h);
        using(var g = Graphics.FromImage(bm)){
            g.DrawString(executedTime, boldFont, timeBrush, 0, 0);
        }
        execTimeBtn = MakeButton(w, h, bm, streatch: false);
        execTimeBtn.Click += new EventHandler((object? s, EventArgs e) => ToggleTimeBtn());
        buttonsOnScreen.Add((execTimeBtn, ETBCalcPos));
        return execTimeBtn;
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