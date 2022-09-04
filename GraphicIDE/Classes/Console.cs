namespace GraphicIDE;
public static class Console {
    public static (string txt, ConsoleTxtType typ)[] consoleTxt = new[]{("", ConsoleTxtType.text)};
    public static Window console = null!;
    public static string executedTime = "-------";
    public static bool isConsoleVisible = false;
    public static Button? closeConsoleBtn, openConsoleBtn, errOpenButton, execTimeBtn;
    public static readonly Func<(int w, int h), Point> 
        ETBCalcPos = (size) => new(size.w - execTimeBtn!.Width - 35 /*open console btn size*/ - 20, size.h - execTimeBtn!.Height - 5),
        CCBCalcPos = (_) => new((int)(console.pos.x + console.size.width) - closeConsoleBtn!.Width - 10, (int)console.pos.y + 10),
        EOBCalcPos = (_) => new((int)(console.pos.x + console.size.width) - errOpenButton!.Width - 10, (int)console.pos.y + 40),
        OCBCalcPos = (size) => new(size.w - openConsoleBtn!.Width - 10, size.h - openConsoleBtn.Height - 5);
    
    public static void ShowConsole() {
        int idx = nonStatic.Controls.IndexOf(openConsoleBtn);
        if(idx != -1) {
            buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(openConsoleBtn!)));
            nonStatic.Controls[idx].Dispose();
        }
        openConsoleBtn = null;

        var (height, width) = (screenHeight, screenWidth);

        int consolePos = height - (height / 4);
        console.pos.y = consolePos;
        console.size.height = height - consolePos;
        console.size.width = width;
        Bitmap img = new(width, height);
        using(var g = Graphics.FromImage(img)) {
            int end = 5;
            console.function.linesText = new();
            foreach(var item in consoleTxt) {
                console.function.linesText.AddRange(item.txt.Split('\n'));
                if(item.typ == ConsoleTxtType.text) {
                    g.DrawString(item.txt, boldFont, textBrush, 0, end);
                    end += MeasureHeight(item.txt, boldFont);
                    if(errOpenButton is not null) {
                        buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(errOpenButton)));
                        nonStatic.Controls[nonStatic.Controls.IndexOf(errOpenButton)].Dispose();
                        errOpenButton = null;
                    }
                } else {
                    g.DrawString(item.txt, boldFont, redBrush, 0, end);
                    end += MeasureHeight(item.txt, boldFont);
                    if(errOpenButton is null) {
                        errOpenButton = new() {
                            Size = new(20, 20),
                            BackColor = Color.Transparent,
                            BackgroundImage = searchImg,
                            FlatStyle = FlatStyle.Flat,
                            BackgroundImageLayout = ImageLayout.Zoom,
                        };
                        errOpenButton.Location = EOBCalcPos((0, 0));
                        errOpenButton.FlatAppearance.BorderSize = 0;
                        errOpenButton.FlatAppearance.BorderColor = Color.White;
                        errOpenButton.FlatAppearance.MouseOverBackColor = lightGray;
                        errOpenButton.Click += new EventHandler(OpenErrLink!);
                        nonStatic.Controls.Add(errOpenButton);
                        buttonsOnScreen.Add((errOpenButton, EOBCalcPos));
                        toolTip.SetToolTip(errOpenButton, "Google exception");
                    }
                }
            }
        }
        console.function.displayImage = img;
        isConsoleVisible = true;

        if(closeConsoleBtn is null) {
            closeConsoleBtn = new() {
                Size = new(20, 20),
                BackColor = Color.Transparent,
                BackgroundImage = xImg,
                FlatStyle = FlatStyle.Flat,
                BackgroundImageLayout = ImageLayout.Zoom,
            };
            closeConsoleBtn.Location = CCBCalcPos((0, 0));
            closeConsoleBtn.FlatAppearance.BorderSize = 0;
            closeConsoleBtn.FlatAppearance.BorderColor = Color.White;
            closeConsoleBtn.FlatAppearance.MouseOverBackColor = lightGray;
            closeConsoleBtn.Click += new EventHandler(HideConsole!);
            nonStatic.Controls.Add(closeConsoleBtn);
            buttonsOnScreen.Add((closeConsoleBtn, CCBCalcPos));
            toolTip.SetToolTip(closeConsoleBtn, "close console");
        }
        if(!windows[0].Equals(console)){
            windows.Add(windows[0]); windows[0] = console;
        }
        nonStatic.Invalidate();
    }
    public static void HideConsole(object? sender, EventArgs e) {
        isConsoleVisible = false;
        int idx = nonStatic.Controls.IndexOf(closeConsoleBtn);
        if(idx != -1) {
            buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(closeConsoleBtn!)));
            nonStatic.Controls[idx].Dispose();
        }
        closeConsoleBtn = null;
        idx = nonStatic.Controls.IndexOf(errOpenButton);
        if(idx != -1) {
            buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(errOpenButton!)));
            nonStatic.Controls[idx].Dispose();
        }
        errOpenButton = null;

        if(openConsoleBtn is null) {
            MakeOpenConsoleBtn();
        }
        windows.Remove(console);
        nonStatic.Invalidate();
    }
    public static void MakeOpenConsoleBtn() {
        openConsoleBtn = new() {
            Size = new(35, 35),
            BackColor = Color.Transparent,
            BackgroundImage = consoleImg,
            FlatStyle = FlatStyle.Flat,
            BackgroundImageLayout = ImageLayout.Zoom,
        };
        openConsoleBtn.Location = OCBCalcPos((screenWidth, screenHeight));
        openConsoleBtn.FlatAppearance.BorderSize = 0;
        openConsoleBtn.FlatAppearance.BorderColor = Color.White;
        openConsoleBtn.FlatAppearance.MouseOverBackColor = lightGray;
        openConsoleBtn.Click += new EventHandler((object? s, EventArgs e) => ShowConsole());
        nonStatic.Controls.Add(openConsoleBtn);
        buttonsOnScreen.Add((openConsoleBtn, OCBCalcPos));
        toolTip.SetToolTip(openConsoleBtn, "open console");
    }
    public static void RefreshTimeBtn(){
        int h = MeasureHeight(executedTime, boldFont);
        int w = MeasureWidth(executedTime, boldFont);
        Bitmap bm = new(w, h);
        using(var g = Graphics.FromImage(bm)){
            g.DrawString(executedTime, boldFont, timeBrush, 0, 0);
        }
        execTimeBtn!.Size = new(w, h);
        execTimeBtn!.Location = ETBCalcPos((screenWidth, screenHeight));
        execTimeBtn!.BackgroundImage = bm;
    }
    public static void ToggleTimeBtn(){
        if(execTimeBtn!.BackgroundImage is null){
            RefreshTimeBtn();
        } else {
            execTimeBtn!.BackgroundImage = null;
        }
    }
    public static void ToggleConsole() {
        if(isConsoleVisible) { HideConsole(null, new()); } else { ShowConsole(); }
    }
    public static void RefreshConsole() {
        if(isConsoleVisible) { ShowConsole(); } else { HideConsole(null, new()); }
    }

    public static void OpenErrLink(object? sender, EventArgs e) {
        if(PythonFuncs.errLink is not null) {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {PythonFuncs.errLink}") { CreateNoWindow = true });
        }
    }
}
public enum ConsoleTxtType {   text, error }
