# [<](../../README.md)

# Breaking-Change Sentinel
`IVSoftware.WinOS.MSTest.Extensions` now exposes a small public-contract API for
compatibility analysis.

That API ships in the NuGet package.

The MSTest projects discussed below do **not** ship in the NuGet package. They
are repository examples and regression harnesses that demonstrate how to use the
API against published witness versions and current source builds.

___

## Shipped API

The core public surface is:

- `ToPublicContract(this Assembly assembly, ManifestTypePolicy policy = ManifestTypePolicy.AssemblyOnly)`
- `ToPublicManifest(this Type type, Assembly asm, ManifestTypePolicy policy = ManifestTypePolicy.AllowExternalReferences)`
- `IsContractValid(this string baselineXml, string revisionXml, ManifestTypePolicy policy)`
- `GetBreakingChanges(this string baselineXml, string revisionXml, ManifestTypePolicy policy)`
- `ReadManifestResourceFile<T>(this string resourcePath, ThrowOrAdvise throw = ThrowOrAdvise.ThrowSoft)`

Together, these methods let you:

1. emit a deterministic XML contract for a public assembly surface
2. store that contract as a baseline witness
3. compare a later build against that witness
4. extract grouped breaking-change diagnostics when the contract no longer holds

___

## Policy Scopes

### `ManifestTypePolicy.AssemblyOnly`

Use this when the question is:

- "Did this assembly preserve its own public API?"

External types are normalized to `[external]`.

### `ManifestTypePolicy.IVSoftwareAssembliesOnly`

Use this when the question is:

- "Did this assembly preserve its own public API plus the meaningful shape of
  IVSoftware dependencies?"

IVSoftware dependency identities are preserved rather than flattened.

___

## Actual IVS Internal XBoundObject Contract Sentinel

The project [repo](https://github.com/IVSoftware/IVSoftware.WinOS.MSTest.Extensions.git) demonstrates an actual in-house sentinel workflow. The validation target is: `IVSoftware.Portable.Xml.Linq.XBoundObject`.

There are two MSTest projects:

- `TestBench.PublicContractWitness.MSTest`
- `TestBench.PublicContractLatest.MSTest`

### Witness Project

The witness project references a **published** package version and uses the
current WinOS contract tooling to write a stable baseline XML contract.

For the current example, the witness target is:

- `IVSoftware.Portable.Xml.Linq.XBoundObject` `2.0.3`

Important distinction:

- The old package assembly is the **subject**
- `IVSoftware.WinOS.MSTest.Extensions` is the **tool provider**

So this pattern:

```csharp
typeof(IVSoftware.Portable.Xml.Linq.XBoundAttribute)
    .Assembly
    .ToPublicContract()
    .ToString();
```

means "emit a contract for the referenced `XBoundObject` package assembly using
the current WinOS contract tooling."

### Latest Project

The latest project references the **current source build under construction**
and compares it against embedded witness XML files produced from older published
versions.

For the current example, the latest project compares:

- Witness: `XBoundObject` `2.0.3`
- Revision: current `XBoundObject` source build (for example `2.0.4-beta`)

The embedded witness files are:

- `Witness\XBoundObject Version=2.0.3.xml`
- `Witness\XBoundObject Version=2.0.3.Dependencies.xml`

___

## Recommended Workflow

1. Generate a witness contract from a trusted published package.
2. Embed the witness XML into the latest comparison project.
3. Generate the current contract from the source build under construction.
4. Call `IsContractValid(...)`.
5. If invalid, call `GetBreakingChanges(...)`.

## Important Rule

The validation policy and diff policy must match.

Good:

- Validate with `AssemblyOnly`, diff with `AssemblyOnly`
- Validate with `IVSoftwareAssembliesOnly`, diff with `IVSoftwareAssembliesOnly`

Bad:

- Validate with one policy and diff with another

If they differ, the diagnostic no longer describes the same scope that caused
the failure.

## Why The Witness Files Are Long

The witness XML is intentionally exact.

That is not noise. That is the point.

The figure of merit is:

- Every character must match verbatim

Because the MSTest witness files live only in the repository test harness, this
bulk does not affect the customer-facing NuGet payload.
