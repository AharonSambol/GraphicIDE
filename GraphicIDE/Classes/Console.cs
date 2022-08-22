using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.Helpers;
using static GraphicIDE.MyImages;
using static GraphicIDE.DrawScreen;

namespace GraphicIDE;
public static class Console {

    public static void ShowConsole() {
        int idx = nonStatic.Controls.IndexOf(openConsoleBtn);
        if(idx != -1) {
            buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(openConsoleBtn!)));
            nonStatic.Controls[idx].Dispose();
        }
        openConsoleBtn = null;

        var (height, width) = (screenHeight, screenWidth);

        int consolePos = height - (height / 4);
        console.Pos.y = consolePos;
        console.Size.height = height - consolePos;
        console.Size.width = width;
        Bitmap img = new(width, height);
        using(var g = Graphics.FromImage(img)) {
            if(consoleTxt.typ == ConsoleTxtType.text) {
                console.Function.LinesText = consoleTxt.txt.Split('\n').ToList();
                g.DrawString(consoleTxt.txt, boldFont, textBrush, 0, 5);
                int end = 5 + MeasureHeight(consoleTxt.txt, boldFont);
                if(errOpenButton is not null) {
                    buttonsOnScreen.Remove(buttonsOnScreen.Find((x)=>x.btn.Equals(buttonsOnScreen)));
                    nonStatic.Controls[nonStatic.Controls.IndexOf(errOpenButton)].Dispose();
                    errOpenButton = null;
                }
            } else {
                console.Function.LinesText = consoleTxt.txt.Split('\n').ToList();

                g.DrawString(consoleTxt.txt, boldFont, redBrush, 0, 5);
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
                }
            }
        }
        console.Function.DisplayImage = img;
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
    }
    public static void RefreshTimeBtn(){
        int h = MeasureHeight(executedTime, boldFont);
        int w = MeasureWidth(executedTime, boldFont);
        Bitmap bm = new(w, h);
        using(var g = Graphics.FromImage(bm)){
            g.DrawString(executedTime, boldFont, timeBrush, 0, 0);
        }
        execTimeBtn!.Size = new(w, h);
        // var pos = buttonsOnScreen.Find((x)=>x.btn.Equals(execTimeBtn)).calcPos((Width, Height));
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

}
public enum ConsoleTxtType {   text, error }
