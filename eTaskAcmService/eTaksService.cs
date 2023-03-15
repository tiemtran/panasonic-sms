using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using IFData;
using IFData.Customs;
using IFData.Enums;
using IFData.Helper;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Reflection;
using System.ServiceModel;
using log4net;
using OfficeOpenXml.Table;

namespace eTaskAcmService
{
    public partial class eTaksService : ServiceBase
    {
        private static ILog log = LogManager.GetLogger("acm");

        public eTaksService()
        {
            InitializeComponent();
            Thread faxWorkerThread = new Thread(TaksServiceList);
            faxWorkerThread.Start();
        }

        public void TaksServiceList()
        {
            long idMax = 0;
            while (true)
            {
                if (DateTime.Now.Hour >= 9)
                {
                    var dateNow = DateTime.Now.Date.ToString("yyyyMMdd");
                    var campaigns = new long[] { 5695858903218918033, 5733440809860418665 };
                    string fileResultNowPath = @"C:\\Panasonic\\data\\result_" + dateNow + ".xlsx";
                    string fileIdMaxPath = @"C:\\Panasonic\\data\\id.txt";
                    // record result today
                    if (!System.IO.File.Exists(fileResultNowPath))
                    {
                        // Read file idMax != null
                        if (System.IO.File.Exists(fileIdMaxPath))
                        {
                            string fileCurrentIdMax = System.IO.File.ReadAllText(fileIdMaxPath);
                            idMax = Int32.Parse(fileCurrentIdMax);
                        }
                        var newAnalyticEvents =
                            DALHelper.GetAll<analytic_event>(
                                x => x.id > idMax & campaigns.Contains(x.campaign_id));
                        // Quay so tu list nay
                        var random = new Random();
                        List<PersonInfo> luckyPersons = new List<PersonInfo>();
                        if (!newAnalyticEvents.Any())
                            continue;
                        if (newAnalyticEvents.Count >= 20)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                var randomEvent = newAnalyticEvents[random.Next(newAnalyticEvents.Count)];
                                var person = JsonHelper.DeserializeSafely<PersonInfo>(randomEvent.event_object);
                                luckyPersons.Add(person);
                            }
                        }
                        else
                            for (int i = 0; i < newAnalyticEvents.Count; i++)
                            {
                                var person =
                                    JsonHelper.DeserializeSafely<PersonInfo>(newAnalyticEvents[i].event_object);
                                luckyPersons.Add(person);
                            }
                        // export Excel
                        DataTable dt = new DataTable();
                        dt.Columns.Add("FullName", typeof(String));
                        dt.Columns.Add("Phone", typeof(String));
                        dt.Columns.Add("Email", typeof(String));
                        dt.AcceptChanges();
                        foreach (var pesionInfoLucky in luckyPersons)
                        {
                            DataRow row = dt.NewRow();
                            row["FullName"] = pesionInfoLucky.Name;
                            row["Phone"] = pesionInfoLucky.Phone;
                            row["Email"] = pesionInfoLucky.Email;
                            dt.Rows.Add(row);
                            dt.AcceptChanges();
                        }
                        string exportFilePath = fileResultNowPath;
                        var newFile = new FileInfo(exportFilePath);
                        using (var package = new ExcelPackage(newFile))
                        {
                            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("NewSheet1");
                            worksheet.Cells["A1"].LoadFromDataTable(dt, true, TableStyles.None);
                            package.Save();
                        }
                        //Quay thưởng
                        if (luckyPersons.Count != 0)
                        {
                            var sms = new ServiceReference.JetNavSMSSoapClient();
                            foreach (var luckyPerson in luckyPersons)
                            {
                                string message =
                                "Panasonic chúc mừng khách hàng [" + luckyPerson.Name + "] may mắn nhận được phiếu mua hàng Co.opmart trị giá 100.000 VND trong chương trình máy nước nóng Panasonic. Chúng tôi sẽ liên lạc để tiến hành trao giải. Mọi thông tin chi tiết vui lòng gửi email đến maynuocnongpanasonic@gmail.com";
                                sms.SendMT(luckyPerson.Phone, "0899551539", message, "text", "test", 200, "10641", "awing", "a$%#@wing");
                                var smsInfo = new string[] { luckyPerson.Phone, luckyPerson.Name, message };
                                log.Info(smsInfo);
                            }
                        }

                        // Update idmax = max(id) cua list 
                        if (newAnalyticEvents.Count != 0)
                        {
                            var luckyPersonsId = newAnalyticEvents.Max(x => x.id);
                            idMax = luckyPersonsId;
                            FileStream fs = new FileStream(fileIdMaxPath, FileMode.OpenOrCreate,
                                FileAccess.Write);
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine(luckyPersonsId);
                            sw.Flush();
                            sw.Close();
                        }
                    }
                }
                Thread.Sleep(30 * 60 * 1000); // Cho 30 phut kiem tra 1 lan
            }
        }
    }

    protected override void OnStart(string[] args)
    {

    }
    protected override void OnStop()
    {

    }

}
}
