namespace GraphicIDE;

public static class Helpers {
    public static Dictionary<Font, float> fontToPipeSize = new();
    public static readonly Graphics nullGraphics = Graphics.FromImage(new Bitmap(1,1));
    
    public static Point GetScreenPos(){
        return Form1.nonStatic.RectangleToScreen(Form1.nonStatic.ClientRectangle).Location;
    }
    public static int GetHeight(){
        return Form1.nonStatic.RectangleToScreen(Form1.nonStatic.ClientRectangle).Height;
    }
    public static int GetWidth(){
        return Form1.nonStatic.RectangleToScreen(Form1.nonStatic.ClientRectangle).Width;
    }
    public static void ChangeOffsetTo(int i){
        curWindow.offset = Math.Clamp(
            i, Form1.txtHeight - curWindow.function.displayImage!.Height, 0
        );
    }
    public static void FirstChangeFontSize(int size){
        Form1.boldFont = new(FontFamily.GenericMonospace, size, FontStyle.Bold);
        Form1.indentW = MeasureWidth("    ", Form1.boldFont);
        Form1.qWidth = MeasureWidth("¿ ?", Form1.boldFont);
        Form1.qHeight = MeasureHeight("¿?", Form1.boldFont);
        Form1.upSideDownW = MeasureWidth("¿", Form1.boldFont);
        Form1.txtHeight = MeasureHeight(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789`~!@#$%^&*()-_=+[]{};:'\"\\/?", 
            Form1.boldFont
        );
        Form1.fromW = MeasureWidth("from ", Form1.boldFont);
        Form1.colW = MeasureWidth(": ", Form1.boldFont);
        Form1.nonStatic.Invalidate();
    }
    public static float imgSize = 1;
    public static void ChangeFontSize(int size){
        imgSize += size / 7f;
    }
    public static string PythonOperatorToString(PythonOperator po) => po switch{
        PythonOperator.In => "in",
        PythonOperator.Is => "is",
        PythonOperator.IsNot => "is not",
        PythonOperator.Not => "not",
        PythonOperator.NotIn => "not in",
        PythonOperator.LessThan => "<",
        PythonOperator.GreaterThan => ">",
        PythonOperator.LessThanOrEqual => "<=",
        PythonOperator.GreaterThanOrEqual => ">=",
        PythonOperator.Equal => "==",
        PythonOperator.NotEqual => "!=",
        PythonOperator.Add => "+",
        PythonOperator.Subtract => "-",
        PythonOperator.Multiply => "*",
        PythonOperator.Divide => "/",
        PythonOperator.FloorDivide => "//",
        PythonOperator.Power => "**",
        PythonOperator.Mod => "%",
        PythonOperator.BitwiseAnd => "&",
        PythonOperator.BitwiseOr => "|",
        PythonOperator.Xor => "^",
        PythonOperator.LeftShift => "<<",
        PythonOperator.RightShift => ">>",
        PythonOperator.Negate => "-",
        PythonOperator.None => "None",
        PythonOperator.Invert => "~",
        PythonOperator.Pos => "+",
        PythonOperator.TrueDivide => "???? what is true divide???",
        _ => "??I missed one???"
    };
    public static BM_Middle MakeTxtBM(string txt, Brush? brush = null) {
        var width = MeasureWidth(txt, Form1.boldFont);
        var height = MeasureHeight(txt, Form1.boldFont);
        var res = new Bitmap(width, height);
        brush = txt switch {
            "True" => greenBrush,
            "False" => redBrush,
            _ => brush ?? textBrush
        };
        var g = Graphics.FromImage(res);
        g.DrawString(txt, Form1.boldFont, brush, 0, 0);
        return new(res, (int)(height / 2));
    }
    public static bool IsNumeric(char val) => val == '_' || char.IsLetter(val) || char.IsDigit(val);
    public static bool IsAltNumeric(char val) => char.IsLower(val) || char.IsDigit(val);
    public static int MeasureWidth(string st, Font ft) {
        if(st.Contains('\n')) {
            return st.Split("\n").Select(
                (line) => MeasureWidth(line, Form1.boldFont)
            ).Max();
        }
        // used || so that trailing\leading spaces get included too
        // you might wonder why theres an "a" in here... me too... it just doesn't work without it...
        st = $"|a{st}|";
        return (int)(nullGraphics.MeasureString(st, ft).Width - CachedWidthOfPipes(ft));
    }
    public static float CachedWidthOfPipes(Font ft){
        if(fontToPipeSize.TryGetValue(ft, out float res)){
            return res;
        }
        float w = nullGraphics.MeasureString("|", ft).Width * 2;
        fontToPipeSize[ft] = w; 
        return w;
    }
    public static int MeasureHeight(string st, Font ft) => (int)nullGraphics.MeasureString(st, ft).Height;

    public static bool IsShiftPressed() => (Form1.ModifierKeys & Keys.Shift) == Keys.Shift;
    public static bool IsCapsPressed() => (Form1.ModifierKeys & Keys.CapsLock) == Keys.CapsLock;
    public static bool IsAltlPressed() => (Form1.ModifierKeys & Keys.Alt) == Keys.Alt;
    public static bool IsCtrlPressed() => (Form1.ModifierKeys & Keys.Control) == Keys.Control;

    public static bool IsBefore((int line, int col) one, (int line, int col) two) {
        return one.line < two.line || (one.line == two.line && one.col < two.col);
    }
    public static bool InBetween((int line, int col) cur, (int line, int col) one, (int line, int col) two) {
        var (first, last) = IsBefore(one, two) ? (one, two) : (two, one);
        return IsBefore(cur, last) && IsBefore(first, cur);
    }

}