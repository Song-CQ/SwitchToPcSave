// See https://aka.ms/new-console-template for more information

using System;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading;
using MediaDevices;


namespace StardewValleySave
{


    public class PreferencesData
    {

        public string Steam_Save_Path = "";//Steam的存档路径
        public string Switch_Save_Path = "";//Switch的存档路径
        public string Switch_User_Name = "";//Switch用户名
        public string Switch_Game_Name = "";//Switch要同步的游戏名字

        public List<string> Sync_Files = new List<string>(); // 存档下要同步的文件(包括文件夹)


    }

    enum SaveType
    {
        SetPreferencesData = 0,
        SteamToSwitch = 1,
        SwitchToSteam = 2,
    }

    class MainClass
    {
        private static PreferencesData Preferences = null;
        private static string currentDirectory = "";
        private static string From_SavePath_Steam = "";
        private static string From_SavePath_Switch = "";
        private static string To_SaveData_Steam = "";
        private static string To_SaveData_Switch = "";
        private static JsonSerializerOptions options = new JsonSerializerOptions();
        private static MediaDevice Switchdevice = null;



        [STAThread]
        static void Main(string[] args)
        {

            Thread.CurrentThread.Name = "SwitchToPcSave";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("启动SwitchToPcSave程序!");
            currentDirectory = System.Environment.CurrentDirectory;
            Console.ForegroundColor = ConsoleColor.White;

            To_SaveData_Steam = currentDirectory + @"\SaveData_Steam";
            if (!Directory.Exists(To_SaveData_Steam))
            {
                Directory.CreateDirectory(To_SaveData_Steam);
            }

            To_SaveData_Switch = currentDirectory + @"\SaveData_Switch";
            if (!Directory.Exists(To_SaveData_Switch))
            {
                Directory.CreateDirectory(To_SaveData_Switch);

            }

            options.IncludeFields = true;
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            Console.WriteLine("读取设置数据 Preferences!");
            Preferences = Readjson(currentDirectory + "/Preferences.json");




            SaveType type = SaveType.SetPreferencesData;

            if (args != null && args.Length >= 1)
            {
                int type_int = int.Parse(args[0]);


                type = (SaveType)type_int;
            }

            Console.WriteLine($"---------------------------");
            Console.Write($"操作类型: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{type}! ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine($"---------------------------");
            switch (type)
            {
                case SaveType.SteamToSwitch:
                    {
                        SetPreferencesData();
                        SteamToSwitch();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Steam存档成功同步到Switch!");
                        Console.ForegroundColor = ConsoleColor.White;

                        break;
                    }

                case SaveType.SwitchToSteam:
                    {
                        SetPreferencesData();
                        SwitchToSteam();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Switch存档成功同步到Steam!");
                        Console.ForegroundColor = ConsoleColor.White;

                        break;
                    }
                case SaveType.SetPreferencesData:
                    {
                        Preferences = new PreferencesData();
                        SetPreferencesData();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("设置数据完成!");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    }

                default:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("操作类型无效!");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    }
            }


            Console.WriteLine("");

            SaveJson(currentDirectory + "/Preferences.json");

            ColseSwitch();
            Console.WriteLine("按任意键退出!");
            Console.ReadKey();
            Environment.Exit(0);
        }

