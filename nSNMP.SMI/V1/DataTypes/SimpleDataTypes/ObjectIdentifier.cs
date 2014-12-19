using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace nSNMP.SMI.V1.DataTypes.SimpleDataTypes
{
    public class ObjectIdentifier : SimpleDataType
    {
        public ObjectIdentifier(byte[] data) : base(data)
        {
        }

        public uint[] Value
        {
            get
            {
                var oid = new List<uint>();
                oid.Add((uint) Data[0] / 40);
                oid.Add((uint) Data[0] % 40);

                uint buffer = 0;

                for (var i = 1; i < Data.Length; i++)
                {
                    if ((Data[i] & 0x80) == 0)
                    {
                        oid.Add(Data[i] + (buffer << 7));
                        buffer = 0;
                    }
                    else
                    {
                        buffer <<= 7;
                        buffer += (uint)(Data[i] & 0x7F);
                    }
                }

                return oid.ToArray();
            }
        }

        public static ObjectIdentifier Create(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            var parts = data.Split(new[] { '.' });

            var result = new List<byte>();
            
            foreach (var s in parts)
            {
                uint temp;
                
                if (uint.TryParse(s, out temp))
                {
                    result.Add(Convert.ToByte(temp));
                }
                else
                {
                    throw new ArgumentException(string.Format("Parameter {0} is out of 32 bit unsigned integer range", s), "data");
                }
            }

            return new ObjectIdentifier(result.ToArray());
        }

        public override string ToString()
        {
            var oid = Value;

            if (oid == null)
            {
                throw new ArgumentNullException();
            }

            var result = new StringBuilder();
            
            foreach (uint section in oid)
            {
                result.Append(".").Append(section.ToString(CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }
    }
}
