namespace RPEManager;

public class Settings
{
    public Settings() { }

    public string? ResourcePath { get; set; }

    public bool? UseDefaultValue { get; set; }
    public string[]? DefaultValueItems { get; set; }

    public bool? NewPageWriteLogo { get; set; }

    public readonly static Dictionary<string, string> KeyChinese = new()
    {
        ["资源文件夹路径"] = "ResourcePath",
        ["使用默认参数"] = "UseDefaultValue",
        ["默认参数"] = "DefaultValueItems",
        ["新页输出标识"] = "NewPageWriteLogo"
    };

    private void Set(string key, string value)
    {
        switch(key)
        {
            case "ResourcePath":
                ResourcePath = value; break;
            case "UseDefaultValue":
                UseDefaultValue = bool.Parse(value); break;
            case "DefaultValueItems":
                DefaultValueItems = value.Split(','); break;
            case "NetPageWriteLogo":
                NewPageWriteLogo = bool.Parse(value); break;
            default:
                break;
        }
    }

    public string this[string index]
    {
        set
        {
            if(Util.IfListContains(index, KeyChinese.Keys.ToArray()))
            {
                Set(KeyChinese[index], value);
                return;
            }
            else if(Util.IfListContains(index, KeyChinese.Values.ToArray()))
            {
                Set(index, value);
                return;
            }
            else
            {
                throw new ArgumentException("The NAME is not contain in this list.");
            }
        }
    }
}
