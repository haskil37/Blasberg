using System.Collections.Generic;
using RshCSharpWrapper;
using RshCSharpWrapper.RshDevice;
using System;

namespace Blasberg
{
    class RSH
    {
        const string BOARD_NAME = "LA48DPCI";
        RSH_API st;
        Device device = new Device(BOARD_NAME);
        RshBoardPortInfo bpi = new RshBoardPortInfo();
        public bool Connect()
        {
            st = device.OperationStatus;
            //Коннектимся к первой плате
            st = device.Connect(1);
            if (st != RSH_API.SUCCESS)
                return false;

            st = device.Get(RSH_GET.DEVICE_PORT_INFO, ref bpi);
            for (int i = 0; i < bpi.confs.Length; i++)
            {
                RshInitPort port = new RshInitPort();
                port.operationType = RshInitPort.OperationTypeBit.Write;
                port.portAddress = bpi.confs[i].address;
                port.portValue = 0x80;
                st = device.Init(port); //У первой платы все на вывод
            }

            //Сбрасываем все порты в 0, но т.к. у нас инверсия то в ff
            RshInitPort p = new RshInitPort();
            p.operationType = RshInitPort.OperationTypeBit.Write;
            p.portAddress = 0;
            p.portValue = 0xff;
            st = device.Init(p);
            p.portAddress = 1;
            p.portValue = 0xff;
            st = device.Init(p);
            p.portAddress = 2;
            p.portValue = 0xff;
            st = device.Init(p);
            //-------------------

            //Коннектимся ко второй плате
            st = device.Connect(2);
            if (st != RSH_API.SUCCESS)
                return false;

            st = device.Get(RSH_GET.DEVICE_PORT_INFO, ref bpi);
            for (int i = 0; i < bpi.confs.Length; i++)
            {
                RshInitPort port = new RshInitPort();
                port.operationType = RshInitPort.OperationTypeBit.Write;
                port.portAddress = bpi.confs[i].address;
                port.portValue = 0x9B;
                st = device.Init(port); //У второй платы все на ввод
            }
            return true;
        }
        public List<int> Read()
        {
            st = device.Connect(2);
            List<int> InputData = new List<int>();
            RshInitPort p = new RshInitPort();
            p.operationType = RshInitPort.OperationTypeBit.Read;
            for (uint i = 0; i < 3; i++)
            {
                p.portAddress = i;
                st = device.Init(p);
                if (st != RSH_API.SUCCESS)
                    return new List<int>() { 0, 0, 0, 0 };
                InputData.Add(Convert.ToInt32(p.portValue));
            }
            p.portAddress = 4;
            st = device.Init(p);
            if (st != RSH_API.SUCCESS)
                return new List<int>() { 0, 0, 0, 0 };
            InputData.Add(Convert.ToInt32(p.portValue));
            return InputData;
        }
        public bool Write(List<byte> outputData)
        {
            st = device.Connect(1);
            RshInitPort p = new RshInitPort();
            p.operationType = RshInitPort.OperationTypeBit.Write;
            for (int i = 0; i < 3; i++)
            {
                var byteToSave = Inverse(outputData[i]);
                p.portAddress = Convert.ToUInt16(i);
                p.portValue = byteToSave;
                st = device.Init(p);
                if (st != RSH_API.SUCCESS)
                    return false;
            }
            return true;
        }
        private byte Inverse(byte value)
        {
            BitsOperations BO = new BitsOperations();
            for (int i = 0; i <= 7; i++)
            {
                if (BO.Get(value, i))
                    BO.Set(ref value, i, 0);
                else
                    BO.Set(ref value, i, 1);
            }
            return value;
        }
    }
}