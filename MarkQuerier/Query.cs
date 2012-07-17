using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace MarkQuerier
{
    class Query
    {
        static readonly string[] birthmonths = { "9301", "9302", "9303", "9304", "9305", "9306", "9307", "9308", "9309", "9310", "9311", "9312", 
                                                "9401", "9402", "9403", "9404", "9405", "9406", "9407", "9408", "9409", "9410", "9411", "9412",
                                                "9201", "9202", "9203", "9204", "9205", "9206", "9207", "9208", "9209", "9210", "9211", "9212", 
                                                "9501", "9502", "9503", "9504", "9505", "9506", "9507", "9508", "9509", "9510", "9511", "9512"};

        public event Action<Student> Querried;
        public event Action<Student> Fault;
        public Student Info { get; private set; }
        public bool Successful { get; set; }

        public Query(string examRef, Action<Student> action = null, Action<Student> action2 = null)
            : this(new Student() { ExamReference = examRef }, action, action2)
        {

        }

        public Query(Student info, Action<Student> action, Action<Student> fault)
        {
            if (info.ExamReference.Length == 9)//少0格式
            {
                info.ExamReference.PadLeft(10, '0');
            }

            if (info.ExamReference.Length == 10)
            {
                this.Info = info;
                this.Querried = action;
                this.Fault = fault;
            }
            else
            {
                throw new ArgumentException("考号不正确", "info");
            }
        }

        public void Execute()
        {
            try
            {
                if (!string.IsNullOrEmpty(Info.Birth))
                {
                    if (QueryMarkOnce())
                    {
                        Done();
                        return;
                    }
                }

                if (QueryMarkTask())
                {
                    Done();
                }
                else
                {
                    var temp = Fault;
                    if (temp != null)
                    {
                        temp(this.Info);
                    }
                    //Successful = false;    default
                }
            }
            catch (Exception e)
            {
                Program.ThrowError(e);
            }
        }

        private void Done()
        {
            QueryRecruiting(GetRecruiting(this.Info.ExamReference, this.Info.Birth));
            var temp = Querried;
            if (temp != null)
            {
                temp(this.Info);
            }
            Successful = true;
        }

        private void ExecuteTask(object state)
        {
            Execute();
        }

        public async void ExecuteAsync()
        {
            await Task.Factory.StartNew(ExecuteTask, this.Info);
        }

        private bool QueryMarkOnce()
        {
            HtmlDocument page;
            page = GetMark(this.Info.ExamReference, this.Info.Birth);
            return (QueryMark(page));
        }

        private bool QueryMarkTask()
        {
            HtmlDocument page;

            foreach (var mon in birthmonths)
            {
                page = GetMark(this.Info.ExamReference, mon);
                if (QueryMark(page))
                {
                    this.Info.Birth = mon;
                    return true;
                }
            }
            return false;
        }

        private bool QueryMark(HtmlDocument doc)
        {
            var marksHtml = doc.DocumentNode.LastChild.LastChild.PreviousSibling.ChildNodes[7];
            var children = marksHtml.ChildNodes;
            if (children.Count == 5)
            {
                var tr = marksHtml.ChildNodes[3].ChildNodes;
                this.Info.Name = tr[3].ChildNodes[3].InnerText;
                var text = tr[9].InnerText.Trim();
                this.Info.Mark = text.Substring(text.Length - 3).Trim();
                return true;
            }
            else if (children.Count == 3)
            {
                //失败!!!
                //mark = string.Empty;
                this.Info.Mark = "unknow";
                return false;
            }
            else
            {
                this.Info.Mark = "unknow";
                Program.ThrowError(new ArgumentException("unknow", "doc"));
                return false;
            }
        }

        private bool QueryRecruiting(HtmlDocument doc)
        {
            var body = doc.DocumentNode.LastChild.ChildNodes[3].ChildNodes;
            if (body.Count == 17)
            {
                var recruitingHtml = body[9].ChildNodes[3].ChildNodes[1].ChildNodes[7];
                this.Info.Recruiting = recruitingHtml.InnerText.Trim();
                return true;
            }
            else if (body.Count == 15)
            {
                //失败!!!
                this.Info.Recruiting = "unknow";
                return false;
            }
            else
            {
                this.Info.Recruiting = "unknow";
                Program.ThrowError(new ArgumentException("unknow", "doc"));
                return false;
            }
        }

        static HtmlDocument GetMark(string examRef, string birth)
        {
            string url = string.Format("http://wap.5184.com/NCEE_WAP/controller/examEnquiry/performExamEnquiryWithoutAuthForGZ?categoryCode=CE_1&examReferenceNo={0}&birthday={1}&mobileNo=13760703318&examYear=2012&userName=%E9%82%B1%E5%B0%9A%E6%98%AD&redirected_url=http://wap.wirelessgz.cn/myExamWeb/wap/school/gaokao/myUniversity!main.action", examRef, birth);
            return new HtmlWeb().Load(url);
        }

        /// <summary>
        /// 不是我起的名字...教育网起的..查询录取
        /// </summary>
        /// <param name="examRef"></param>
        /// <param name="birth"></param>
        /// <returns></returns>
        static HtmlDocument GetRecruiting(string examRef, string birth)
        {
            string url = string.Format("http://wap.5184.com/NCEE_WAP/controller/examEnquiry/performRecruitedEnquiryWithoutAuth?categoryCode=CE_1&examReferenceNo={0}&birthday={1}&mobileNo=13760703318&examYear=2012&userName=0104133004&redirected_url=http://wap.wirelessgz.cn/myExamWeb/wap/school/gaokao/myUniversity!main.action", examRef, birth);
            return new HtmlWeb().Load(url);
        }


    }
}
