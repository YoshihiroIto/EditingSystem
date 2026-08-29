# Jewelry.EditingSystem Benchmarks

Run the reviewed hot-path benchmarks in Release mode:

```shell
dotnet run -c Release --project EditingSystem/Jewelry.EditingSystem.Benchmarks -- --filter "*"
```

Useful focused runs:

```shell
dotnet run -c Release --project EditingSystem/Jewelry.EditingSystem.Benchmarks -- --filter "*PropertyHistory*"
dotnet run -c Release --project EditingSystem/Jewelry.EditingSystem.Benchmarks -- --filter "*BatchHistory*"
dotnet run -c Release --project EditingSystem/Jewelry.EditingSystem.Benchmarks -- --filter "*SetHistory*"
dotnet run -c Release --project EditingSystem/Jewelry.EditingSystem.Benchmarks -- --filter "*Move*"
```

The benchmark project uses BenchmarkDotNet with `MemoryDiagnoser` so both elapsed time and managed allocations are tracked.
