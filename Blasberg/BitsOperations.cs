namespace Blasberg
{
    public class BitsOperations
    {        
        public void Set(ref byte aByte, int pos, int value)
        {
            if (value == 1)
                Set(ref aByte, pos, true);
            else
                Set(ref aByte, pos, false);
        }
        public void Set(ref byte aByte, int pos, bool value)
        {
            if (value)
                aByte = (byte)(aByte | (1 << pos));
            else
                aByte = (byte)(aByte & ~(1 << pos));
        }
        public bool Get(byte aByte, int pos)
        {            
            return ((aByte & (1 << pos)) != 0);
        }
    }
}