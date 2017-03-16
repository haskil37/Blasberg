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

        public Dictionary<string, string> DataBase = new Dictionary<string, string>();
        public Dictionary<int, int> StartEnd = new Dictionary<int, int>();

        public Dictionary<string, int> TimerSE = new Dictionary<string, int>();
        public Dictionary<string, int> TimerSA = new Dictionary<string, int>();

        public Dictionary<string, int> FrontP = new Dictionary<string, int>();
        public Dictionary<string, int> FrontN = new Dictionary<string, int>();

        public BackgroundWorker backgroundWorker = new BackgroundWorker();

        public Dictionary<string, int> Stekanie = new Dictionary<string, int>();
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
        private string Path = "3.AWL";
        private List<string> tempProgramList = new List<string>();
        private bool ReadFileDB()
        {
            if (!File.Exists(Path))
                return false;
                    
            List<string> tempDB = new List<string>();
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
                ParseDB(tempDB);
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
        private void ParseDB_Bool(ref Dictionary<string, string> DB, string currentString)
        {
            var splitCurrentString = currentString.Split(':');
            var tempAdressCurrentString = splitCurrentString[0].Split('_');
            var AdressCurrentString = tempAdressCurrentString[1].Split('i');

            var value = "false";
            if (splitCurrentString.Count() > 2)
            {
                value = splitCurrentString[2];
                value = value.Replace(";", "");
                value = value.Replace("=", "");
                value = value.Trim();
            }

            if (AdressCurrentString.Count() > 1)
                DB.Add(AdressCurrentString[0].Trim() + "." + AdressCurrentString[1].Trim(), value);
            else
                DB.Add(AdressCurrentString[0].Trim(), value);
        }
        private void ParseDB_Time(ref Dictionary<string, string> DB, string currentString)
        {
            var splitCurrentString = currentString.Split(':');
            var tempAdressCurrentString = splitCurrentString[0].Split('_');
            var AdressCurrentString = tempAdressCurrentString[1].Split('i');

            var time = 0;
            if (splitCurrentString.Count() > 2)
            {
                var splitTime = splitCurrentString[2].Split('#');
                var value = splitTime[1];
                value = value.Replace(";", "");
                value = value.Trim();

                if (value.Contains("ms"))
                    time = Convert.ToInt32(value.Replace("ms", ""));
                else
                    time = Convert.ToInt32(value.Replace("s", "")) * 1000;
            }

            if (AdressCurrentString.Count() > 1)
            {
                DB.Add(AdressCurrentString[0].Trim() + "." + AdressCurrentString[1].Trim(), time + "ms");
                Stekanie.Add(AdressCurrentString[0].Trim() + "." + AdressCurrentString[1].Trim(), time);
            }
            else
            {
                DB.Add(AdressCurrentString[0].Trim(), time + "ms");
                Stekanie.Add(AdressCurrentString[0].Trim(), time);
            }
        }
        private void ParseDB_Int(ref Dictionary<string, string> DB, string currentString)
        {
            var splitCurrentString = currentString.Split(':');
            var tempAdressCurrentString = splitCurrentString[0].Split('_');
            var AdressCurrentString = tempAdressCurrentString[1].Split('i');

            var value = "0";
            if (splitCurrentString.Count() > 2)
            {
                value = splitCurrentString[2];
                value = value.Replace(";", "");
                value = value.Replace("=", "");
                value = value.Trim();
            }

            if (AdressCurrentString.Count() > 1)
                DB.Add(AdressCurrentString[0].Trim() + "." + AdressCurrentString[1].Trim(), value);
            else
                DB.Add(AdressCurrentString[0].Trim(), value);
        }
        private void ParseDB(List<string> content)
        {
            foreach (var item in content)
            {
                if (string.IsNullOrEmpty(item))
                    break;

                var currentString = item.ToLower();
                if (item.Contains("//"))
                    currentString = item.Substring(0, item.IndexOf('/')).ToLower();

                if (currentString.Contains("bool"))
                    ParseDB_Bool(ref DataBase, currentString);

                if (currentString.Contains("s5time"))
                    ParseDB_Time(ref DataBase, currentString);

                if (currentString.Contains("int"))
                    ParseDB_Int(ref DataBase, currentString);
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
