using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using HtmlAgilityPack;

namespace MarkQuerier
{
    class Program
    {
        static KeyValuePair<string, string>[] schools;
        static System.IO.StreamWriter writer;

        static void Main(string[] args)
        {
            Initialize();

            Console.Write("选择要查询的学校序号或考号文件:");
            var num = Console.ReadLine();
            int i;
            while (true)
            {
                if (int.TryParse(num, out i))//不能解析字符串为数字  或 数字>=学校列表长度
                {
                    if (i > schools.Length || i < 0)
                    {
                        Console.Write("错误序号,请重新输入学校序号:");
                        num = Console.ReadLine();
                        continue;
                    }
                    else
                    {
                        FromSpecificSchool(i);
                        break;
                    }
                }
                else if (System.IO.File.Exists(num))
                {
                    FromFile(num);
                    break;
                }
                else
                {
                    Console.WriteLine("不存在文件");
                    continue;
                }
            }

            Console.ReadLine();
            Program_Exited(null, null);
        }

        private static void Initialize()
        {
            writer = new System.IO.StreamWriter("marks.csv", false, Encoding.UTF8);
            writer.AutoFlush = true;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            System.Diagnostics.Process.GetCurrentProcess().Exited += Program_Exited;
            AppDomain.CurrentDomain.ProcessExit += Program_Exited;

            var lines = System.IO.File.ReadAllLines("schoolnumber.csv");//读取学校列表
            schools = Array.ConvertAll(lines, str =>
            {
                var strs = str.Split(',');
                var kvp = new KeyValuePair<string, string>(strs[0], strs[1]);
                return kvp;
            });

            for (int i = 0; i < schools.Length; i++)//输出学校列表
            {
                var kvp = schools[i];
                Console.WriteLine("{0}:{1}", i, kvp.Value);
            }
            Console.WriteLine("{0}:All", schools.Length);
        }

        private static void FromSpecificSchool(int i)
        {
            Console.WriteLine("按回车即停止,保存为csv文件并退出");

            if (i == schools.Length)
            {
                for (int j = 0; j < schools.Length; j++)
                {
                    ExecuteQuerying(j);
                }
            }
            else
                ExecuteQuerying(i);
        }

        private static void FromFile(string file)
        {
            Console.WriteLine("按回车即停止,保存为csv文件并退出");

            var dict = Load(file);
            Parallel.ForEach(dict, item =>
            {
                if (string.IsNullOrEmpty(item.Key))
                {
                    return;//continue
                }
                new Query(new Student() { ExamReference = item.Key, Birth = item.Value, School = "广附" }, WriteMark, ReportFault).Execute();

            });
        }

        private static void ExecuteQuerying(int i)
        {
            var kvp = schools[i];
            QueryAllStudentsIn(kvp.Value, kvp.Key);
        }

        static void QueryAllStudentsIn(string schoolName, string schoolNumber)
        {
            for (int i = 1; i < 3; i++)//1&2 理科-文科
            {
                Parallel.For(1, 1000, (n, state) =>
                {
                    var examRef = string.Format("01{0}{1}", schoolNumber.Insert(2, i.ToString()), n.ToString().PadLeft(3, '0'));

                    var query = new Query(new Student() { ExamReference = examRef, School = schoolName }, WriteMark, ReportFault);
                    query.Execute();
                    if (!query.Successful)
                    {
                        state.Break();
                    }
                });
            }
        }

        /// <summary>
        /// 错误处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Program.ThrowError(e.ExceptionObject as Exception ?? new Exception());
        }

        static void Program_Exited(object sender, EventArgs e)
        {
            writer.Close();
            System.Diagnostics.Process.Start("marks.csv");
        }

        static Dictionary<string, string> Load(string file)
        {
            var refs = new Dictionary<string, string>();
            using (var reader = new System.IO.StreamReader(file, Encoding.Default))
            {
                while (!reader.EndOfStream)
                {
                    var strs = reader.ReadLine().Split(',');
                    switch (strs.Length)
                    {
                        case 1:
                            refs.Add(strs[0], null);
                            break;
                        case 2:
                            refs.Add(strs[0], strs[1]);
                            break;
                        case 3:
                            refs.Add(strs[0], strs[2]);
                            break;
                        default:
                            Program.ThrowError(new ArgumentException("file format is incorrrect"));
                            return refs;
                    }
                }
            }
            return refs;
        }

        /// <summary>
        /// 输出分数
        /// </summary>
        /// <param name="student"></param>
        static void WriteMark(Student student)
        {
            string line =
                string.Format("{0},{1},{2},{3},{4},{5}", student.ExamReference,
                                                         student.School,
                                                         student.Name,
                                                         student.Birth,
                                                         student.Mark,
                                                         student.Recruiting);
            ClearCurrentLine();
            Console.WriteLine(line);
            writer.WriteLine(line);
        }

        private static void ClearCurrentLine()
        {
            Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b");
        }

        static void ReportFault(Student student)
        {
            ClearCurrentLine();
            Console.Write("考号:{0} 无法查询", student.ExamReference);
        }

        /// <summary>
        /// 不抛出错误,保证查询继续进行
        /// </summary>
        /// <param name="e"></param>
        public static void ThrowError(Exception e)
        {
            //throw e;
            Console.Error.WriteLine(e.Message);
        }
    }
}
