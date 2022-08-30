using System.Text;

using static GraphicIDE.Form1;
using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.MyImages;
using static GraphicIDE.Start;
using static GraphicIDE.Tabs;
using static GraphicIDE.PythonFuncs;

namespace GraphicIDE;

public static class Settings {
    public static Menu? settingsScreen;
    public static Menu? staticSettingsScreen;
    public static string? savePath;
    public static void ToggleSettings(){
        int size = 80, gap = 10;

        bool addToControls = true;
        if(staticSettingsScreen is null){   
            MakeFirstSettingsScreen(); 
            addToControls = false;
        }
        
        int sizeH = size * 2 + gap * 3;
        int sizeW = size * 3 + gap * 4;
        (int X, int Y) location = (screenWidth/2 - sizeW/2, screenHeight/2 - sizeH/2);
        Rectangle bgPos = new(location.X, location.Y, sizeW, sizeH);

        foreach(var button in staticSettingsScreen!.Value.buttons) {
            button.btn.Location = button.getPos(location);
            if(addToControls){
                nonStatic.Controls.Add(button.btn);
            }
            button.btn.BringToFront();
        }

        settingsScreen = new(bgPos, staticSettingsScreen!.Value.bgColor, staticSettingsScreen.Value.buttons);
        visibleMenues.Add((Menu)settingsScreen);
        BlockMouse(staticSettingsScreen!.Value.buttons[0].btn, settingsScreen);
        nonStatic.Invalidate();
    }
    private static void MakeFirstSettingsScreen(){
        int size = 80, gap = 10;

        Button bg = MakeButton(40, 40, new(1, 1), streatch: false);
        bg.FlatAppearance.MouseOverBackColor = Color.Transparent;
        bg.MouseDown += new MouseEventHandler((object? _, MouseEventArgs e) => {
            if(e.Button == MouseButtons.Right){
                RightClick();
            } else {
                DisposeMenu(ref settingsScreen);
            }
        });

        Button lightmode = MakeButton(size, size, lightmodeImg, streatch: false);
        Func<(int X, int Y), Point> lightmodeGetPos = (pos) => new(pos.X + gap, pos.Y + gap);
        lightmode.Click += new EventHandler((_,_) => {
            DisposeMenu(ref settingsScreen);
            MessageBox.Show("no.");
        });
        toolTip.SetToolTip(lightmode, "light-mode");

        staticSettingsScreen = new(new(0,0,1,1), smokeWhiteBrush, new(){ 
            (bg, (_)=>new(0,0)),
            (lightmode, lightmodeGetPos),
        });
    }
    public static void Save(bool isShift){
        if(savePath is not null && !isShift){
            SaveAsync();
            return;
        }
        using(SaveFileDialog save = new SaveFileDialog()){
            save.FileName = "AnAwsomeProgram.py";
            save.Filter = "Python File | *.py | Text File | *.txt";
            if (save.ShowDialog() == DialogResult.OK){
                savePath = Path.GetFullPath(save.FileName);
                SaveAsync();
            }
        }
    }
    private static async void SaveAsync(){
        await File.WriteAllTextAsync(savePath!, GetPythonStr());
        unsavedButton!.Hide();
    }
}