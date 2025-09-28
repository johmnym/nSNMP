```

BenchmarkDotNet v0.15.4, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
Apple M2, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.305
  [Host]   : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a
  .NET 9.0 : .NET 9.0.9 (9.0.9, 9.0.925.41916), Arm64 RyuJIT armv8.0-a

Job=.NET 9.0  Runtime=.NET 9.0  InvocationCount=1  
UnrollFactor=1  

```
| Method               | Mean     | Error     | StdDev    | Min      | Max      | Median   | Allocated |
|--------------------- |---------:|----------:|----------:|---------:|---------:|---------:|----------:|
| AuthenticationSHA256 | 1.307 μs | 0.0606 μs | 0.1691 μs | 1.020 μs | 1.812 μs | 1.272 μs |     144 B |
