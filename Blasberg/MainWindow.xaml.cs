using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Blasberg
{
    public partial class MainWindow : Window
    {
        #region Глобальные переменные
        BitsOperations BO = new BitsOperations();
        RSH rsh = new RSH();
        //public List<MemoryData> MemoryGridTable = new List<MemoryData>();
        //public List<ProgramData> DataGridTable = new List<ProgramData>();
        //public List<MyTimers> TimerGridTable = new List<MyTimers>();

        //124,126,93,1 - Исходное положение новое
        public List<int> InputData = new List<int>() { 0, 0, 0, 0 };
        public List<int> OutputData = new List<int>() { 0, 0, 0 };
        public List<int> MarkerData = Enumerable.Repeat(0, 20).ToList();

        public List<Timers> TimerData = new List<Timers>();
        public List<ProgramData> ProgramData = new List<ProgramData>();

        public Dictionary<string, string> DB = new Dictionary<string, string>();
        public Dictionary<int, int> StartEnd = new Dictionary<int, int>();

        public Dictionary<string, int> TimerSE = new Dictionary<string, int>();
        public Dictionary<string, int> TimerSA = new Dictionary<string, int>();

        public Dictionary<string, int> FrontP = new Dictionary<string, int>();
        public Dictionary<string, int> FrontN = new Dictionary<string, int>();

        public BackgroundWorker backgroundWorker = new BackgroundWorker();

        public Dictionary<string, int> Stek = new Dictionary<string, int>();
        #endregion
        #region Отправка выражения в парсер
        private bool Parse(string value)
        {
            var tokens = new Tokenizer(value).Tokenize();
            var parser = new Parser(tokens);
            return parser.Parse();
        }
        #endregion
        public MainWindow()
        {
            rsh.Connect();
            InitializeComponent();
            ReadFileDB();
        }
        #region Чтение файла с программой
        private string Path = "0000000d.AWL";
        private List<string> tempDB = new List<string>();
        private List<string> tempProgramList = new List<string>();
        private bool ReadFileDB()
        {
            if (!File.Exists(Path))
                return false;
            using (StreamReader fs = new StreamReader(Path, Encoding.Default))
            {
                int start = 0;
                while (true)
                {
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    if (temp.Contains("END_STRUCT"))
                        break;

                    if (start == 1)
                        tempDB.Add(temp);

                    if (temp.Contains("STRUCT") && start != 1)
                        start = 1;
                }
                ParseDB();
                start = 0;
                while (true)
                {
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    if (temp.Contains("FUNCTION FC") && start != 1)
                        start = 1;
                    if (start == 1 && !temp.Contains("NOP"))
                        tempProgramList.Add(temp);
                    if (temp.Contains(": NOP"))
                        tempProgramList.Add(temp);
                }
            }
            FillGrid();
            return true;
        }
        private void ParseDB()
        {
            foreach (var item in tempDB)
            {
                if (string.IsNullOrEmpty(item))
                    break;
                var itemNew = item;
                if (item.Contains("//"))
                    itemNew = item.Substring(0, item.IndexOf('/'));
                var tempFirstString = itemNew.Split('_');
                var tempSecondString = tempFirstString[1].Split('i');
                string tempIndex = "";
                if (tempSecondString.Count() > 1)
                    tempIndex = tempSecondString[0] + "." + tempSecondString[1];
                else
                    tempIndex = tempSecondString[0];
                var tempThirdString = tempFirstString[tempFirstString.Count() - 1].Split('=');
                if (tempThirdString.Count() > 1)
                {
                    var endOfString = tempThirdString[1].Trim();

                    if (endOfString.Contains(';'))
                        endOfString = endOfString.Remove(endOfString.Length - 1, 1);

                    DB.Add(tempIndex.Trim(), endOfString);
                }
                else
                {
                    if (tempThirdString[0].Contains("BOOL"))
                        DB.Add(tempIndex, "False");
                    else
                        DB.Add(tempIndex, "0");
                }
                var tempString = itemNew.Substring(itemNew.IndexOf('_') + 1); //Дважды удаляем до знака "_"
                tempString = tempString.Substring(tempString.IndexOf('_') + 1);
                var tempNameP = tempString.Split(':');
                //MemoryData result = new MemoryData("", "", "", "", "");
                if (tempNameP.Count() > 2)
                {
                    var value = tempNameP[2].Replace('=', ' ');
                    value = value.Replace(';', ' ');
                    //if (tempNameP[1].Contains("BOOL"))
                    //    result = new MemoryData(tempIndex, tempNameP[0].Trim(), "bool", value.Trim().ToLower(), value.Trim().ToLower());
                    //if (tempNameP[1].Contains("INT"))
                    //    result = new MemoryData(tempIndex, tempNameP[0].Trim(), "integer", value.Trim(), value.Trim());
                    //if (tempNameP[1].Contains("TIME"))
                    //    result = new MemoryData(tempIndex, tempNameP[0].Trim(), "timer", value.Trim(), value.Trim());
                    if (tempNameP[0].Contains("Stek") && tempNameP[0].Trim() != "Stek2" && tempNameP[0].Trim() != "Stek1")
                    {
                        var tempTimerData = value.ToLower().Split('#');
                        string tempTime;
                        int newTempTime;
                        if (tempTimerData[1].Contains("ms"))
                        {
                            tempTime = tempTimerData[1].Replace("ms", "");
                            newTempTime = Convert.ToInt32(tempTime);
                        }
                        else
                        {
                            tempTime = tempTimerData[1].Replace("s", "");
                            newTempTime = Convert.ToInt32(tempTime);
                            newTempTime = newTempTime * 1000;
                        }
                        Stek.Add(tempNameP[0].Trim(), newTempTime);
                    }
                }
                else
                {
                    //if (tempNameP[1].Contains("BOOL"))
                    //    result = new MemoryData(tempIndex, tempNameP[0].Trim(), "bool", "false", "false");
                    //if (tempNameP[1].Contains("INT"))
                    //    result = new MemoryData(tempIndex, tempNameP[0].Trim(), "integer", "0", "0");
                    //if (tempNameP[1].Contains("TIME"))
                    //    result = new MemoryData(tempIndex, tempNameP[0].Trim(), "timer", "0", "0");
                }
                //MemoryGridTable.Add(result);
            }
        }
        private void FillGrid()
        {
            var countKey = 0;
            //var countText = 0;
            foreach (string item in tempProgramList) // Загоняем в таблицу данные программы из файла
            {
                try
                {
                    if (item.Contains("NETWORK") || item.Contains("TITLE") || item.Contains("END") || item.Contains("FUNCTION FC") || item.Contains("VERSION") || item.Contains("BEGIN") || item.Contains("AUF   DB"))
                    {
                        //var result = new ProgramData(0, item, "", "");
                        //ProgramData.Add(result);
                        if (StartEnd.Count != 0) //Разбитие на подпрограммы
                        {
                            var lastStart = StartEnd.Last();
                            if (lastStart.Key == lastStart.Value)
                                StartEnd[lastStart.Key] = countKey ;
                        }
                        //countText++;
                    }
                    else if (item.Trim().Length != 0)
                    {
                        if (StartEnd.Count != 0) //Разбитие на подпрограммы
                        {
                            var lastStart = StartEnd.Last();
                            if (lastStart.Key != lastStart.Value)
                                StartEnd.Add(countKey + 1, countKey + 1);
                        }
                        else
                            StartEnd.Add(0, 0);

                        var itemSplit = item.Replace(';', ' ');
                        var stringData = itemSplit.Split(' ').ToList();
                        stringData.RemoveAll(RemoveEmpty);
                        countKey++;
                        if (stringData.Count > 2)
                        {
                            var result = new ProgramData(countKey, stringData[0], stringData[1], stringData[2]);
                            ProgramData.Add(result);
                            if (stringData.Contains("FP"))
                                FrontP.Add(countKey.ToString(), 1);
                            if (stringData.Contains("FN"))
                                FrontN.Add(countKey.ToString(), 0);
                        }
                        else if (stringData.Count == 2)
                        {
                            if (stringData.Contains("SPBNB"))
                            {
                                var result = new ProgramData(countKey, stringData[0], stringData[1], "");
                                ProgramData.Add(result);
                            }
                            else if (stringData.Contains("BLD"))
                            {
                                var result = new ProgramData(countKey, stringData[0], stringData[1], "");
                                ProgramData.Add(result);
                            }
                            else if (stringData.Contains("S5T"))
                            {
                                var stringTimer = stringData[1].Split('#');
                                var result = new ProgramData(countKey, stringData[0], stringTimer[0], stringTimer[1]);
                                ProgramData.Add(result);
                            }
                            else if (stringData.Contains("L"))
                            {
                                var result = new ProgramData(countKey, stringData[0], stringData[1], "");
                                ProgramData.Add(result);
                            }
                        }
                        else
                        {
                            var result = new ProgramData(countKey, stringData[0], "", "");
                            ProgramData.Add(result);
                        }
                    }
                }
                catch
                {
                }
            }
        }
        private bool RemoveEmpty(String s)
        {
            return s.Length == 0;
        }
        #endregion
    }
}
