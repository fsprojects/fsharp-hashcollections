``` ini

BenchmarkDotNet=v0.13.1, OS=arch 
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK=5.0.301
  [Host]     : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT DEBUG
  DefaultJob : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT


```
|                             Method | CollectionSize |       Mean |    Error |   StdDev |
|----------------------------------- |--------------- |-----------:|---------:|---------:|
|                         **AddHashMap** |           **1000** |   **200.2 ns** |  **1.87 ns** |  **1.75 ns** |
|                     AddToFSharpMap |           1000 |   510.8 ns |  7.52 ns |  7.04 ns |
|             AddToFSharpAdaptiveMap |           1000 |   212.6 ns |  1.08 ns |  1.01 ns |
|                    AddToFSharpXMap |           1000 |   489.5 ns |  3.63 ns |  3.40 ns |
| AddToSystemCollectionsImmutableMap |           1000 |   596.4 ns |  6.52 ns |  6.10 ns |
|               AddToFsharpxChampMap |           1000 |   261.0 ns |  3.03 ns |  2.83 ns |
|                         **AddHashMap** |         **100000** |   **308.4 ns** |  **2.89 ns** |  **2.56 ns** |
|                     AddToFSharpMap |         100000 |   991.1 ns | 10.59 ns |  9.91 ns |
|             AddToFSharpAdaptiveMap |         100000 |   338.9 ns |  4.93 ns |  4.61 ns |
|                    AddToFSharpXMap |         100000 |   677.0 ns |  6.28 ns |  5.88 ns |
| AddToSystemCollectionsImmutableMap |         100000 |   871.3 ns |  7.18 ns |  6.72 ns |
|               AddToFsharpxChampMap |         100000 |   280.6 ns |  2.47 ns |  2.31 ns |
|                         **AddHashMap** |         **500000** |   **379.1 ns** |  **3.99 ns** |  **3.73 ns** |
|                     AddToFSharpMap |         500000 |   950.5 ns | 13.09 ns | 12.24 ns |
|             AddToFSharpAdaptiveMap |         500000 |   384.4 ns |  4.19 ns |  3.92 ns |
|                    AddToFSharpXMap |         500000 |   814.9 ns | 10.85 ns | 10.15 ns |
| AddToSystemCollectionsImmutableMap |         500000 | 1,097.2 ns |  9.44 ns |  8.83 ns |
|               AddToFsharpxChampMap |         500000 |   237.7 ns |  2.19 ns |  2.05 ns |
|                         **AddHashMap** |         **750000** |   **381.0 ns** |  **3.16 ns** |  **2.96 ns** |
|                     AddToFSharpMap |         750000 |   990.5 ns |  7.37 ns |  6.54 ns |
|             AddToFSharpAdaptiveMap |         750000 |   383.0 ns |  2.92 ns |  2.59 ns |
|                    AddToFSharpXMap |         750000 |   798.4 ns |  6.80 ns |  6.02 ns |
| AddToSystemCollectionsImmutableMap |         750000 | 1,044.3 ns | 11.12 ns | 10.40 ns |
|               AddToFsharpxChampMap |         750000 |   302.1 ns |  2.67 ns |  2.50 ns |
|                         **AddHashMap** |        **1000000** |   **382.3 ns** |  **3.61 ns** |  **3.37 ns** |
|                     AddToFSharpMap |        1000000 | 1,022.0 ns | 12.34 ns | 11.54 ns |
|             AddToFSharpAdaptiveMap |        1000000 |   410.8 ns |  6.81 ns |  6.37 ns |
|                    AddToFSharpXMap |        1000000 |   810.5 ns |  9.86 ns |  9.23 ns |
| AddToSystemCollectionsImmutableMap |        1000000 | 1,140.3 ns |  8.67 ns |  8.11 ns |
|               AddToFsharpxChampMap |        1000000 |   298.6 ns |  2.54 ns |  2.37 ns |
|                         **AddHashMap** |        **5000000** |   **402.9 ns** |  **3.63 ns** |  **3.39 ns** |
|                     AddToFSharpMap |        5000000 | 1,195.5 ns | 15.27 ns | 13.54 ns |
|             AddToFSharpAdaptiveMap |        5000000 |   489.2 ns |  5.38 ns |  5.03 ns |
|                    AddToFSharpXMap |        5000000 |   878.5 ns |  9.37 ns |  8.77 ns |
| AddToSystemCollectionsImmutableMap |        5000000 | 1,247.1 ns | 13.09 ns | 12.24 ns |
|               AddToFsharpxChampMap |        5000000 |   286.8 ns |  2.55 ns |  2.39 ns |
|                         **AddHashMap** |       **10000000** |   **429.6 ns** |  **3.06 ns** |  **2.87 ns** |
|                     AddToFSharpMap |       10000000 | 1,269.8 ns | 19.55 ns | 17.33 ns |
|             AddToFSharpAdaptiveMap |       10000000 |   511.8 ns |  6.01 ns |  5.62 ns |
|                    AddToFSharpXMap |       10000000 |   913.2 ns |  6.76 ns |  6.32 ns |
| AddToSystemCollectionsImmutableMap |       10000000 | 1,392.1 ns | 15.02 ns | 14.05 ns |
|               AddToFsharpxChampMap |       10000000 |   229.3 ns |  2.28 ns |  2.13 ns |
