using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;
using static RPEManager.Util;

namespace RPEManager;

internal class Program
{

    public delegate string LineFormatter(params object[] items);
    public delegate bool ControlCtrlDelegate(int CtrlType);

    static Settings CurrentSettings = new();
    static List<Dictionary<string, string>> Charts = new();
    readonly static Dictionary<string, string> ChartInfoKeyChinese = new()
    {
        {"名称", "Name"},
        {"唯一标识符", "Path"},
        {"歌曲文件", "Song"},
        {"图片文件", "Picture"},
        {"铺面文件", "Chart" },
        {"难度标识", "Level" },
        {"作曲者", "Composer" },
        {"谱师", "Charter" }
    };

    
    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);
    private static ControlCtrlDelegate cancelHandler = new(OnDestory);

    static bool LoadSetting()
    {
        if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RPEManager\\Setting.dat"))
        {
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RPEManager");
            File.CreateText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RPEManager\\Setting.dat");
            return true;
        }
        else if (File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RPEManager\\Setting.dat") == "")
            return true;

        StreamReader setting;
        setting = new(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RPEManager\\Setting.dat");
        CurrentSettings = JsonSerializer.Deserialize<Settings>(JsonDocument.Parse(setting.BaseStream));
        setting.Close();

        return false;
    }

    static bool HasOption(string option, string[] optlist)
    {
        return optlist.Contains(option) ||
             ((CurrentSettings.UseDefaultValue ?? false) &&
              (CurrentSettings.DefaultValueItems??Array.Empty<string>()).Contains(option));
    }

    static string[]? AddString(string[]? sv, string str)
    {
        var cp = sv;
        if (cp == null)
            return null;

        for (int i = 0; i < cp.Length; i++)
            cp[i] = str + cp[i];
        return cp;
    }

    static string[] FindChartFolder(string key)
    {
        return AddString(FindItems("Name", "Path", key), CurrentSettings.ResourcePath) ?? AddString(FindItems("Path", "Path", key), CurrentSettings.ResourcePath) ?? Array.Empty<string>();
    }

    static Dictionary<string,string> FormatChartlist(string content)
    {
        Dictionary<string, string> block = new();
        foreach (string infos in content.Split('\n')[1..^1])
        {
            string[] infounit = infos.Split(": ");
            block.Add(infounit[0], infounit[1]);
        }
        return block;
    }

    static string[]? FindItems(string findkey, string outputkey, string searchcontent)
    {
        List<string> result = new();
        foreach (Dictionary<string, string> items in Charts)
        {
            if (items[findkey].Replace("\r","") == searchcontent)
            {
                result.Add(items[outputkey]);
            }
        }
        if (result.Count > 0)
        {
            return result.ToArray();
        }
        return null;
    }

    static string? FindItemFirst(string findkey, string outputkey, string searchcontent) => FindItems(findkey, outputkey, searchcontent)?.FirstOrDefault((string)null);

    static void RemoveAutosaveInternal(string path, string[] options)
    {
        var autosavefiles = new List<string>(Directory.GetFiles((path + "\\").Replace("\r",""), "AutoSave*"));
        autosavefiles.Sort();
        var asf = autosavefiles.ToArray();
        foreach (string file in
                    asf[..(
                            HasOption("--sl",options) ? new Index(1, true) : new Index(1, false)
                        )
                    ]
               )
        {
            File.Delete(file);
        }
    }

    static void ChartOperator(string name, string[] ConstArg, Action<string, string, string[]> @delegate)
    {
        string[] folders = FindChartFolder(name);

        if (folders.Length == 0)
        {
            return;
        }
        else if (folders.Length == 1)
        {
            string folder = folders[0];
            @delegate(folder, FindItems("Path", "Name", folder.Split("\\")[^1].Replace("\r",""))[0], ConstArg);
        }
        else
        {
            int index = Which(folders, (l) =>
            {
                return $"{l[0]}. {FindItemFirst("Path", "Name", ((string[])l)[1].Split("\\")[^0])[0]}({((string[])l)[1].Split("\\")[^0]})";
            });

            @delegate(folders[index], FindItemFirst("Path", "Name", folders[index].Split("\\")[^0]), ConstArg);
        }
    }

    static void RemoveAutosave(string name, string[] options)
    {
        ChartOperator(name, options, (folder, _, options) =>
        {
            RemoveAutosaveInternal(folder, options);
        });
    }

    static void RemoveChart(string name, string[] options)
    {
        ChartOperator(name, options, (folder, id, options) =>
        {
            if (HasOption("--nmtb", options))
                Directory.Delete(folder.Replace("\r", ""), true);
            else
                FileSystem.DeleteDirectory(folder.Replace("\r", ""), UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
            Dictionary<string, string> item = new();
            foreach(var v in Charts)
            {
                if (v["Path"] == id)
                {
                    item = v;
                }
            }

            Charts.Remove(item);
        });
    }

    static void ToPEZ(string name)
    {
        ChartOperator(name, null, (folder, Name, _) =>
        {
            using System.IO.Compression.ZipArchive zipArchive = new(File.Create(string.Join("\\", folder.Split("\\")[..^1]) + "\\" + Name.Replace("\r", "") + ".pez"), System.IO.Compression.ZipArchiveMode.Create);
            var id = folder.Split("\\")[^1].Replace("\r", "");

            StreamWriter imsw = new(zipArchive.CreateEntry(FindItemFirst("Path", "Picture", id)).Open());
            imsw.BaseStream.Write(File.ReadAllBytes(folder.Replace("\r", "") + "\\" + FindItemFirst("Path", "Picture", id).Replace("\r", "")));
            imsw.Close();

            StreamWriter musw = new(zipArchive.CreateEntry(FindItemFirst("Path", "Song", id)).Open());
            musw.BaseStream.Write(File.ReadAllBytes(folder.Replace("\r", "") + "\\" + FindItemFirst("Path", "Song", id).Replace("\r", "")));
            musw.Close();

            StreamWriter chsw = new(zipArchive.CreateEntry(FindItemFirst("Path", "Chart", id)).Open());
            chsw.BaseStream.Write(File.ReadAllBytes(folder.Replace("\r", "") + "\\" + FindItemFirst("Path", "Chart", id).Replace("\r", "")));
            chsw.Close();

            StreamWriter insw = new(zipArchive.CreateEntry(id + ".txt").Open());
            foreach (var d in Charts)
            {
                if (d["Path"] == id)
                {
                    insw.WriteLine("#");
                    foreach (var i in d)
                    {
                        insw.WriteLine($"{i.Key}: {i.Value}");
                    }
                }
            }
            insw.Close();


        });
    }

    static void SaveChartInfo()
    {
        Charts.ForEach(chart =>
        {
            using StreamWriter info = new(CurrentSettings.ResourcePath + chart["Path"] + "\\info.txt");
            info.BaseStream.Seek(0, SeekOrigin.Begin);
            info.WriteLine("#");
            info.WriteLine(FormatChartlist(chart));
            info.Close();
        });
    }

    

    static void FirstStart()
    {
        WriteLogo();
        Console.WriteLine("您看起来像是第一次启动");
        Console.WriteLine("来让我们一起开始配置吧！");
        Console.ReadKey();

        Console.Clear();

        string path = Last<string>((ok, no) =>
        {
            WriteLogo();
            Console.WriteLine("请输入资源文件夹路径~");
            var r = Console.ReadLine();
            if (!Directory.Exists(r))
                no();
            else
                ok();

            return r;

        });

        CurrentSettings.ResourcePath = path.EndsWith("\\")? path : path + "\\";

        WriteLogo();
        Console.WriteLine("配置完成！请重启应用以应用更改！");
        Console.ReadKey();
    }

    static void SaveSettingToFile()
    {
        var setting = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RPEManager\\Setting.dat");
        JsonSerializer.Serialize(setting.BaseStream, CurrentSettings);
        setting.Close();
    }

    static string FormatChartlist(Dictionary<string, string> chart)
    {
        string result = "";

        foreach(var item in chart)
        {
            result += item.Key + ": " + item.Value + "\n";
        }

        return result;
    }

    static void SaveChartslist()
    {
        using StreamWriter sw = new(string.Join('\\', CurrentSettings.ResourcePath.Replace("\r", "").Split('\\')[..^1]) + "Chartlist.txt");
        sw.BaseStream.Seek(0, SeekOrigin.Begin);
        foreach(var c in Charts)
        {
            sw.WriteLine("#");
            sw.WriteLine(FormatChartlist(c));
        }
    }

    static bool OnDestory(int CtrlId = 0)
    {
        SaveSettingToFile();

        SaveChartslist();

        SaveChartInfo();

        //Process.GetCurrentProcess().Kill();

        return true;
    }

    static void InitApp()
    {
        bool fs = LoadSetting();
        if (fs)
        {
            FirstStart();
            OnDestory();
        }

        string file = string.Join("\\", CurrentSettings.ResourcePath.Split("\\")[..^2]) + "\\Chartlist.txt";
        StreamReader streamReader = new(file);

        string filecontent = streamReader.ReadToEnd().Replace("\r", "");
        streamReader.Close();
        foreach(string block in filecontent.Replace("\r", "").Split("#")[1..]) 
        {
            Charts.Add(FormatChartlist(block.Replace("\r","")));
        }

        SetConsoleCtrlHandler(cancelHandler, true);
    }

    static void DisplayCharts()
    {
        WriteLogo();
        Console.WriteLine("|\tName\t|\tPath\t|\tSong\t|\tPicture\t|\tChart\t|\tLevel\t|\tComposer\t|\tCharter\t|");
        foreach (var d in Charts)
        {
            Console.Write("|");
            foreach (var item in d)
            {
                Console.Write(item.Value.Replace("\r", "") + "|");
            }
            Console.WriteLine();
        }
        Console.ReadKey();
    }

    static void Setting()
    {
        Last((mok, mno) =>
        {
            static bool IfReturn(string str)
            {
                return IfListContains(str, "Exit", "退出");
            }
            var v = Last((ok, no) =>
            {
                WriteLogo();

                List<string> lines = new()
                {
                    "请输入要设置的内容~",
                    "",
                    "中文            英文"
                };
                foreach (var v in Settings.KeyChinese)
                {
                    string line = "";
                    line += v.Key;
                    int whitespace = 2 * (8 - v.Key.Length);
                    while (whitespace > 0)
                    {
                        line += " ";
                        whitespace--;
                    }
                    line += v.Value;
                    lines.Add(line);
                }

                lines.Add("退出        Exit");

                var result = GetInput<string>(
                    desc: FormatLines(
                        lines.ToArray()
                    )
                );

                if (!IfListContains(result, Settings.KeyChinese.Keys.ToArray().Concat(Settings.KeyChinese.Values.ToArray()).ToArray()))
                {
                    WriteLogo();
                    Console.WriteLine("输入不正确哦~");
                    Console.ReadKey();
                    no();
                    return "";
                }
                else
                {
                    ok();
                    return result;
                }
            });

            switch (v)
            {
                case "资源文件夹路径":
                case "ResourcePath":
                    CurrentSettings.ResourcePath = Last((ok, no) =>
                    {
                        WriteLogo();
                        string input = GetInput<string>(desc: "资源文件夹路径");
                        if (IfReturn(input))
                        {
                            mok();
                            ok();
                            return"";
                        }
                            
                        if (Directory.Exists(input))
                        {
                            WriteLogo();
                            Console.WriteLine("设置成功！请重启以应用更改！");
                            Console.ReadLine();
                            ok();
                            OnDestory();
                        }
                        else
                        {
                            WriteLogo();
                            Console.WriteLine("输入不正确哦~");
                            Console.ReadLine();
                            no();
                        }
                        return input;
                    }) ?? "";
                    CurrentSettings.ResourcePath = CurrentSettings.ResourcePath.EndsWith("\\") ? CurrentSettings.ResourcePath : CurrentSettings.ResourcePath + "\\";
                    break;
                case "使用默认参数":
                case "UseDefaultValue":
                    CurrentSettings.UseDefaultValue = Last((ok, no) =>
                    {
                        WriteLogo();
                        string input = "false";
                        bool b = false;
                        try
                        {
                            input = GetInput<string>(tip: "true|false");
                            if (IfReturn(input))
                            {
                                mok();
                                ok();
                                return false;
                            }
                            b = As<bool>(input);
                        }
                        catch (FormatException)
                        {
                            WriteLogo();
                            Console.WriteLine("输入不正确哦~");
                            Console.ReadLine();
                            no();
                            return false;
                        }

                        WriteLogo();
                        Console.WriteLine("设置成功！");
                        Console.ReadLine();
                        ok();

                        return b;
                    });

                    break;

                case "默认参数":
                case "DefaultValueItems":
                    CurrentSettings.DefaultValueItems = Last((ok, no) =>
                    {
                        WriteLogo();

                        string input = GetInput<string>(desc: "参数", tip: "用,隔开");

                        if (IfReturn(input))
                        {
                            mok();
                            ok();
                            return Empty<string>();
                        }
                        WriteLogo();
                        Console.WriteLine("设置成功！");
                        Console.ReadLine();

                        ok();
                        return input.Split(",");
                    });

                    break;

                case "新页输出标识":
                case "NewPageWriteLogo":
                    CurrentSettings.NewPageWriteLogo = Last((ok, no) =>
                    {
                        WriteLogo();
                        string input = "false";
                        bool b = false;
                        try
                        {
                            input = GetInput<string>(tip: "true|false");
                            if (IfReturn(input))
                            {
                                mok();
                                ok();
                                return false;
                            }

                        }
                        catch (FormatException)
                        {
                            WriteLogo();
                            Console.WriteLine("输入不正确哦~");
                            Console.ReadLine();
                            no();
                            return false;
                        }

                        WriteLogo();
                        Console.WriteLine("设置成功！");
                        Console.ReadLine();
                        ok();

                        return b;

                    });
                    break;
                case "退出":
                case "Exit":
                    mok();
                    return "";
                default:
                    mno();
                    WriteLogo();
                    Console.WriteLine("输入不正确哦~");
                    Console.ReadLine();
                    return "";
            }
            mok();
            return "";
        });
    }

    static void SetChartInfo()
    {
        var i = Last((ok, no) =>
        {
            WriteLogo();
            List<string> lines = new()
            {
                "请输入要设置的内容~",
                "",
                "中文        英文"
            };
            foreach (var v in ChartInfoKeyChinese)
            {
                string line = "";
                line += v.Key;
                int whitespace = 2 * (6 - v.Key.Length);
                while (whitespace > 0)
                {
                    line += " ";
                    whitespace--;
                }
                line += v.Value;
                lines.Add(line);
            }

            lines.Add("退出        Exit");
            var result = GetInput<string>(desc: FormatLines(lines.ToArray()));
            ok();
            return result;
        });
    }

    static void OperatorMenu()
    {
        static (string? i, string? opt) Input(Action ok, Action no)
        {
            string? i, opt;
            WriteLogo();
            i = GetInput<string>(desc: "铺面名称（或ID)和选项 (使用|号分开)");
            var l = FindChartFolder(i);
            if (l == null || l == Empty<string>())
            {
                WriteLogo();
                Console.WriteLine("输入不正确哦~");
                Console.ReadLine();
                no();
                return ("", "");
            }
            ok();
            opt = i.Split("|").Length > 1 ? i.Split("|")[1] : "";
            return (i, opt);
        }
        while (true)
        {
            var w = Last((ok, no) =>
            {
                WriteLogo();
                var v = GetInput(
                    inputfunction: Console.ReadKey,
                    desc: FormatLines(
                        "以下编号以进行操作",
                        "",
                        "A. 删除铺面的自动保存文件",
                        "B. 删除铺面",
                        "C. 打包铺面",
                        "D. 退出"
                    )
                );
                if (!IfListContains(v.Key, 
                           ConsoleKey.A,
                           ConsoleKey.B,
                           ConsoleKey.C,
                           ConsoleKey.D))
                {
                    WriteLogo();
                    Console.WriteLine("输入不正确哦~");
                    no();
                }
                else
                    ok();

                return v;
            });
            switch (w.Key)
            {
                case ConsoleKey.A: 
                    var (ravi, ravopt) = Last(Input);

                    RemoveAutosave(ravi, ravopt.Split(" "));

                    WriteLogo();
                    Console.WriteLine("清除成功！");
                    Console.ReadLine();
                    break;
                case ConsoleKey.B:
                    int count = 0;
                    bool notmovetobin = false;
                    while (true)
                    {
                        WriteLogo();
                        Console.WriteLine($"您确定要这样做吗？[铺面会消失很久！（真的很久！）](Y/N){{{count}/10}}");
                        if (Console.ReadKey(false).Key == ConsoleKey.Y)
                        {
                            count++;
                        }
                        else
                        {
                            break;
                        }
                        if(count >= 10)
                        {
                            var (ri, ropt) = Last(Input);
                            if (HasOption("--nmtb", ropt.Split(" ")))
                                notmovetobin = true;
                            RemoveChart(ri, ropt.Split(" "));

                            WriteLogo();
                            if (!notmovetobin)
                                Console.WriteLine("清除成功！(已将文件移动到回收站，可恢复)");
                            else
                                Console.WriteLine("清除成功！(不可恢复！)");
                            Console.ReadLine();
                            break;
                        }
                    }
                    break;
                case ConsoleKey.C: 
                    var (tpi,_) = Last(Input);
                    ToPEZ(tpi);

                    WriteLogo();
                    Console.WriteLine("打包成功！");
                    Console.ReadLine();
                    break;
                    
                case ConsoleKey.D:
                    return;
            }　
        }
    }

    public static void WriteLogo(bool ClearCurrentContent = true)
    {
        if (CurrentSettings.NewPageWriteLogo ?? false)
            return;
        if (ClearCurrentContent)
            Console.Clear();
        Console.WriteLine("RPE 铺面管理器 v1.0  by 秋风Elipese");
        Console.WriteLine();
    }

    static void MainMenu()
    {
        while (true)
        {
            var w = Last((ok, no) =>
            {
                WriteLogo();
                var v = GetInput(
                    inputfunction: Console.ReadKey,
                    desc: FormatLines(
                        "以下编号以进行操作",
                        "",
                        "A. 显示铺面列表",
                        "B. 对铺面进行操作",
                        "C. 设置",
                        "D. 退出"
                    )
                );
                if (!IfListContains(v.Key,
                           ConsoleKey.A,
                           ConsoleKey.B,
                           ConsoleKey.C,
                           ConsoleKey.D))
                {
                    Console.WriteLine("输入不正确哦~");
                    no();
                }
                else
                    ok();

                return v;
            });

            switch (w.Key)
            {
                case ConsoleKey.A:
                    DisplayCharts();
                    break;
                case ConsoleKey.B:
                    OperatorMenu();
                    break;
                case ConsoleKey.C:
                    Setting();
                    break;
                case ConsoleKey.D:
                    return;
            }
        }
    }

    public static void Main(string[] args)
    {
        InitApp();

        MainMenu();

        OnDestory();
    }
}