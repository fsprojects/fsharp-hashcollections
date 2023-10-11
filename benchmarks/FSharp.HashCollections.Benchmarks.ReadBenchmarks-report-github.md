``` ini

BenchmarkDotNet=v0.13.1, OS=arch 
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.403
  [Host]     : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT


```
|                           Method | CollectionSize |        Mean |     Error |    StdDev |
|--------------------------------- |--------------- |------------:|----------:|----------:|
|                       **GetHashMap** |             **10** |    **11.49 ns** |  **0.091 ns** |  **0.081 ns** |
|                GetImToolsHashMap |             10 |    31.58 ns |  0.190 ns |  0.169 ns |
|                     GetFSharpMap |             10 |    26.97 ns |  0.208 ns |  0.194 ns |
|                GetFSharpXHashMap |             10 |   129.21 ns |  0.806 ns |  0.754 ns |
| GetSystemCollectionsImmutableMap |             10 |    21.80 ns |  0.325 ns |  0.304 ns |
|         GetFSharpDataAdaptiveMap |             10 |    22.95 ns |  0.234 ns |  0.219 ns |
|               GetFSharpxChampMap |             10 |    72.75 ns |  0.439 ns |  0.367 ns |
|                    GetLangExtMap |             10 |    24.83 ns |  0.218 ns |  0.193 ns |
|                       **GetHashMap** |            **100** |    **11.75 ns** |  **0.120 ns** |  **0.112 ns** |
|                GetImToolsHashMap |            100 |    44.99 ns |  0.358 ns |  0.317 ns |
|                     GetFSharpMap |            100 |    47.45 ns |  0.418 ns |  0.391 ns |
|                GetFSharpXHashMap |            100 |   148.55 ns |  0.919 ns |  0.815 ns |
| GetSystemCollectionsImmutableMap |            100 |    33.26 ns |  0.246 ns |  0.230 ns |
|         GetFSharpDataAdaptiveMap |            100 |    44.84 ns |  0.400 ns |  0.374 ns |
|               GetFSharpxChampMap |            100 |    83.54 ns |  1.017 ns |  0.951 ns |
|                    GetLangExtMap |            100 |    31.26 ns |  0.264 ns |  0.247 ns |
|                       **GetHashMap** |           **1000** |    **11.67 ns** |  **0.090 ns** |  **0.084 ns** |
|                GetImToolsHashMap |           1000 |    69.63 ns |  0.974 ns |  0.911 ns |
|                     GetFSharpMap |           1000 |    71.16 ns |  0.762 ns |  0.713 ns |
|                GetFSharpXHashMap |           1000 |   161.89 ns |  0.539 ns |  0.450 ns |
| GetSystemCollectionsImmutableMap |           1000 |    49.93 ns |  0.489 ns |  0.458 ns |
|         GetFSharpDataAdaptiveMap |           1000 |    67.05 ns |  0.725 ns |  0.678 ns |
|               GetFSharpxChampMap |           1000 |    84.44 ns |  1.061 ns |  0.992 ns |
|                    GetLangExtMap |           1000 |    31.00 ns |  0.312 ns |  0.292 ns |
|                       **GetHashMap** |         **100000** |    **33.47 ns** |  **0.376 ns** |  **0.314 ns** |
|                GetImToolsHashMap |         100000 |   216.79 ns |  4.285 ns |  4.009 ns |
|                     GetFSharpMap |         100000 |   177.11 ns |  2.074 ns |  1.940 ns |
|                GetFSharpXHashMap |         100000 |   231.84 ns |  4.178 ns |  6.627 ns |
| GetSystemCollectionsImmutableMap |         100000 |   162.46 ns |  2.537 ns |  2.374 ns |
|         GetFSharpDataAdaptiveMap |         100000 |   154.96 ns |  2.743 ns |  2.566 ns |
|               GetFSharpxChampMap |         100000 |   114.73 ns |  1.243 ns |  1.163 ns |
|                    GetLangExtMap |         100000 |    61.52 ns |  0.609 ns |  0.570 ns |
|                       **GetHashMap** |         **500000** |    **35.67 ns** |  **0.622 ns** |  **0.582 ns** |
|                GetImToolsHashMap |         500000 |   528.33 ns | 10.475 ns | 12.063 ns |
|                     GetFSharpMap |         500000 |   327.39 ns |  6.410 ns | 11.881 ns |
|                GetFSharpXHashMap |         500000 |   336.89 ns |  5.012 ns |  4.688 ns |
| GetSystemCollectionsImmutableMap |         500000 |   359.41 ns |  7.174 ns | 11.787 ns |
|         GetFSharpDataAdaptiveMap |         500000 |   324.83 ns |  6.091 ns |  5.982 ns |
|               GetFSharpxChampMap |         500000 |   122.62 ns |  1.997 ns |  2.137 ns |
|                    GetLangExtMap |         500000 |    67.00 ns |  1.128 ns |  1.055 ns |
|                       **GetHashMap** |         **750000** |    **38.42 ns** |  **0.513 ns** |  **0.428 ns** |
|                GetImToolsHashMap |         750000 |   639.63 ns | 12.341 ns | 14.691 ns |
|                     GetFSharpMap |         750000 |   429.89 ns |  8.344 ns | 10.247 ns |
|                GetFSharpXHashMap |         750000 |   446.19 ns |  5.108 ns |  4.778 ns |
| GetSystemCollectionsImmutableMap |         750000 |   440.76 ns |  8.714 ns | 10.035 ns |
|         GetFSharpDataAdaptiveMap |         750000 |   364.25 ns |  7.225 ns |  6.758 ns |
|               GetFSharpxChampMap |         750000 |   128.73 ns |  2.571 ns |  3.848 ns |
|                    GetLangExtMap |         750000 |    72.10 ns |  1.426 ns |  1.334 ns |
|                       **GetHashMap** |        **1000000** |    **41.58 ns** |  **0.804 ns** |  **0.752 ns** |
|                GetImToolsHashMap |        1000000 |   716.12 ns | 13.576 ns | 12.699 ns |
|                     GetFSharpMap |        1000000 |   478.64 ns |  9.391 ns | 11.877 ns |
|                GetFSharpXHashMap |        1000000 |   426.63 ns |  4.339 ns |  4.059 ns |
| GetSystemCollectionsImmutableMap |        1000000 |   506.48 ns |  9.872 ns |  9.695 ns |
|         GetFSharpDataAdaptiveMap |        1000000 |   395.59 ns |  5.966 ns |  5.581 ns |
|               GetFSharpxChampMap |        1000000 |   132.05 ns |  2.585 ns |  4.319 ns |
|                    GetLangExtMap |        1000000 |    79.30 ns |  1.577 ns |  2.408 ns |
|                       **GetHashMap** |        **5000000** |   **155.94 ns** |  **0.957 ns** |  **0.799 ns** |
|                GetImToolsHashMap |        5000000 | 1,138.25 ns | 18.869 ns | 17.650 ns |
|                     GetFSharpMap |        5000000 |   823.41 ns | 10.984 ns | 10.274 ns |
|                GetFSharpXHashMap |        5000000 |   475.15 ns |  6.462 ns |  6.044 ns |
| GetSystemCollectionsImmutableMap |        5000000 |   826.09 ns | 15.213 ns | 14.230 ns |
|         GetFSharpDataAdaptiveMap |        5000000 |   579.94 ns |  6.645 ns |  6.216 ns |
|               GetFSharpxChampMap |        5000000 |   285.59 ns |  3.235 ns |  3.026 ns |
|                    GetLangExtMap |        5000000 |   234.92 ns |  3.571 ns |  3.341 ns |
|                       **GetHashMap** |       **10000000** |   **159.87 ns** |  **2.199 ns** |  **1.950 ns** |
|                GetImToolsHashMap |       10000000 | 1,345.84 ns | 26.844 ns | 26.364 ns |
|                     GetFSharpMap |       10000000 |   957.82 ns | 14.825 ns | 13.868 ns |
|                GetFSharpXHashMap |       10000000 |   509.87 ns |  9.244 ns |  8.647 ns |
| GetSystemCollectionsImmutableMap |       10000000 |   960.39 ns | 18.921 ns | 17.699 ns |
|         GetFSharpDataAdaptiveMap |       10000000 |   672.11 ns |  9.232 ns |  8.636 ns |
|               GetFSharpxChampMap |       10000000 |   291.37 ns |  3.615 ns |  3.381 ns |
|                    GetLangExtMap |       10000000 |   237.09 ns |  3.173 ns |  2.968 ns |