        private static void SetPreferencesData()
        {


            From_SavePath_Steam = Preferences.Steam_Save_Path;
            if (From_SavePath_Steam == null || From_SavePath_Steam == string.Empty || !Directory.Exists(From_SavePath_Steam) || Preferences.Sync_Files == null || Preferences.Sync_Files.Count == 0)
            {
                string ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                From_SavePath_Steam = ApplicationData + @"\StardewValley\Saves";

                Console.WriteLine("Steam的存档位置：");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(From_SavePath_Steam);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("(正确/错误[Y/N])");

                while (true)
                {
                    ConsoleKeyInfo _key = Console.ReadKey();
                    Console.WriteLine();

                    if (_key.Key == ConsoleKey.Y || _key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }

                    if (_key.Key == ConsoleKey.N)
                    {
                        Console.WriteLine("请输入正确的Steam的存档路径：");
                        System.Diagnostics.Process.Start(ApplicationData);

                        while (true)
                        {
                            string path = Console.ReadLine();

                            if (!Directory.Exists(path))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("路径不存在，请重新输入！");
                                Console.ForegroundColor = ConsoleColor.White;
                                continue;
                            }

                            From_SavePath_Steam = path;
                            break;
                        }
                        break;

                    }
                }

                Preferences.Steam_Save_Path = From_SavePath_Steam;
                Console.WriteLine("---------------------------------------------");
                Preferences.Sync_Files = new List<string>();

                DirectoryInfo directoryInfo = new DirectoryInfo(From_SavePath_Steam);

                DirectoryInfo[] allDirectoryInfo = directoryInfo.GetDirectories();
                FileInfo[] allFileInfo = directoryInfo.GetFiles();
                int index = 0;
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (var item in allDirectoryInfo)
                {
                    Console.WriteLine(index + " - " + item.Name);
                    index++;
                }
                foreach (var item in allFileInfo)
                {
                    Console.WriteLine(index + " - " + item.Name);
                    index++;
                }
                Console.ForegroundColor = ConsoleColor.White;

            selectFile:
                Console.WriteLine("选择要同步的文件序号！");
                int selectIndex = ReadLine_Int(allDirectoryInfo.Length + allFileInfo.Length);
                string name = string.Empty;
                if (selectIndex < allDirectoryInfo.Length)
                {
                    name = (allDirectoryInfo[selectIndex].Name);
                }
                else
                {
                    name = (allFileInfo[selectIndex - allDirectoryInfo.Length].Name);
                }


                if (!Preferences.Sync_Files.Contains(name))
                    Preferences.Sync_Files.Add(name);

                Console.WriteLine("已选择同步文件：");
                Console.Write($"【");
                foreach (var item in Preferences.Sync_Files)
                {
                    Console.Write($"{item} , ");
                }
                Console.Write($"】");
                Console.WriteLine();

                Console.WriteLine("是否继续选择同步文件！（Y/N）");

                ConsoleKeyInfo key = Console.ReadKey();
                Console.WriteLine();

                if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter)
                {
                    goto selectFile;
                }
                else
                {
                    Console.WriteLine("选择同步文件结束");
                    Console.WriteLine("");
                }



            }



            if (!OpenSwitch())
            {
                Environment.Exit(1);
                return;
            }

            From_SavePath_Switch = Preferences.Switch_Save_Path;
            if (From_SavePath_Switch == null || From_SavePath_Switch == string.Empty || !Switchdevice.DirectoryExists(From_SavePath_Switch))
            {
                Console.WriteLine();
                MediaDirectoryInfo Installed_directoryInfo = Switchdevice.GetDirectoryInfo(@"7: Saves\Installed games");
                Console.WriteLine("Installed games");
                Console.WriteLine("---------------------------------------------");

                List<MediaDirectoryInfo> allInfo = new List<MediaDirectoryInfo>();
                Console.ForegroundColor = ConsoleColor.Green;
                int currsum = 0;
                foreach (MediaDirectoryInfo info in Installed_directoryInfo.EnumerateDirectories())
                {
                    string name = allInfo.Count + " - " + info.Name;
                    if (currsum > 3)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        currsum = 0;
                    }
                    else
                    {
                        currsum++;
                    }


                    string temp = "|" + name;
                    Console.Write(temp);
                    for (int i = temp.Length; i < 60; i++)
                    {
                        Console.Write(" ");
                    }



                    allInfo.Add(info);

                }
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine();
                Console.WriteLine();
                MediaDirectoryInfo Uninstalled_directoryInfo = Switchdevice.GetDirectoryInfo(@"7: Saves\Uninstalled games");
                Console.WriteLine("Uninstalled games");
                Console.WriteLine("---------------------------------------------");
                currsum = 0;
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (MediaDirectoryInfo info in Uninstalled_directoryInfo.EnumerateDirectories())
                {

                    string name = allInfo.Count + " - " + info.Name;
                    if (currsum > 3)
                    {
                        Console.WriteLine();
                        currsum = 0;
                    }
                    currsum++;

                    string temp = "|" + name;
                    Console.Write(temp);
                    for (int i = temp.Length; i < 60; i++)
                    {
                        Console.Write(" ");
                    }

                    allInfo.Add(info);

                }


