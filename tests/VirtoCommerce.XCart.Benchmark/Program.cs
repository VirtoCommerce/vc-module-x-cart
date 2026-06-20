using System.Reflection;
using VirtoCommerce.XCart.Benchmark;

// Upstream (un-extended XCart) runner. The benchmark logic lives in the Core library's *BenchmarksBase
// abstract classes; the concrete subclasses that bake the upstream module setup are source-generated
// into THIS exe (see [assembly: BenchmarkSetup] below), so FromAssembly(this exe) discovers them.
BenchmarkProgram.Run(Assembly.GetExecutingAssembly(), args);
