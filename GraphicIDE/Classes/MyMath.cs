namespace GraphicIDE;
public static class MyMath {
    public static float Abs(float num) => num > 0 ? num : -num;
    public static float Max(float num1, float num2) => num1 > num2 ? num1 : num2;
    public static int Max(int num1, int num2) => num1 > num2 ? num1 : num2;
    public static float Min(float num1, float num2) => num1 < num2 ? num1 : num2;
    public static int Min(int num1, int num2) => num1 < num2 ? num1 : num2;
    public static (float, float) MaxMin(float num1, float num2) => num1 > num2 ? (num1, num2) : (num2, num1);
    public static (int, int) MaxMin(int num1, int num2) => num1 > num2 ? (num1, num2) : (num2, num1);
}