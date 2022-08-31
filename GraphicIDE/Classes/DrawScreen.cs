using static GraphicIDE.BrushesAndPens;
using static GraphicIDE.MyMath;
using static GraphicIDE.Helpers;
using static GraphicIDE.Form1;
using static GraphicIDE.AST;
using static GraphicIDE.Tabs;

namespace GraphicIDE;

public static class DrawScreen{
    public const int WINDOW_LEFT_GAP = 6;
    public static bool skipDrawNewScreen = false;
    private static readonly Font titleFont = new(FontFamily.GenericMonospace, 35, FontStyle.Bold);
    public static void DrawPicScreen() {
        if(curFunc.isPic) {
            DrawTextScreen();
            nonStatic.Invalidate();
            curFunc.isPic = false;
        } else {
            try {
                var bm_ = MakeImg(PythonFuncs.ToAST());
                
                if(bm_ is BM_Middle bm) {
                    if(!curFunc.name.StartsWith(".")){
                        int w = MeasureWidth(curFunc.name, titleFont);
                        int h = MeasureHeight(curFunc.name, titleFont);
                        Bitmap newBm = new(Max(bm.Img.Width, w), bm.Img.Height + h);
                        using(var g = Graphics.FromImage(newBm)){
                            g.DrawImage(bm.Img, 0, h);
                            g.DrawString(curFunc.name, titleFont, titleBrush, 0, 0);
                        }
                        bm = new(newBm, 0);
                    }
                    #region resize to fit screen
                    if(bm.Img.Height > curWindow.size.height || bm.Img.Width > curWindow.size.width){
                        (float newWidth, float newHeight) = (bm.Img.Width, bm.Img.Height);
                        if(newWidth > curWindow.size.width - 2 * WINDOW_LEFT_GAP){
                            newWidth = curWindow.size.width - 2 * WINDOW_LEFT_GAP;
                            newHeight /= (bm.Img.Width / newWidth);
                        }
                        if(newHeight > curWindow.size.height - TAB_HEIGHT){
                            newHeight = curWindow.size.height - TAB_HEIGHT;
                            newWidth /= (bm.Img.Height / newHeight);
                        }
                        bm = new(new(bm.Img, (int)newWidth, (int)newHeight), 0);
                    }
                    #endregion
                    curFunc.displayImage = bm.Img;
                    skipDrawNewScreen = true;
                    nonStatic.Invalidate();
                    curFunc.isPic = true;
                }
            } catch (Exception){}
        }
    }
    public static void DrawTextScreen(bool withCurser = true) {
        if(skipDrawNewScreen) {
            skipDrawNewScreen = false;
            return;
        }
        curFunc.isPic = false;

        LinkedList<Bitmap> bitmaps = new();
        int totalWidth = 0;
        int end = 0;
        for(int i = 0; i < linesText.Count; i++) {
            var lineText = linesText[i];
            int width = MeasureWidth(lineText, boldFont);
            totalWidth = Max(totalWidth, width);

            Bitmap bm = new(width, txtHeight);
            var g = Graphics.FromImage(bm);
            g.DrawString(lineText, boldFont, curTextBrush, 0, 0);

            if(i == CursorPos.line && withCurser) {
                var before = CursorPos.col == -1 ? "": linesText[i][..(CursorPos.col + 1)];
                g.FillRectangle(
                    curserBrush,
                    MeasureWidth(before, boldFont) - 3,
                    txtHeight/5, 1, txtHeight/5*3
                );
            }

            if(selectedLine is (int, int) sl && withCurser) {
                if((i < sl.line && i > CursorPos.line) || (i > sl.line && i < CursorPos.line)) {
                    g.FillRectangle(
                        selectBrush, 0, 0,
                        MeasureWidth(lineText, boldFont), txtHeight
                    );
                } else if(i == sl.line || i == CursorPos.line) {
                    int cCol = CursorPos.col, sCol = sl.col;
                    if(i == sl.line) {
                        cCol = i == CursorPos.line ? CursorPos.col : (i > CursorPos.line ? -1 : lineText.Length - 1);
                    } else {
                        sCol = i > sl.line ? -1 : lineText.Length - 1;
                    }
                    var (smaller, bigger) = cCol < sCol ? (cCol, sCol) : (sCol, cCol);
                    var startS = smaller == -1 ? 0 : MeasureWidth(linesText[i][..(smaller + 1)], boldFont);

                    var endS = MeasureWidth(linesText[i][..Min(linesText[i].Length, bigger + 1)], boldFont);
                    g.FillRectangle(selectBrush, startS, 0, endS - startS, txtHeight);
                }
            }
            end += bm.Height;
            bitmaps.AddLast(bm);
        }
        Bitmap newBitMap = new(Min(totalWidth, (int)curWindow.size.width - WINDOW_LEFT_GAP * 2), end);
        var gr = Graphics.FromImage(newBitMap);
        end = 0;
        foreach(var item in bitmaps) {
            gr.DrawImage(item, 0, end);
            end += item.Height;
        }
        curFunc.displayImage = newBitMap;
    }
    public static void Form1_Paint(object? sender, PaintEventArgs e) {
        foreach(var window in windows.AsEnumerable().Reverse()) {
            var itemHeight = (int)window.size.height - TAB_HEIGHT;
            var drawPosY = window.pos.y + TAB_HEIGHT;
            if(window.function.name.Equals(".console")){
                itemHeight = (int)window.size.height;
                drawPosY = window.pos.y;
            }
            if((int)window.size.width > 0 && itemHeight > 0){
                // ? draw the img
                Bitmap bm = new((int)window.size.width, itemHeight);
                using(var g = Graphics.FromImage(bm)){
                    g.Clear(Color.Black); // ? make back black
                    Bitmap img = window.function.displayImage!;
                    if(window.function.isPic){
                        g.DrawImage(img, bm.Width/2-img.Width/2, 0);
                    } else {
                        g.DrawImage(img, 0, window.offset);
                    }
                }
                e.Graphics.DrawImage(bm, window.pos.x + WINDOW_LEFT_GAP, drawPosY);
                // ? draw frame
                e.Graphics.DrawRectangle(new(Color.White, 2), window.pos.x-2, window.pos.y-2, window.size.width+2, window.size.height+2);
            }
        }
        // ? prompt
        if(visablePrompt is Prompt p){
            e.Graphics.DrawImage(
                p.bm, 
                (int)(screenWidth / 2 - p.bm.Width / 2), 
                (int)(screenHeight / 2 - p.bm.Height + p.tb.Height / 2 + 20)
            );
        }
        // ? right click menu
        foreach(var menu in visibleMenues) {
            e.Graphics.FillRectangle(menu.bgColor, menu.bgPos);
        }
        // ? tab bar
        e.Graphics.FillRectangle(tabBarBrush, 0, 0, screenWidth, TAB_HEIGHT);
    }
}