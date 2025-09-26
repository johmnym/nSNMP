using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace nSNMP.SMI.DataTypes.V1.Primitive
{
    public class ObjectIdentifier : PrimitiveDataType
    {
        public ObjectIdentifier(byte[] data) : base(data)
        {
        }

        public uint[] Value
        {
            get
            {
                if (Data == null) return Array.Empty<uint>();
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

        public static uint[] ConvertToUIntArray(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            string trimStart = data.TrimStart('.');

            var parts = trimStart.Split(new[] { '.' });

            var result = new List<uint>();
            
            foreach (var s in parts)
            {
                uint temp;
                
                if (uint.TryParse(s, out temp))
                {
                    result.Add(temp);
                }
                else
                {
                    throw new ArgumentException(string.Format("Parameter {0} is out of 32 bit unsigned integer range", s), "data");
                }
            }

            return result.ToArray();
        }

        public static ObjectIdentifier Create(string oid)
        {
            uint[] array = ConvertToUIntArray(oid);

            return Create(array);
        }

        public static ObjectIdentifier Create(uint[] oid)
        {
            var temp = new List<byte>();

            var first = (byte)((40 * oid[0]) + oid[1]);
            
            temp.Add(first);
            
            for (var i = 2; i < oid.Length; i++)
            {
                temp.AddRange(ConvertToBytes(oid[i]));
            }

            return new ObjectIdentifier(temp.ToArray());
        }

        private static IEnumerable<byte> ConvertToBytes(uint subIdentifier)
        {
            var result = new List<byte> { (byte)(subIdentifier & 0x7F) };
            
            while ((subIdentifier = subIdentifier >> 7) > 0)
            {
                result.Add((byte)((subIdentifier & 0x7F) | 0x80));
            }

            result.Reverse();
            
            return result;
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
