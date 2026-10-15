// Explicit rather than relying on Verify.XunitV3's own conditional implicit-usings injection
// (its buildTransitive props, gated on $(ImplicitUsings)) - that injection silently no-ops under
// the pinned Playwright Docker image's .NET SDK (10.0.301), breaking every test file's "Verify"
// call with CS0103. Harmless if the package's own injection does fire elsewhere: duplicate
// identical global usings are a no-op in C#.
global using VerifyTests;
global using VerifyXunit;
global using static VerifyXunit.Verifier;
global using Xunit;
