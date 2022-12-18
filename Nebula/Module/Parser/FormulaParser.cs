namespace Nebula.Module.Parser;

public class FormulaResult
{
    int iValue;
    bool bValue;
    bool isBool;

    public int GetInt() => iValue;
    public bool GetBool() => bValue;
    public bool IsBool() => isBool;

    public FormulaResult(int value = 0)
    {
        iValue = value;
        bValue = value == 1;
        isBool = false;
    }

    public FormulaResult(bool value)
    {
        iValue = value ? 1 : 0;
        bValue = value;
        isBool = true;
    }
}

public delegate FormulaResult FormulaOperation(FormulaContent left, FormulaContent right);


public class FormulaContent
{
    public string Attribute;

    public FormulaContent(string text)
    {
        Attribute = text;
    }

    virtual public FormulaResult GetResult()
    {
        if (Attribute == "") return new FormulaResult();
        if (int.TryParse(Attribute, out int iResult))
        {
            return new FormulaResult(iResult);
        }
        if (bool.TryParse(Attribute, out bool bResult))
        {
            return new FormulaResult(bResult);
        }
        return new FormulaResult();
    }

    virtual public void Substitute(string variable, string result)
    {
        if (Attribute == variable) Attribute = result;
    }

    virtual public void ConvertOperation(string opertionAttribute, FormulaOperation operation) { }
}

public class FormulaComplexContent : FormulaContent
{
    List<FormulaContent> List;
    public FormulaComplexContent(List<FormulaContent> list) : base("")
    {
        List = list;
    }

    override public FormulaResult GetResult()
    {
        if (List.Count > 0) return List[0].GetResult();
        return new FormulaResult();
    }

    override public void Substitute(string variable, string result)
    {
        foreach (var c in List)
        {
            c.Substitute(variable, result);
        }
    }

    override public void ConvertOperation(string opertionAttribute, FormulaOperation operation)
    {
        foreach (var c in List)
            c.ConvertOperation(opertionAttribute, operation);

        int i = 0;
        while (i < List.Count - 2)
        {
            if (List[i + 1].Attribute == opertionAttribute)
            {
                List[i] = new FormulaOperationContent(List[i], List[i + 2], operation);
                List.RemoveRange(i + 1, 2);
                continue;
            }
            i++;
        }
    }
}

public class FormulaOperationContent : FormulaContent
{
    FormulaContent Left, Right;
    FormulaOperation Operation;

    public FormulaOperationContent(FormulaContent left, FormulaContent right, FormulaOperation operation) : base("")
    {
        Left = left;
        Right = right;
        Operation = operation;
    }

    override public FormulaResult GetResult()
    {
        return Operation.Invoke(Left, Right);
    }

    override public void Substitute(string variable, string result)
    {
        Left.Substitute(variable, result);
        Right.Substitute(variable, result);
    }

    override public void ConvertOperation(string opertionAttribute, FormulaOperation operation)
    {
        Left.ConvertOperation(opertionAttribute, operation);
        Right.ConvertOperation(opertionAttribute, operation);
    }
}

public class FormulaAnalyzer
{
    FormulaComplexContent Formula;

    public FormulaAnalyzer(string text, params Dictionary<string, string>[] variables)
    {
        text = text.Replace(" ", "");
        List<FormulaContent> temp = new List<FormulaContent>();
        List<char> pool = new List<char>();
        foreach (char ch in text)
        {
            if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) pool.Add(ch);
            else
            {
                if (ch == '=' && temp.Count > 0)
                {
                    var c = temp[temp.Count - 1];
                    if (c.Attribute == "=" || c.Attribute == "<" || c.Attribute == ">" || c.Attribute == "!")
                    {
                        c.Attribute = c.Attribute + "=";
                        continue;
                    }
                }
                if (pool.Count > 0)
                {
                    temp.Add(new FormulaContent(new string(pool.ToArray())));
                    pool.Clear();
                }
                temp.Add(new FormulaContent(ch.ToString()));
            }
        }

        //プールにまだ残っていれば入れる
        if (pool.Count > 0)
            temp.Add(new FormulaContent(new string(pool.ToArray())));

        //かっこを処理
        ConvertBracket(temp);

        Formula = new FormulaComplexContent(temp);

        //各種演算子を処理
        Formula.ConvertOperation("*", (left, right) =>
        {
            return new FormulaResult(left.GetResult().GetInt() * right.GetResult().GetInt());
        });
        Formula.ConvertOperation("/", (left, right) =>
        {
            int r = right.GetResult().GetInt();
            if (r == 0) return new FormulaResult();
            return new FormulaResult(left.GetResult().GetInt() / r);
        });
        Formula.ConvertOperation("+", (left, right) =>
        {
            return new FormulaResult(left.GetResult().GetInt() + right.GetResult().GetInt());
        });
        Formula.ConvertOperation("-", (left, right) =>
        {
            return new FormulaResult(left.GetResult().GetInt() - right.GetResult().GetInt());
        });
        Formula.ConvertOperation("<", (left, right) =>
        {
            return new FormulaResult(left.GetResult().GetInt() < right.GetResult().GetInt());
        });
        Formula.ConvertOperation(">", (left, right) =>
        {
            return new FormulaResult(left.GetResult().GetInt() > right.GetResult().GetInt());
        });
        Formula.ConvertOperation("<=", (left, right) =>
        {
            return new FormulaResult(left.GetResult().GetInt() <= right.GetResult().GetInt());
        });
        Formula.ConvertOperation(">=", (left, right) =>
        {
            return new FormulaResult(left.GetResult().GetInt() >= right.GetResult().GetInt());
        });
        Formula.ConvertOperation("==", (left, right) =>
        {
            var l = left.GetResult();
            if (l.IsBool())
                return new FormulaResult(l.GetBool() == right.GetResult().GetBool());
            else
                return new FormulaResult(l.GetInt() == right.GetResult().GetInt());
        });
        Formula.ConvertOperation("!=", (left, right) =>
        {
            var l = left.GetResult();
            if (l.IsBool())
                return new FormulaResult(l.GetBool() != right.GetResult().GetBool());
            else
                return new FormulaResult(l.GetInt() != right.GetResult().GetInt());
        });
        Formula.ConvertOperation("&&", (left, right) =>
        {
            return new FormulaResult(left.GetResult().GetBool() && right.GetResult().GetBool());
        });
        Formula.ConvertOperation("||", (left, right) =>
        {
            return new FormulaResult(left.GetResult().GetBool() || right.GetResult().GetBool());
        });

        foreach (var variableTable in variables)
        {
            foreach (var entry in variableTable)
            {
                Formula.Substitute(entry.Key, entry.Value);
            }
        }
    }

    private void ConvertBracket(List<FormulaContent> list)
    {
        int count = 0;
        int beginIndex = -1;
        while (count < list.Count)
        {
            if (list[count].Attribute == "(") beginIndex = count;
            else if (list[count].Attribute == ")" && beginIndex != -1)
            {
                var segment = list.GetRange(beginIndex + 1, count - beginIndex - 1);
                list.RemoveRange(beginIndex + 1, count - beginIndex);
                list[beginIndex] = new FormulaComplexContent(segment);

                beginIndex = -1;
                count = 0;
                continue;
            }
            
            count++;
        }
    }

    public FormulaResult GetResult()
    {
        return Formula.GetResult();
    }
}