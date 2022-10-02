using KintaiTools.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KintaiTools
{
    public static class GetTeijikanDate
    {
        [FunctionName("GetTeijikanDate")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string reqYearStr = req.Query["reqyear"].ToString();
            string reqMonthStr = req.Query["reqmonth"].ToString();

            int reqYear = 0;
            int reqMonth = 0;
            try
            {
                reqYear = int.Parse(reqYearStr);
                reqMonth = int.Parse(reqMonthStr);
            }
            catch (Exception ex)
            {
                string errInfo = ex.Message; 
                return new BadRequestObjectResult("パラメータに問題があります。" + System.Environment.NewLine + errInfo);
            }


            var dataList = GetTeijikanDateList(reqYear, reqMonth );
            return new JsonResult(dataList.ToArray());
        }
        
        private static List<TeijikanDateData> GetTeijikanDateList(int year, int month)
        {
            var retDataList = new List<TeijikanDateData>();
            // 指定年月の1日の日付を取得
            var dt = new DateTime(year, month, 1);
            // 祝日ディクショナリ生成
            var holidayDic = GetHolidayDic();
            string dayOfWeek = string.Empty;

            bool isKyuyoDay = false; 
            while(dt.Month == month)
            {
                isKyuyoDay = false;
                // 賞与日
                if ((dt.Month == 6 && dt.Day == 20) || (dt.Month == 12 && dt.Day == 10))
                {
                    AddKyuyoDay(retDataList, dt, holidayDic, "賞与日");
                    isKyuyoDay = true;
                }

                // 給料日
                if (dt.Day == 25)
                {
                    AddKyuyoDay(retDataList, dt, holidayDic, "給料日");
                    isKyuyoDay =true;
                }

                // 水曜日
                dayOfWeek = GetDayOfWeek(dt);
                if (!isKyuyoDay &&  string.Equals(dayOfWeek, "水"))
                {
                    // 祝日でなければ定時間日
                    if (!holidayDic.ContainsKey(dt))
                    {                        
                        var teijiData = new TeijikanDateData();
                        teijiData.Year = dt.Year;
                        teijiData.Month = dt.Month;
                        teijiData.Day = dt.Day;
                        teijiData.DayOfWeek = dayOfWeek;
                        teijiData.Remarks = "定時間日";
                        retDataList.Add(teijiData);
                    }
                }
                dt = dt.AddDays(1);
            }
            
            return retDataList;
        }

        private static string GetDayOfWeek(DateTime dt)
        {
            string ret = string.Empty;

            DayOfWeek dow = dt.DayOfWeek;
            switch (dow) { 
                case DayOfWeek.Sunday:
                  ret = "日";
                    break;
                case DayOfWeek.Monday:
                  ret = "月";
                    break;
                case DayOfWeek.Tuesday:
                  ret = "火";
                    break;
                case DayOfWeek.Wednesday:
                  ret = "水";
                    break;
                case DayOfWeek.Thursday:
                  ret = "木";
                    break;
                case DayOfWeek.Friday:
                  ret = "金";
                    break;
                case DayOfWeek.Saturday:
                  ret = "土";
                    break;
            }
            return ret;
        }

        private static Dictionary<DateTime, string> GetHolidayDic()
        {
            // 内閣府の「国民の祝日」CSV
            string targetUrl = @"http://www8.cao.go.jp/chosei/shukujitsu/syukujitsu.csv";
            // TODO やり方が古いので HTTPClientで書き直す。
            var client = new WebClient();

            byte[] buffer = client.DownloadData(targetUrl);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string dataStr = Encoding.GetEncoding("shift_jis").GetString(buffer);

            var dic = new Dictionary<DateTime, string>();

            string[] rows = dataStr.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            // 1行目はヘッダー情報のため読み飛ばす
            // CSVのフォーマット例：2020/11/23,勤労感謝の日
            // 注意)上記のフォーマットは変わる可能性あり。
            rows.Skip(1).ToList().ForEach(row =>
            {
                var cols = row.Split(',');
                dic.Add(DateTime.ParseExact(cols[0], "yyyy/M/d", null), cols[1]);
            });

            return dic;
        }

        private static void AddKyuyoDay(List<TeijikanDateData> retDataList, DateTime dt, Dictionary<DateTime, string>  holidayDic, string remarks)
        {
            // DateTimeは値型のため以下で複製可能
            DateTime workDt = dt;
            string dayOfWeek = string.Empty;
            while (true)
            {
                // 土、日、祝の場合
                dayOfWeek = GetDayOfWeek(workDt);
                if (string.Equals(dayOfWeek, "土") || 
                    string.Equals(dayOfWeek, "日") ||
                    holidayDic.ContainsKey(workDt))
                {
                    workDt = workDt.AddDays(-1);
                }
                else
                {
                    foreach (var data in retDataList )
                    {
                        // 既に定時間日として登録されていたら処理を抜ける
                        if (data.Day == workDt.Day  )
                        {
                            break;
                        }
                    }
                    var teijiData = new TeijikanDateData();
                    teijiData.Year = workDt.Year;
                    teijiData.Month = workDt.Month;
                    teijiData.Day = workDt.Day;
                    teijiData.DayOfWeek = dayOfWeek;
                    teijiData.Remarks = remarks;
                    retDataList.Add(teijiData);
                    break;
                }
            }
        }
    }
}
