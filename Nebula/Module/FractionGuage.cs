namespace Nebula.Module;

public class FractionGuage
{
    int num, den;

    //最小公倍数
    private static int Lcm(int a, int b)
    {
        return a * b / Gcd(a, b);
    }

    //最大公約数
    private static int Gcd(int a, int b)
    {
        if (a < b)
            return Gcd(b, a);
        while (b != 0)
        {
            var remainder = a % b;
            a = b;
            b = remainder;
        }
        return a;
    }

    public void AddOneOutOf(int n)
    {
        Add(1, n);
    }

    public void Add(int num, int den)
    {
        int newDen = Lcm(den, this.den);
        int newNum = 0;
        newNum += (newDen / den) * num;
        newNum += (newDen / this.den) * this.num;

        num = newNum;
        den = newDen;

        int gcd = Gcd(num, den);
        num /= gcd;
        den /= gcd;
    }

    public static implicit operator float(FractionGuage fGuage) => (float)fGuage.num / (float)fGuage.den;

    public FractionGuage()
    {
        num = 0;
        den = 1;
    }
}