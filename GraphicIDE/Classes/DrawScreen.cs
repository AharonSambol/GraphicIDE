using System.Diagnostics;

using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.MyMath;
using static GraphicIDE.Helpers;
using static GraphicIDE.Form1;
using static GraphicIDE.AST;
using static GraphicIDE.Tabs;

namespace GraphicIDE;

public static class DrawScreen{
    public const int LINE_HEIGHT = 30;
    public static bool isPic = false, skipDrawNewScreen = false;
    public static void DrawPicScreen() {
        if(isPic) {
            DrawTextScreen();
            nonStatic.Invalidate();
            isPic = false;
        } else {
            try {
                var bm = MakeImg(PythonFuncs.ToAST());
                if(bm is not null) {
                    #region resize to fit screen
                    if(bm.Img.Height > curWindow.Size.height || bm.Img.Width > curWindow.Size.width){
                        (float newWidth, float newHeight) = (bm.Img.Width, bm.Img.Height);
                        if(newWidth > curWindow.Size.width){
                            newWidth = curWindow.Size.width;
                            newHeight = newHeight / (bm.Img.Width / newWidth);
                        }
                        if(newHeight > curWindow.Size.height - TAB_HEIGHT){
                            newHeight = curWindow.Size.height - TAB_HEIGHT;
                            newWidth = newWidth / (bm.Img.Height / newHeight);
                        }
                        bm = new(new(bm.Img, (int)newWidth, (int)newHeight), 0);
                    }
                    #endregion
                    curFunc.DisplayImage = bm.Img;
                    skipDrawNewScreen = true;
                    nonStatic.Invalidate();
                    isPic = true;
                }
            } catch (Exception){}
        }
    }
    public static void DrawTextScreen(bool withCurser = true) {
        if(skipDrawNewScreen) {
            skipDrawNewScreen = false;
            return;
        }
        isPic = false;

        List<Bitmap> bitmaps = new();
        int totalWidth = 0;
        int end = 0;
        for(int i = 0; i < linesText.Count; i++) {
            var lineText = linesText[i];
            int width = MeasureWidth(lineText, boldFont);
            totalWidth = Max(totalWidth, width);

            Bitmap bm = new(width, txtHeight);
            var g = Graphics.FromImage(bm);
            g.DrawString(lineText, boldFont, curTextBrush, 0, 0);

            if(i == CursorPos.Line && withCurser) {
                var before = CursorPos.Col == -1 ? "": linesText[i][..(CursorPos.Col + 1)];
                g.FillRectangle(
                    curserBrush,
                    MeasureWidth(before, boldFont) - 3,
                    5, 1, txtHeight - 10
                );
            }

            if(selectedLine is (int, int) sl && withCurser) {
                if((i < sl.line && i > CursorPos.Line) || (i > sl.line && i < CursorPos.Line)) {
                    g.FillRectangle(
                        selectBrush, 0, 0,
                        MeasureWidth(lineText, boldFont), LINE_HEIGHT
                    );
                } else if(i == sl.line || i == CursorPos.Line) {
                    int cCol = CursorPos.Col, sCol = sl.col;
                    if(i == sl.line) {
                        cCol = i == CursorPos.Line ? CursorPos.Col : (i > CursorPos.Line ? -1 : lineText.Length - 1);
                    } else {
                        sCol = i > sl.line ? -1 : lineText.Length - 1;
                    }
                    var (smaller, bigger) = cCol < sCol ? (cCol, sCol) : (sCol, cCol);
                    var startS = smaller == -1 ? 0 : MeasureWidth(linesText[i][..(smaller + 1)], boldFont);

                    var endS = MeasureWidth(linesText[i][..Min(linesText[i].Length, bigger + 1)], boldFont);
                    g.FillRectangle(selectBrush, 0 + startS, 0, endS - startS, LINE_HEIGHT);
                }
            }
            end += bm.Height;
            bitmaps.Add(bm);
        }
        Bitmap newBitMap = new(Min(totalWidth, screenWidth), end);
        var gr = Graphics.FromImage(newBitMap);
        end = 0;
        foreach(var item in bitmaps) {
            gr.DrawImage(item, 0, end);
            end += item.Height;
        }
        curFunc.DisplayImage = newBitMap;
    }
    public static void OpenErrLink(object? sender, EventArgs e) {
        if(PythonFuncs.errLink is not null) {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {PythonFuncs.errLink}") { CreateNoWindow = true });
        }
    }
    public static void Form1_Paint(object? sender, PaintEventArgs e) {
        foreach(var item in windows.AsEnumerable().Reverse()) {
            var itemHeight = (int)item.Size.height - TAB_HEIGHT;
            var drawPosY = item.Pos.y + TAB_HEIGHT;
            if(item.Function.Name.Equals(".console")){
                itemHeight = (int)item.Size.height;
                drawPosY = item.Pos.y;
            }
            if((int)item.Size.width > 0 && itemHeight > 0){
                //draw the img
                Bitmap bm = new((int)item.Size.width, itemHeight);
                using(var g = Graphics.FromImage(bm)){
                    g.Clear(Color.Black); // make back black
                    g.DrawImage(item.Function.DisplayImage!, 0, item.Offset);
                }
                e.Graphics.DrawImage(bm, item.Pos.x, drawPosY);
                //draw frame
                e.Graphics.DrawRectangle(new(Color.White, 2), item.Pos.x-2, item.Pos.y-2, item.Size.width+2, item.Size.height+2);
            }
        }
        // tab bar
        e.Graphics.FillRectangle(tabBarBrush, 0, 0, screenWidth, TAB_HEIGHT);
    }
}