using SharpFuzz;

namespace nSNMP.Fuzz
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("nSNMP Fuzzing Harness");
                Console.WriteLine("Usage: nSNMP.Fuzz <target>");
                Console.WriteLine("Available targets:");
                Console.WriteLine("  ber-decoder     - Fuzz BER/DER decoder");
                Console.WriteLine("  snmp-message    - Fuzz SNMP message parsing");
                Console.WriteLine("  oid-parser      - Fuzz ObjectIdentifier parsing");
                Console.WriteLine("  usm-processor   - Fuzz USM security processing");
                Console.WriteLine("  varbind-parser  - Fuzz VarBind parsing");
                Console.WriteLine("  trap-parser     - Fuzz Trap message parsing");
                return;
            }

            var target = args[0].ToLowerInvariant();

            switch (target)
            {
                case "ber-decoder":
                    Fuzzer.Run(SimpleBerFuzzer.FuzzBerDecoder);
                    break;
                case "oid-parser":
                    Fuzzer.Run(OidParserFuzzer.FuzzOidParser);
                    break;
                case "snmp-message":
                case "usm-processor":
                case "varbind-parser":
                case "trap-parser":
                    Console.WriteLine($"Target '{target}' not fully implemented yet. Use 'ber-decoder' or 'oid-parser'.");
                    break;
                default:
                    Console.WriteLine($"Unknown target: {target}");
                    break;
            }
        }
    }
}