```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M2, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.305
  [Host]   : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  .NET 9.0 : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Job=.NET 9.0  Runtime=.NET 9.0  InvocationCount=1  
UnrollFactor=1  

```
| Method          | Mean     | Error    | StdDev   | Min      | Max      | Median   | Allocated |
|---------------- |---------:|---------:|---------:|---------:|---------:|---------:|----------:|
| VarBindCreation | 319.7 ns | 23.69 ns | 69.48 ns | 209.0 ns | 542.0 ns | 334.0 ns |      32 B |
