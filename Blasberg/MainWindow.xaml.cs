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
                List<string> tempProgramList = new List<string>();
                while (true)
                {
                    string temp = fs.ReadLine();
                    if (temp == null) break;
                    if (temp.Contains("FUNCTION FC") && start != 1)
                        start = 1;
                    
                    if (start == 1 && temp.Trim().Length != 0)
                        if (!temp.Contains("NOP")
                            && !temp.Contains("FUNCTION")
                            && !temp.Contains("VERSION")
                            && !temp.Contains("BEGIN")
                            && !temp.Contains("NETWORK")
                            && !temp.Contains("AUF")
                            || temp.Contains(": NOP"))
                            tempProgramList.Add(temp);
                }
                ParseProgramCode(tempProgramList);
                //FillGrid();
            }
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
                time = ParseTime(splitCurrentString[2]);

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
        private int ParseTime(string content)
        {
            var time = 0;
            var splitTime = content.Split('#');
            var value = splitTime[1];
            value = value.Replace(";", "");
            value = value.Trim();

            if (value.Contains("ms"))
                time = Convert.ToInt32(value.Replace("ms", ""));
            else
                time = Convert.ToInt32(value.Replace("s", "")) * 1000;

            return time;
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
        private void ParseProgramCode(List<string> content)
        {
            int key = 0;
            foreach (var item in content)
            {
                if (item.Contains("TITLE"))
                {
                    if (StartEnd.Count != 0) //Разбитие на подпрограммы
                        StartEnd[StartEnd.Last().Key] = key - 1;
                }
                else
                {
                    if (StartEnd.Count != 0) //Разбитие на подпрограммы
                    {
                        if (StartEnd.Last().Key != StartEnd.Last().Value)
                            StartEnd.Add(key, key);
                    }
                    else
                        StartEnd.Add(key, key);
                    //Занесение кода программы в переменную 
                    var itemTemp = item.Replace(';', ' ');
                    var contentSplit = itemTemp.Split(' ').ToList();
                    contentSplit.RemoveAll(RemoveEmpty);
                    var firstValue = "";
                    var secondValue = "";
                    var thirdValue = "";
                    if (contentSplit.Count > 2)
                    {
                        firstValue = contentSplit[0].Trim();
                        secondValue = contentSplit[1].Trim();
                        thirdValue = contentSplit[2].Trim();
                        //Создание пременных циклов
                        if (contentSplit.Contains("FP"))
                            FrontP.Add(key.ToString(), 0);
                        if (contentSplit.Contains("FN"))
                            FrontN.Add(key.ToString(), 0);
                    }
                    else if (contentSplit.Count == 2)
                    {
                        if (contentSplit.Contains("S5T"))
                            thirdValue = ParseTime(contentSplit[1]).ToString();
                        firstValue = contentSplit[0].Trim();
                        secondValue = contentSplit[1].Trim();
                    }
                    else
                        firstValue = contentSplit[0].Trim();

                    var result = new ProgramData(key, firstValue, secondValue, thirdValue);
                    ProgramData.Add(result);
                    key++;
                }
            }
            StartEnd[StartEnd.Last().Key] = key - 1;
        }
        private bool RemoveEmpty(String s)
        {
            return s.Length == 0;
        }
        #endregion
    }
}