                Console.WriteLine();
                Console.WriteLine("---------------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("输入要同步的游戏序号");


                int index = ReadLine_Int(allInfo.Count);

                MediaDirectoryInfo selectInfo = allInfo[index];

                Preferences.Switch_Game_Name = selectInfo.Name;

                allInfo.Clear();

                Console.WriteLine("");

                Console.WriteLine("---------------------------------------------");
                Console.WriteLine(selectInfo.Name);
                Console.WriteLine("");
                Console.WriteLine("All Switch user:");
                Console.ForegroundColor = ConsoleColor.Green;

                foreach (MediaDirectoryInfo _info in selectInfo.EnumerateDirectories())
                {
                    Console.WriteLine("序号：" + allInfo.Count + "  -----   " + _info.Name);
                    allInfo.Add(_info);

                }
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine("");
                Console.WriteLine("选择要同步的Switch用户序号");
                index = ReadLine_Int(allInfo.Count);

                MediaDirectoryInfo selectUser = allInfo[index];
                Preferences.Switch_User_Name = selectUser.Name;



                From_SavePath_Switch = selectUser.FullName;
                Console.WriteLine("Switch的存档位置：");
                Console.WriteLine("");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(From_SavePath_Switch);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("");
                Console.WriteLine("---------------------------------------------");
                Console.WriteLine("");

                Preferences.Switch_Save_Path = From_SavePath_Switch;
            }


            Console.WriteLine("当前设置 Preferences");

            foreach (var item in Preferences.GetType().GetFields())
            {
                object value = item.GetValue(Preferences);
                Console.Write($"{item.Name}:");
                Console.ForegroundColor = ConsoleColor.Green;
                if (value is List<string>)
                {
                    Console.Write("{");
                    List<string> list = (List<string>)value;
                    foreach (var str in list)
                    {
                        Console.Write(str + ",");
                    }
                    Console.Write("}");
                }
                else
                {
                    Console.Write($"{value.ToString()}");
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("");
            }


            Console.ForegroundColor = ConsoleColor.White;
        }

        private static int ReadLine_Int(int MaxSum)
        {
            string key = "";
            int index = -1;

            while (true)
            {
                key = Console.ReadLine();

                if (int.TryParse(key, out index))
                {
                    if (index < 0 || index >= MaxSum)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("输入索引超界，请重新输入");
                        Console.ForegroundColor = ConsoleColor.White;

                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("输入错误，重新输入");
                    Console.ForegroundColor = ConsoleColor.White;
                }

            }

            return index;
        }


