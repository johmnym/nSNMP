using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nSNMP.Tests
{
    public class SnmpMessageFactory
    {
        public static byte[] CreateMessage()
        {
            var data = new Byte[72];
            data[0] = (byte)48;
            data[1] = (byte)70;
            data[2] = (byte)2;
            data[3] = (byte)1;
            data[4] = (byte)0;
            data[5] = (byte)4;
            data[6] = (byte)6;
            data[7] = (byte)112;
            data[8] = (byte)117;
            data[9] = (byte)98;
            data[10] = (byte)108;
            data[11] = (byte)105;
            data[12] = (byte)99;
            data[13] = (byte)162;
            data[14] = (byte)57;
            data[15] = (byte)2;
            data[16] = (byte)4;
            data[17] = (byte)121;
            data[18] = (byte)192;
            data[19] = (byte)109;
            data[20] = (byte)237;
            data[21] = (byte)2;
            data[22] = (byte)1;
            data[23] = (byte)0;
            data[24] = (byte)2;
            data[25] = (byte)1;
            data[26] = (byte)0;
            data[27] = (byte)48;
            data[28] = (byte)43;
            data[29] = (byte)48;
            data[30] = (byte)41;
            data[31] = (byte)6;
            data[32] = (byte)8;
            data[33] = (byte)43;
            data[34] = (byte)6;
            data[35] = (byte)1;
            data[36] = (byte)2;
            data[37] = (byte)1;
            data[38] = (byte)1;
            data[39] = (byte)1;
            data[40] = (byte)0;
            data[41] = (byte)4;
            data[42] = (byte)29;
            data[43] = (byte)72;
            data[44] = (byte)80;
            data[45] = (byte)32;
            data[46] = (byte)69;
            data[47] = (byte)84;
            data[48] = (byte)72;
            data[49] = (byte)69;
            data[50] = (byte)82;
            data[51] = (byte)78;
            data[52] = (byte)69;
            data[53] = (byte)84;
            data[54] = (byte)32;
            data[55] = (byte)77;
            data[56] = (byte)85;
            data[57] = (byte)76;
            data[58] = (byte)84;
            data[59] = (byte)73;
            data[60] = (byte)45;
            data[61] = (byte)69;
            data[62] = (byte)78;
            data[63] = (byte)86;
            data[64] = (byte)73;
            data[65] = (byte)82;
            data[66] = (byte)79;
            data[67] = (byte)78;
            data[68] = (byte)77;
            data[69] = (byte)69;
            data[70] = (byte)78;
            data[71] = (byte)84;

            return data;
        }
    }
}
