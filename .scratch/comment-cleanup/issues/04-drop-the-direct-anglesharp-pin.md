# Drop the direct AngleSharp pin

Status: needs-triage

`test/D12Canvas.Tests/D12Canvas.Tests.csproj` references AngleSharp 1.5.2 directly. The project doesn't use AngleSharp itself. The reference is there to override the vulnerable AngleSharp version that bunit 2.8.4-preview pins (GHSA-pgww-w46g-26qg).

## Fix

When a bunit release raises its AngleSharp floor past the advisory, upgrade bunit and delete the direct reference. Until then the pin stays. This issue waits on bunit, so it's in triage rather than ready.
