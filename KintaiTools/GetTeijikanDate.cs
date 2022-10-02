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
                return new BadRequestObjectResult("�p�����[�^�ɖ�肪����܂��B" + System.Environment.NewLine + errInfo);
            }


            var dataList = GetTeijikanDateList(reqYear, reqMonth );
            return new JsonResult(dataList.ToArray());
        }
        
        private static List<TeijikanDateData> GetTeijikanDateList(int year, int month)
        {
            var retDataList = new List<TeijikanDateData>();
            // �w��N����1���̓��t���擾
            var dt = new DateTime(year, month, 1);
            // �j���f�B�N�V���i������
            var holidayDic = GetHolidayDic();
            string dayOfWeek = string.Empty;

            bool isKyuyoDay = false; 
            while(dt.Month == month)
            {
                isKyuyoDay = false;
                // �ܗ^��
                if ((dt.Month == 6 && dt.Day == 20) || (dt.Month == 12 && dt.Day == 10))
                {
                    AddKyuyoDay(retDataList, dt, holidayDic, "�ܗ^��");
                    isKyuyoDay = true;
                }

                // ������
                if (dt.Day == 25)
                {
                    AddKyuyoDay(retDataList, dt, holidayDic, "������");
                    isKyuyoDay =true;
                }

                // ���j��
                dayOfWeek = GetDayOfWeek(dt);
                if (!isKyuyoDay &&  string.Equals(dayOfWeek, "��"))
                {
                    // �j���łȂ���Β莞�ԓ�
                    if (!holidayDic.ContainsKey(dt))
                    {                        
                        var teijiData = new TeijikanDateData();
                        teijiData.Year = dt.Year;
                        teijiData.Month = dt.Month;
                        teijiData.Day = dt.Day;
                        teijiData.DayOfWeek = dayOfWeek;
                        teijiData.Remarks = "�莞�ԓ�";
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
                  ret = "��";
                    break;
                case DayOfWeek.Monday:
                  ret = "��";
                    break;
                case DayOfWeek.Tuesday:
                  ret = "��";
                    break;
                case DayOfWeek.Wednesday:
                  ret = "��";
                    break;
                case DayOfWeek.Thursday:
                  ret = "��";
                    break;
                case DayOfWeek.Friday:
                  ret = "��";
                    break;
                case DayOfWeek.Saturday:
                  ret = "�y";
                    break;
            }
            return ret;
        }

        private static Dictionary<DateTime, string> GetHolidayDic()
        {
            // ���t�{�́u�����̏j���vCSV
            string targetUrl = @"http://www8.cao.go.jp/chosei/shukujitsu/syukujitsu.csv";
            // TODO �������Â��̂� HTTPClient�ŏ��������B
            var client = new WebClient();

            byte[] buffer = client.DownloadData(targetUrl);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string dataStr = Encoding.GetEncoding("shift_jis").GetString(buffer);

            var dic = new Dictionary<DateTime, string>();

            string[] rows = dataStr.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            // 1�s�ڂ̓w�b�_�[���̂��ߓǂݔ�΂�
            // CSV�̃t�H�[�}�b�g��F2020/11/23,�ΘJ���ӂ̓�
            // ����)��L�̃t�H�[�}�b�g�͕ς��\������B
            rows.Skip(1).ToList().ForEach(row =>
            {
                var cols = row.Split(',');
                dic.Add(DateTime.ParseExact(cols[0], "yyyy/M/d", null), cols[1]);
            });

            return dic;
        }

        private static void AddKyuyoDay(List<TeijikanDateData> retDataList, DateTime dt, Dictionary<DateTime, string>  holidayDic, string remarks)
        {
            // DateTime�͒l�^�̂��߈ȉ��ŕ����\
            DateTime workDt = dt;
            string dayOfWeek = string.Empty;
            while (true)
            {
                // �y�A���A�j�̏ꍇ
                dayOfWeek = GetDayOfWeek(workDt);
                if (string.Equals(dayOfWeek, "�y") || 
                    string.Equals(dayOfWeek, "��") ||
                    holidayDic.ContainsKey(workDt))
                {
                    workDt = workDt.AddDays(-1);
                }
                else
                {
                    foreach (var data in retDataList )
                    {
                        // ���ɒ莞�ԓ��Ƃ��ēo�^����Ă����珈���𔲂���
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
