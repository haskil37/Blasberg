namespace Blasberg
{    public class ProgramData
    {
        public ProgramData(int Key, string Operator, string AEM, string Bit)
        {
            this.Key = Key;
            //this.Code = Code;
            this.Operator = Operator;
            this.AEM = AEM;
            this.Bit = Bit;
            //this.Input = Input;
            //this.Marker = Marker;
            //this.Output = Output;
        }
        public int Key { get; set; }
        //public string Code { get; set; }
        public string Operator { get; set; }
        public string AEM { get; set; }
        public string Bit { get; set; }
        //public string Input { get; set; }
        //public string Marker { get; set; }
        //public string Output { get; set; }
    }
    public class Timers
    {
        public string Address { get; set; }
        public int Time { get; set; }
        public int EndTime { get; set; }
        public int Value { get; set; }
    }
}
