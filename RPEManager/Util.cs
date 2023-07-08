using static RPEManager.Program;

namespace RPEManager;

internal class Util
{
    public static T Last<T>(Func<Action, Action, T> function)
    {
        var cancel = false;
        T result;
        while (true)
        {
            result = function(() => { cancel = true; }, () => { cancel = false; });
            if (cancel == true)
                return result;
        }
    }

    public static bool IfListContains<LT>(LT value, params LT[] list)
    {
        return list.Contains(value);
    }

    public static bool IfListContains<LT>(LT value,int _ = 0, LT[] list)
    {
        return list.Contains(value);
    }

    public static int Which<T>(T[] items, LineFormatter formatter)
    {
        while (true)
        {
            Console.Clear();
            for (int i = 0; i < items.Length; i++)
            {
                Console.WriteLine(formatter(i, items[i]));
            }
            WriteLogo(false);
            Console.Write("选择(输入数字):");

            try
            {
                int i = int.Parse(Console.ReadLine());
                if (i <= items.Length)
                    return i;
                else
                    throw new IndexOutOfRangeException();
            }
            catch
            {
                WriteLogo();
                Console.WriteLine("请检查输入~ (按任意键继续)");
                Console.ReadKey(false);
            }
        }
    }

    public static void WriteLines(params string[] lines)
    {
        foreach(string s in lines)
        {
            Console.WriteLine(s);
        }
    }

    public static string FormatLines(params string[] lines)
    {
        string result = "";
        foreach(string s in lines)
        {
            result += s + Environment.NewLine;
        }
        return result;
    }

    public static T[] Empty<T>() => Array.Empty<T>();

    public static T As<T>(object obj) where T : class
    {
        return (T)Convert.ChangeType(obj, typeof(T));
    }
    public static T As<T>(object obj, int _ = 0) where T : struct
    {
        return (T)Convert.ChangeType(obj, typeof(T));
    }
    public static T GetInput<T>(Func<T> inputfunction = null,string desc = "",string tip = "") where T : class
    {
        Console.WriteLine($"请输入{(desc != ""? desc : "值")}{(tip != ""? $"（{tip.Split("|")[0]}或{tip.Split("|")[1]})" : "")}");
        return inputfunction == null? As<T>(Console.ReadLine()) : As<T>(inputfunction());
    }

    public static T GetInput<T>(Func<T> inputfunction = null, string desc = "", string tip = "", int _ = 0) where T : struct
    {
        Console.WriteLine($"请输入{(desc != "" ? desc : "值")}{(tip != "" ? $"（{tip.Split("|")[0]}或{tip.Split("|")[1]})" : "")}");
        return inputfunction == null ? As<T>(Console.ReadLine()) : As<T>(inputfunction());
    }
}