        private static void SwitchToSteam()
        {

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("提取Switch文件到缓存目录");

            string To_SaveData_DirInfo = To_SaveData_Switch + @"\" + Preferences.Switch_Game_Name;

            if (!Directory.Exists(To_SaveData_DirInfo))
            {
                Directory.CreateDirectory(To_SaveData_DirInfo);
            }





            if (OpenSwitch())
            {
                //System.Diagnostics.Process.Start("Explorer.exe", sourceDic);

                string SwitchDic = Preferences.Switch_Save_Path;

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"正在提取Switch存档  ------------  请等待！");
                Console.WriteLine($"目标位置：" + (SwitchDic));
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine();
                Console.WriteLine();


                foreach (var files in Preferences.Sync_Files)
                {


                    string formPath = SwitchDic + @"\" + files;
                    string targetPath = To_SaveData_DirInfo + @"\" + files;

                    Console.WriteLine("--------------------------");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("提取Switch路径  ：" + formPath);
                    Switch_MoveTo_LocalDirectory(formPath, targetPath);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Switch存档提取成功：" + formPath);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("缓存路径：" + targetPath);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("--------------------------");



                }

                ColseSwitch();

                //从缓存目录复制到stem文件夹C哦、
                CopyStema();

                Console.WriteLine("--------------------------");

            }
            else
            {
                Environment.Exit(0);
            }






        }

        private static void CopyStema()
        {


            foreach (var fileName in Preferences.Sync_Files)
            {

                string form = To_SaveData_Switch + "/" + Preferences.Switch_Game_Name + "/" + fileName;
                string tar = From_SavePath_Steam + "/" + fileName;

                bool IsChinese = false;
                CopyFileOrDir(form, tar, ref IsChinese);


                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("复制存档到Stema存档完成：" + fileName);
                Console.WriteLine("路径：" + From_SavePath_Steam + "/" + fileName);
                Console.ForegroundColor = ConsoleColor.White;



            }

        }

        private static void SteamToSwitch()
        {
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("复制文件到缓存目录");

            string To_SaveData_DirInfo = To_SaveData_Steam + @"\" + Preferences.Switch_Game_Name;

            if (Directory.Exists(To_SaveData_DirInfo))
            {
                Directory.Delete(To_SaveData_DirInfo, true);
            }
            Directory.CreateDirectory(To_SaveData_DirInfo);

            bool IsChinese = false;

            for (int i = 0; i < Preferences.Sync_Files.Count; i++)
            {

                var fileName = Preferences.Sync_Files[i];
                ////去除中文
                //string fileName = Pinyin.GetPinyin(name).Replace(" ", "");

                //if (!fileName.Equals(name))
                //{
                //    Console.WriteLine($"文件名修改：old --- [{name}]   new --- [{fileName}]");
                //    Preferences.Sync_Files[i] = fileName;
                //}

                Console.WriteLine("复制文件：" + fileName);

                CopyFileOrDir(From_SavePath_Steam + "/" + fileName, To_SaveData_DirInfo + "/" + fileName, ref IsChinese);


            }

            if (IsChinese)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("");
                Console.WriteLine("复制文件失败！！");
                Console.WriteLine("");
                Console.WriteLine("存档文件名包含中文，Switch大气层系统不支持中文读取路径！请修改文件名字");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("按任意键退出!");
                Console.ReadKey();
                Environment.Exit(0);

            }

            //对星露谷物语存档做修改

            if (Preferences.Switch_Game_Name == "Stardew Valley")
            {

                Console.WriteLine("对星露谷物语存档做出修改!");
                string savename = Preferences.Sync_Files[0];
                EditGameInfo_StardewValley(savename, To_SaveData_DirInfo + @"\" + savename + @"\" + savename
                    , To_SaveData_DirInfo + @"\" + savename + @"\SaveGameInfo");
            }



            CopySwitch();


        }

        private static void CopyFileOrDir(string sourPath, string targetPath, ref bool IsChinese)
        {

            string name = Path.GetFileNameWithoutExtension(sourPath);
            if (ContainsChinese(name))
            {
                IsChinese = true;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("文件包含中文：" + name);
                Console.WriteLine(sourPath);
                Console.ForegroundColor = ConsoleColor.White;
            }


            if (Directory.Exists(sourPath))
            {
                if (!Directory.Exists(targetPath) && !IsChinese)
                {
                    Directory.CreateDirectory(targetPath);
                }
                DirectoryInfo directoryInfo = new DirectoryInfo(sourPath);
                foreach (var item in directoryInfo.GetDirectories())
                {
                    CopyFileOrDir(sourPath + "/" + item.Name, targetPath + "/" + item.Name, ref IsChinese);
                }
                foreach (var fileInfo in directoryInfo.GetFiles())
                {
                    CopyFileOrDir(sourPath + "/" + fileInfo.Name, targetPath + "/" + fileInfo.Name, ref IsChinese);
                }


            }
            else
            {
                try
                {
                    if (!IsChinese)
                    {
                        File.Copy(sourPath, targetPath, true);
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"复制文件失败！{sourPath} ");
                    Console.WriteLine($"目标路径： {targetPath}");
                    Console.WriteLine("错误消息：" + e.Message);
                    Console.ForegroundColor = ConsoleColor.White;

                }


            }



        }

        private static bool ContainsChinese(string text)
        {


            // 正则表达式匹配中文字符
            return Regex.IsMatch(text, @"[\u4e00-\u9fff]");
        }

        private static void CopySwitch()
        {
            string sourceDic = To_SaveData_Steam + @"\" + Preferences.Switch_Game_Name;
            string SwitchDic = Preferences.Switch_Save_Path;


            if (OpenSwitch())
            {
                //System.Diagnostics.Process.Start("Explorer.exe", sourceDic);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Switch接收路径：" + (SwitchDic));
                Console.ForegroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"正在上传至Switch中  ------------  请等待！");
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine();
                Console.WriteLine();

                foreach (var files in Preferences.Sync_Files)
                {
                    string formPath = sourceDic + @"\" + files;
                    string targetPath = SwitchDic + @"\" + files;
                    LocalDirectory_MoveTo_Switch(formPath, targetPath);

                    Console.WriteLine("--------------------------");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"存档同步至Switch成功：");
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine("上传路径  ：" + formPath);
                    Console.WriteLine("Switch路径：" + targetPath);

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("--------------------------");

                }



                ColseSwitch();


            }
            else
            {
                Environment.Exit(0);
            }



        }

        private static bool OpenSwitch()
        {
            // 初始化 MTP 设备管理器
            if (Switchdevice != null) return true;

            //获取所有连接的 MTP 设备
            var devices = MediaDevice.GetDevices();
            foreach (var dev in devices)
            {
                if (dev.FriendlyName == "Switch")
                {
                    Switchdevice = dev;
                }

            }

            if (Switchdevice == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("未连接Switch");
                Console.ForegroundColor = ConsoleColor.White;

                return false;
            }

            //（假设是连接的 Switch）

            Switchdevice.Connect();

            Console.WriteLine($"连接成功: {Switchdevice.FriendlyName}");

            return true;


        }
        private static void Switch_MoveTo_LocalDirectory(string SwitchPath, string targetPath)
        {
            if (Switchdevice.DirectoryExists(SwitchPath))
            {
                // 传输文件夹
                try
                {
                    Switchdevice.DownloadFolder(SwitchPath, targetPath, true);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"提取Switch文件夹失败: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.White;
                    return;
                }


            }
            else
            {

                // 传输文件
                try
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"开始上传:  {SwitchPath}");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"目标位置： {targetPath}!");
                    Console.ForegroundColor = ConsoleColor.White;

                    // 检查目标文件是否存在
                    if (File.Exists(targetPath))
                    {
                        // 删除目标文件
                        File.Delete(targetPath);

                    }
                    Switchdevice.DownloadFile(SwitchPath, targetPath);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"提取Switch文件失败: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.White;
                    return;
                }

            }


        }


        private static void LocalDirectory_MoveTo_Switch(string localDirectoryPath, string targetPath)
        {
            if (Directory.Exists(localDirectoryPath))
            {

                // 定义要传输的文件路径和目标路径
                if (Switchdevice.DirectoryExists(targetPath))
                {
                    Switchdevice.DeleteDirectory(targetPath, true);
                }
                Switchdevice.CreateDirectory(targetPath);

                DirectoryInfo localDir = new DirectoryInfo(localDirectoryPath);

                foreach (var dir in localDir.GetDirectories())
                {
                    LocalDirectory_MoveTo_Switch(dir.FullName, targetPath + @"/" + dir.Name);
                }
                foreach (var fileInfo in localDir.GetFiles())
                {
                    LocalDirectory_MoveTo_Switch(fileInfo.FullName, targetPath + @"/" + fileInfo.Name);
                }



            }
            else
            {

                // 传输文件
                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"开始上传:  {localDirectoryPath}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"目标位置： {targetPath}!");
                    Console.ForegroundColor = ConsoleColor.White;


                    // 检查目标文件是否存在
                    if (Switchdevice.FileExists(targetPath))
                    {
                        // 删除目标文件
                        Switchdevice.DeleteFile(targetPath);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("目标文件已存在! 正在删除! 上传新文件！");
                    }
                    Switchdevice.UploadFile(localDirectoryPath, targetPath);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"上传成功:  " + targetPath);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"上传至Switch失败: ");
                    Console.WriteLine("错误消息：" + ex.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();
                    return;
                }

            }


        }
        private static void ColseSwitch()
        {

            if (Switchdevice == null) return;

            Switchdevice.Disconnect();

            Switchdevice = null;

        }

        private static void EditGameInfo_StardewValley(string savename, string baseInfo, string SaveGameInfo)
        {

            bool isface = false;
            //<zoomLevel>1.26</zoomLevel>
            if (File.Exists(baseInfo))
            {
                isface = false;
                string baseInfoStr = "";
                using (System.IO.StreamReader file = System.IO.File.OpenText(baseInfo))
                {
                    baseInfoStr = file.ReadToEnd();

                    //修改版本
                    int s_index = baseInfoStr.IndexOf("<gameVersion>");
                    int e_index = baseInfoStr.IndexOf("</gameVersion>");
                    if (s_index == -1 || e_index == -1)
                    {
                        Console.WriteLine(savename + "修改版本失败");
                    }
                    else
                    {
                        string replaceStr = baseInfoStr.Substring(s_index, e_index - s_index + 14);

                        baseInfoStr = baseInfoStr.Replace(replaceStr, "<gameVersion>1.6.9</gameVersion>");
                        isface = true;
                        Console.WriteLine(savename + "修改版本完成");
                    }

                    //修改视角等级
                    s_index = baseInfoStr.IndexOf("<zoomLevel>");
                    e_index = baseInfoStr.IndexOf("</zoomLevel>");

                    if (s_index == -1 || e_index == -1)
                    {
                        Console.WriteLine(savename + "修改视角失败");
                    }
                    else
                    {
                        string replaceStr = baseInfoStr.Substring(s_index, e_index - s_index + 12);

                        baseInfoStr = baseInfoStr.Replace(replaceStr, "<zoomLevel>0.8</zoomLevel>");
                        isface = true;
                        Console.WriteLine(savename + "修改视角完成");

                    }


                }
                if (isface)
                {
                    using (StreamWriter sw = new StreamWriter(baseInfo))
                    {
                        sw.Write(baseInfoStr);
                        Console.WriteLine(savename + " -- " + savename + " 文件修改完成");
                    }

                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" 文件不存在：" + baseInfo);
                Console.ForegroundColor = ConsoleColor.White;
            }

            if (File.Exists(SaveGameInfo))
            {
                isface = false;
                string saveGameInfoStr = "";
                using (System.IO.StreamReader file = System.IO.File.OpenText(SaveGameInfo))
                {
                    saveGameInfoStr = file.ReadToEnd();

                    int s_index = saveGameInfoStr.IndexOf("<gameVersion>");
                    int e_index = saveGameInfoStr.IndexOf("</gameVersion>");


                    if (s_index == -1 || e_index == -1)
                    {

                        Console.WriteLine("SaveGameInfo 文件修改失败");
                    }
                    else
                    {
                        string replaceStr = saveGameInfoStr.Substring(s_index, e_index - s_index + 14);


                        saveGameInfoStr = saveGameInfoStr.Replace(replaceStr, "<gameVersion>1.6.9</gameVersion>");
                        isface = true;
                        Console.WriteLine("SaveGameInfo 文件修改完成");

                    }


                }

                if (isface)
                {
                    using (StreamWriter sw = new StreamWriter(SaveGameInfo))
                    {
                        sw.Write(saveGameInfoStr);
                        Console.WriteLine(savename + " -- SaveGameInfo 文件修改完成");
                    }

                }
            }

        }

        /// <summary>
        /// 读取JSON文件
        /// </summary>
        /// <param name="key">JSON文件中的key值</param>
        /// <returns>JSON文件中的value值</returns>
        public static PreferencesData Readjson(string jsonfile)
        {


            if (File.Exists(jsonfile))
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonfile))
                {
                    string val = file.ReadToEnd();


                    PreferencesData preferences = JsonSerializer.Deserialize<PreferencesData>(val, options);



                    if (preferences != null)
                    {
                        return preferences;
                    }
                }
            }

            return new PreferencesData();


        }

        public static void SaveJson(string jsonfile)
        {
            if (File.Exists(jsonfile))
            {
                File.Delete(jsonfile);
            }

            string val = JsonSerializer.Serialize(Preferences, options);

            //Console.WriteLine(val);

            using (System.IO.StreamWriter file = File.CreateText(jsonfile))
            {
                file.Write(val);
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("保存数据 Preferences 完成!");
            Console.ForegroundColor = ConsoleColor.White;



        }






    }
}


