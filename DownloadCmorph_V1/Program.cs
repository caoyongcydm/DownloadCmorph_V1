using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaoYong.DataType;
using CaoYong.DataProc;
using CaoYong.SimpleLog;
using System.Diagnostics;
using System.IO;

namespace GetH8FromFTP_V1
{
    class Program
    {
        static void Main(string[] args)
        {
            ///////////////////////////////////////////////////////////////////////////////
            //介绍性开头
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("+++    Download Cmorph From FTP Server V1.0        +++");
            Console.WriteLine("+++++  Supported By CaoYong 2018.08.29       +++++++++");
            Console.WriteLine("+++++  QQ: 403637605                         +++++++++");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            ///////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////
            //打开计时器
            Stopwatch sw = new Stopwatch();  //创建计时器
            sw.Start();                      //开启计数器
            ///////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////
            //程序所需变量设置
            string appDir = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;                                   //程序启动文件夹(正式使用)
            //string appDir = @"D:/HourQPE_V2/DownloadCmorphData_V1/";                                                         //程序启动文件夹(调试使用)                                                                              
            Environment.CurrentDirectory = appDir;                                                                             //设置shell所在目录
            string logPath = StringProcess.DateReplace(appDir + "log/YYYYMMDD.txt", DateTime.Now, 000);                        //日志文件夹地址
            Log simpleLog = new Log(logPath);                                                                                  //建立log对象，用于日志的记录                                                                                                                                 //输出站点ID计算信息
            ///////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////
            try
            {
                ///////////////////////////////////////////////////////////////////////////
                //时间处理(北京时)
                DateTime dtNow = DateTime.Now;                         //程序启动时间（北京时）                          
                if (args.Length == 0)                                  //实时运算处理
                {
                    dtNow = DateTime.Now;                              //正式运行
                    //dtNow = new DateTime(2018, 12, 12, 00, 05, 00);  //调试运行
                }
                else if (args.Length == 1 && args[0].Length == 12)     //指定日期运算处理
                {
                    try
                    {
                        dtNow = DateTime.ParseExact(args[0], "yyyyMMddHHmm", System.Globalization.CultureInfo.CurrentCulture);
                    }
                    catch(Exception ex)
                    {
                        simpleLog.WriteError(ex.Message, 1);
                        simpleLog.WriteError("Date Args Content Is Not Right!", 1);
                        return;
                    }
                }
                else
                {
                    simpleLog.WriteError("Date Args Number Is Not Right!", 1);
                    return;
                }
                ///////////////////////////////////////////////////////////////////////////////

                /////////////////////////////////////////////////////////////////////////////
                //读取控制文件
                string paraFilePath = appDir + @"para/para.ini";                      //控制文件地址
                string remotePathSample = null;                                             //远程下载地址
                string bz2PathSample = null;                                                //原始bz2保存地址

                if (!File.Exists(paraFilePath))
                {
                    simpleLog.WriteError("Para File Is Not Exist!", 1);
                    return;
                }
                else
                {
                    FileStream paraFS = new FileStream(paraFilePath, FileMode.Open, FileAccess.Read);
                    StreamReader paraSR = new StreamReader(paraFS, Encoding.GetEncoding("gb2312"));
                    {
                        try
                        {
                            string strTmp = paraSR.ReadLine();
                            string[] strArrayTmp = strTmp.Split(new char[] { '=' });
                            remotePathSample = strArrayTmp[1].Trim();                                       //远程FTP地址
                            strTmp = paraSR.ReadLine();
                            strArrayTmp = strTmp.Split(new char[] { '=' });
                            bz2PathSample = strArrayTmp[1].Trim();                                          //下载ZIP
                        }
                        catch
                        {
                            simpleLog.WriteError("Para Content Is Not Right!", 1);
                            return;
                        }
                    }
                    paraSR.Close();
                    paraFS.Close();
                }
                ///////////////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////////////////
                for (int n = 0; n < 48; n++)                                                       //需要处理下载个数48,也就是下载最近的48小时的cmorph数据
                {
                    try
                    {
                        /////////////////////////////////////////////////////////////////////////////
                        //预备设置
                        DateTime dtBase = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, 00, 00).AddMinutes(-60 * n).AddHours(-8);    //转化为世界时
                        Console.WriteLine("Process Time: " + dtBase);
                        string[] remotePath = new string[1];
                        string[] bz2Path = new string[1];
                        string[] outputPath = new string[1];
                        remotePath[0] = StringProcess.DateReplace(remotePathSample, dtBase, 00);                                   //远程文件名1
                        bz2Path[0] = StringProcess.DateReplace(bz2PathSample, dtBase, 00);                                         //bz2文件名1
                        if (!Directory.Exists(Path.GetDirectoryName(bz2Path[0])))                                                  //需要创建bz2文件夹
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(bz2Path[0]));
                        }
                        /////////////////////////////////////////////////////////////////////////////

                        ///////////////////////////////////////////////////////////////////////////////
                        //下载数据
                        Console.WriteLine("Step1: Download The CMORPH bz2 Files...");
                        for (int i = 0; i < remotePath.Length; i++)
                        {
                            if (!File.Exists(bz2Path[i]) || new FileInfo(bz2Path[i]).Length < 1024)  //如果本地不存在文件，或者存在文件小于1024字节，则进行下载
                            {
                                /*
                                //下载方法2，利用axel多线程下载,Linux下解压方式                               
                                //Linux下载
                                Process p = new Process();
                                p.StartInfo.FileName = "sh";
                                p.StartInfo.CreateNoWindow = true;         //不显示程序窗口
                                p.StartInfo.UseShellExecute = false;       //是否使用操作系统shell启动
                                p.StartInfo.RedirectStandardInput = true;  //接受来自调用程序的输入信息
                                p.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
                                p.StartInfo.RedirectStandardError = true;  //重定向标准错误输出
                                p.Start();//启动程序
                                p.StandardInput.WriteLine("axel  -N -q -n 20 -o  " + bz2Path[i] + @" ftp://133.82.233.6/" + remotePath[i]);   //下载数据
                                p.StandardInput.WriteLine(@"exit");
                                p.StandardInput.AutoFlush = true;
                                p.StandardOutput.ReadToEnd();
                                p.StandardError.ReadToEnd();
                                p.WaitForExit(); //等待程序结束
                                */

                                //windows下载
                                Process p = new Process();
                                p.StartInfo.FileName = "cmd.exe";
                                p.StartInfo.CreateNoWindow = true;         //不显示程序窗口
                                p.StartInfo.UseShellExecute = false;       //是否使用操作系统shell启动
                                p.StartInfo.RedirectStandardInput = true;  //接受来自调用程序的输入信息
                                p.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
                                p.StartInfo.RedirectStandardError = true;  //重定向标准错误输出
                                p.Start();//启动程序
                                //p.standardinput.writeline(appdir + "axel/axel.exe -n -q -n 20 -o  " + bz2path[i] + " " + remotepath[i]);   //下载数据
                                p.StandardInput.WriteLine(appDir + "wget/wget.exe -N --no-check-certificate -O  " + bz2Path[i] + " " + remotePath[i]);   //下载数据
                                p.StandardInput.WriteLine(@"exit");
                                p.StandardInput.AutoFlush = true;
                                p.StandardOutput.ReadToEnd();
                                p.StandardError.ReadToEnd();
                                p.WaitForExit(); //等待程序结束
                            }
                            Console.Write("\rfinish {0,8:f2} % ", 100.0 * (i + 1) / remotePath.Length);
                        }
                        Console.WriteLine("\rfinish ok!                        ");
                        ///////////////////////////////////////////////////////////////////////////////

                        ///////////////////////////////////////////////////////////////////////////////
                        //解压bz2文件
                        Console.WriteLine("Step2: Decode The bz2 Files...");
                        for (int i = 0; i < bz2Path.Length; i++)
                        {
                            /*
                            //bz2解压，linux下解压方式
                            Process p = new Process();
                            p.StartInfo.FileName = "sh";
                            p.StartInfo.CreateNoWindow = true;         //不显示程序窗口
                            p.StartInfo.UseShellExecute = false;       //是否使用操作系统shell启动
                            p.StartInfo.RedirectStandardInput = true;  //接受来自调用程序的输入信息
                            p.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
                            p.StartInfo.RedirectStandardError = true;  //重定向标准错误输出
                            p.Start();//启动程序
                            p.StandardInput.WriteLine(@"bzip2 -dkf " + bz2Path[i]);   //该程序能判断有无解压过，如果解压过就不在进一步解压一次
                            p.StandardInput.WriteLine(@"exit");
                            p.StandardInput.AutoFlush = true;
                            p.StandardOutput.ReadToEnd();
                            p.StandardError.ReadToEnd();
                            p.WaitForExit(); //等待程序结束
                            */

                            //bz2解压， windows下解压方式                           
                            Process p = new Process();
                            p.StartInfo.FileName = "cmd.exe";
                            p.StartInfo.CreateNoWindow = true;         //不显示程序窗口
                            p.StartInfo.UseShellExecute = false;       //是否使用操作系统shell启动
                            p.StartInfo.RedirectStandardInput = true;  //接受来自调用程序的输入信息
                            p.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
                            p.StartInfo.RedirectStandardError = true;  //重定向标准错误输出
                            p.Start();//启动程序
                            p.StandardInput.WriteLine(appDir + @"gzip/gzip.exe -dkf " + bz2Path[i]);   //该程序能判断有无解压过，如果解压过就不在进一步解压一次
                            p.StandardInput.WriteLine(@"exit");
                            p.StandardInput.AutoFlush = true;
                            p.StandardOutput.ReadToEnd();
                            p.StandardError.ReadToEnd();
                            p.WaitForExit(); //等待程序结束

                            Console.Write("\rfinish {0,8:f2} %", 100.0 * (i + 1) / bz2Path.Length);
                        }
                        Console.WriteLine("\rfinish ok!                              ");
                        ///////////////////////////////////////////////////////////////////////////////
                    }
                    catch (Exception ex)
                    {
                        simpleLog.WriteError(ex.Message,1);
                        continue;
                    }
                }// 处理循环
            }
            catch (Exception ex)
            {
                simpleLog.WriteError(ex.Message, 1);
            }
            ///////////////////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////
            sw.Stop();
            simpleLog.WriteInfo("time elasped: " + sw.Elapsed, 1);
            ///////////////////////////////////////////////////////////////////////////////
        }
    }
}
