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
    public static void Open(){
        using(OpenFileDialog openFileDialog = new OpenFileDialog()){
            openFileDialog.FileName = "AnAwsomeProgram.py";
            openFileDialog.Filter = "Python File | *.py | Text File | *.txt";
            if (openFileDialog.ShowDialog() == DialogResult.OK){
                var filePath = openFileDialog.FileName;
                var fileStream = openFileDialog.OpenFile();
                string fileContent;
                using (StreamReader reader = new StreamReader(fileStream)){
                    fileContent = reader.ReadToEnd();
                }
                var funcs = fileContent.Split("\r\ndef");
                if(funcs[0].StartsWith("def")){
                    funcs[0] = funcs[0].Substring(3);
                }
                foreach(var func in funcs) {
                    var lines = func.Split(new string[]{"\r\n", "\n"}, StringSplitOptions.None);
                    if(func.Equals(funcs[^1])){
                        var newFunc = NewFunc(lines[0].Trim().TrimEnd(':'));
                        for (int i=1; i < lines.Length; i++) {
                            if(!lines[i].StartsWith("\t") && !lines[i].StartsWith("    ")){
                                newFunc.linesText = lines[1..i].Select((x)=>x.Substring(1).Replace("\t", "    ")).ToList();
                                nameToFunc[".Main"].linesText = lines[i..].Select((x)=>x.Replace("\t", "    ")).ToList();
                                break;
                            }
                        }
                    } else {
                        var newFunc = NewFunc(lines[0].Trim().TrimEnd(':'));
                        newFunc.linesText = lines[1..].Select((x)=>x.Substring(1).Replace("\t", "    ")).ToList();
                    }
                }
                nonStatic.Invalidate();
            }
        }
    }
    private static async void SaveAsync(){
        await File.WriteAllTextAsync(savePath!, GetPythonStr());
        unsavedButton!.Hide();
    }
}