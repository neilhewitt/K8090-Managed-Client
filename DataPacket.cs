using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K8090.ManagedClient
{
    public class DataPacket
    {
        private const byte BEGIN_TX = 0x04;
        private const byte END_TX = 0x0F;

        public int SizeInBytes => 7;

        public readonly Command Command;
        public readonly byte Mask;
        public readonly byte Param1;
        public readonly byte Param2;
        public readonly byte Checksum;

        public byte[] AsByteArray => new byte[] { BEGIN_TX, (byte)Command, Mask, Param1, Param2, Checksum, END_TX };

        public override string ToString()
        {
            string output = "";
            foreach(byte b in AsByteArray)
            {
                output += b.ToString("X2") + " ";
            }
            return output.Trim();
        }

        public DataPacket(byte[] serialData)
        {
            Command = (Command)serialData[1];  
            Mask = serialData[2];
            Param1 = serialData[3];
            Param2 = serialData[4];
            Checksum = serialData[5];
        }

        public DataPacket(Command command, byte mask, byte param1, byte param2)
        {
            Command = command;
            Mask = mask;
            Param1 = param1;
            Param2 = param2;
            Checksum = (byte)((-(BEGIN_TX + (byte)Command + Mask + Param1 + Param2)));
        }
    }
}
