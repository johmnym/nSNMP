
using System;
using System.Collections.Generic;

namespace nSNMP.SMI.DataTypes
{
    public abstract class PrimitiveDataType : IDataType
    {
        public byte[] Data { get; private set; }

        protected PrimitiveDataType(byte[] data)
        {
            Data = data;
        }

        protected static byte[] GetRawBytes(IEnumerable<byte> data, bool negative)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            byte flag;
            byte sign;
            if (negative)
            {
                flag = 0xff;
                sign = 0x80;
            }
            else
            {
                flag = 0x0;
                sign = 0x0;
            }

            var list = new List<byte>(data);
            while (list.Count > 1)
            {
                if (list[list.Count - 1] == flag)
                {
                    list.RemoveAt(list.Count - 1);
                }
                else
                {
                    break;
                }
            }

            // if sign bit is not correct, add an extra byte
            if ((list[list.Count - 1] & 0x80) != sign)
            {
                list.Add(flag);
            }

            list.Reverse();
            return list.ToArray();
        }
    }
}