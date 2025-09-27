using System.IO;
using System.Linq;
using nSNMP.SMI.DataTypes;
using nSNMP.SMI.DataTypes.V1.Constructed;
using nSNMP.SMI.DataTypes.V1.Primitive;
using nSNMP.SMI.X690;

namespace nSNMP.SMI.PDUs
{
    /// <summary>
    /// SNMPv1 Trap PDU - has different structure than other PDUs
    /// Format: enterprise, agent-addr, generic-trap, specific-trap, time-stamp, variable-bindings
    /// </summary>
    public record TrapV1(byte[]? Data, ObjectIdentifier? Enterprise, nSNMP.SMI.DataTypes.V1.Primitive.IpAddress? AgentAddr, Integer? GenericTrap, Integer? SpecificTrap, TimeTicks? TimeStamp, Sequence? VarbindList) : IDataType
    {
        public TrapV1() : this(null, null, null, null, null, null, new Sequence(new IDataType[] { }))
        {
        }

        public static TrapV1 Create(byte[] data)
        {
            ReadOnlyMemory<byte> memory = new ReadOnlyMemory<byte>(data);

            var enterprise = (ObjectIdentifier)SMIDataFactory.Create(ref memory);
            var agentAddr = (nSNMP.SMI.DataTypes.V1.Primitive.IpAddress)SMIDataFactory.Create(ref memory);
            var genericTrap = (Integer)SMIDataFactory.Create(ref memory);
            var specificTrap = (Integer)SMIDataFactory.Create(ref memory);
            var timeStamp = (TimeTicks)SMIDataFactory.Create(ref memory);
            var varbindList = (Sequence)SMIDataFactory.Create(ref memory);

            return new TrapV1(data, enterprise, agentAddr, genericTrap, specificTrap, timeStamp, varbindList);
        }

        public byte[] ToBytes()
        {
            // TrapV1 structure: Enterprise, AgentAddr, GenericTrap, SpecificTrap, TimeStamp, VarbindList
            var elements = new IDataType[]
            {
                Enterprise ?? ObjectIdentifier.Create(new uint[] { 1, 3, 6, 1, 4, 1, 0 }), // Default enterprise OID
                AgentAddr ?? nSNMP.SMI.DataTypes.V1.Primitive.IpAddress.Create("0.0.0.0"),
                GenericTrap ?? Integer.Create(6), // enterpriseSpecific
                SpecificTrap ?? Integer.Create(0),
                TimeStamp ?? TimeTicks.Create(0),
                VarbindList ?? new Sequence(new IDataType[] { })
            };

            var childBytes = elements.SelectMany(element => element.ToBytes()).ToArray();
            return BEREncoder.EncodeTLV((byte)SnmpDataType.TrapPDU, childBytes);
        }
    }
}