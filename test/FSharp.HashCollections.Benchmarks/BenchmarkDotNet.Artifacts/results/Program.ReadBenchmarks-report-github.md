``` ini

BenchmarkDotNet=v0.12.0, OS=arch 
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


```
|           Method | CollectionSize |       Mean |     Error |    StdDev |
|----------------- |--------------- |-----------:|----------:|----------:|
|       **GetHashMap** |             **10** |   **7.817 ns** | **0.0109 ns** | **0.0102 ns** |
| GetThirdPartyMap |             10 |  15.313 ns | 0.1553 ns | 0.1452 ns |
|       **GetHashMap** |            **100** |  **10.565 ns** | **0.0432 ns** | **0.0404 ns** |
| GetThirdPartyMap |            100 |  16.630 ns | 0.0467 ns | 0.0364 ns |
|       **GetHashMap** |           **1000** |   **8.580 ns** | **0.0659 ns** | **0.0617 ns** |
| GetThirdPartyMap |           1000 |  19.511 ns | 0.1392 ns | 0.1302 ns |
|       **GetHashMap** |         **100000** |  **20.591 ns** | **0.3748 ns** | **0.5375 ns** |
| GetThirdPartyMap |         100000 |  50.930 ns | 1.0045 ns | 2.1623 ns |
|       **GetHashMap** |         **500000** |  **76.574 ns** | **1.3555 ns** | **1.2680 ns** |
| GetThirdPartyMap |         500000 | 113.342 ns | 2.2625 ns | 2.5147 ns |
|       **GetHashMap** |         **750000** | **113.592 ns** | **1.8542 ns** | **1.7344 ns** |
| GetThirdPartyMap |         750000 | 125.598 ns | 2.3901 ns | 2.1188 ns |
|       **GetHashMap** |        **1000000** | **118.259 ns** | **1.9888 ns** | **1.8603 ns** |
| GetThirdPartyMap |        1000000 | 118.912 ns | 1.5640 ns | 1.3864 ns |
|       **GetHashMap** |        **5000000** | **136.655 ns** | **2.6383 ns** | **2.3387 ns** |
| GetThirdPartyMap |        5000000 | 207.126 ns | 5.7608 ns | 6.8578 ns |
|       **GetHashMap** |       **10000000** | **151.547 ns** | **1.7975 ns** | **1.6814 ns** |
| GetThirdPartyMap |       10000000 | 228.637 ns | 4.2868 ns | 3.8001 ns |
