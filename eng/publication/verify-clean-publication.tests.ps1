[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$verifier = Join-Path $PSScriptRoot "verify-clean-publication.ps1"
$canonicalManifest = Join-Path $PSScriptRoot "manifest.json"
$powershellCommand = Get-Command pwsh -ErrorAction SilentlyContinue
if ($null -eq $powershellCommand) {
    $powershellCommand = Get-Command powershell -ErrorAction Stop
}

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("clean-publication-tests-" + [Guid]::NewGuid().ToString("N"))
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
$script:Passed = 0
$script:Failed = 0

function Write-Utf8File {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Content
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    [IO.File]::WriteAllText($Path, $Content, [Text.UTF8Encoding]::new($false))
}

function Write-UnicodeFile {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    [IO.File]::WriteAllText($Path, $Content, [Text.Encoding]::Unicode)
}

function New-ApprovedManifest {
    param(
        [Parameter(Mandatory)][string]$Path,
        [scriptblock]$Mutate
    )

    $manifest = Get-Content -Raw -LiteralPath $canonicalManifest | ConvertFrom-Json
    foreach ($dependency in @($manifest.dependencies)) {
        $dependency.licenseEvidence = "https://example.invalid/license-evidence"
        $dependency.licenseStatus = "approved"
        $dependency.noticeStatus = "not-required"
    }
    if ($null -ne $Mutate) {
        & $Mutate $manifest
    }
    [IO.File]::WriteAllText($Path, ($manifest | ConvertTo-Json -Depth 30), [Text.UTF8Encoding]::new($false))
}

function New-CleanFixture {
    param([Parameter(Mandatory)][string]$CaseName)

    $caseRoot = Join-Path $testRoot $CaseName
    $source = Join-Path $caseRoot "source"
    $artifact = Join-Path $caseRoot "artifact"
    $control = Join-Path $caseRoot "control"
    [IO.Directory]::CreateDirectory((Join-Path $source "src")) | Out-Null
    [IO.Directory]::CreateDirectory($artifact) | Out-Null
    [IO.Directory]::CreateDirectory($control) | Out-Null

    Write-Utf8File -Path (Join-Path $source "src/NeutralApp.cs") -Content "namespace NeutralApp; public static class EntryPoint { public static int Run() => 0; }"
    Write-Utf8File -Path (Join-Path $source "README.md") -Content "# Neutral property-trading engine"
    Write-Utf8File -Path (Join-Path $source "LICENSE") -Content "Fixture license text"
    Write-Utf8File -Path (Join-Path $source "NOTICE") -Content "No fixture notices"
    Write-Utf8File -Path (Join-Path $source "CONTRIBUTING.md") -Content "Fixture contribution policy"
    [IO.File]::WriteAllBytes((Join-Path $artifact "NeutralApp.dll"), [Text.Encoding]::ASCII.GetBytes("NEUTRAL-BINARY"))
    Write-Utf8File -Path (Join-Path $artifact "NeutralApp.deps.json") -Content '{"runtimeTarget":{"name":"fixture"}}'

    $manifest = Join-Path $control "manifest.json"
    New-ApprovedManifest -Path $manifest

    return [pscustomobject]@{
        CaseRoot = $caseRoot
        Source = $source
        Artifact = $artifact
        Control = $control
        Manifest = $manifest
        Report = Join-Path $control "report.json"
    }
}

function Invoke-Verifier {
    param(
        [Parameter(Mandatory)][ValidateSet("Audit", "Publication")][string]$Mode,
        [Parameter(Mandatory)]$Fixture,
        [string]$ReportPath = $Fixture.Report
    )

    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $verifier,
        "-Mode", $Mode,
        "-Root", $Fixture.Source,
        "-ReportPath", $ReportPath,
        "-ManifestPath", $Fixture.Manifest
    )
    if ($Mode -eq "Publication") {
        $arguments += @("-ArtifactRoot", $Fixture.Artifact)
    }

    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = & $powershellCommand.Source @arguments 2>&1 | Out-String
        $processExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }
    return [pscustomobject]@{
        ExitCode = $processExitCode
        Output = $output
        ReportPath = $ReportPath
    }
}

function Get-Report {
    param([Parameter(Mandatory)]$Result)

    if (-not (Test-Path -LiteralPath $Result.ReportPath -PathType Leaf)) {
        return $null
    }
    return Get-Content -Raw -LiteralPath $Result.ReportPath | ConvertFrom-Json
}

function Assert-Test {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Body
    )

    try {
        & $Body
        Write-Host "PASS $Name"
        $script:Passed++
    }
    catch {
        Write-Host "FAIL $Name - $($_.Exception.Message)"
        $script:Failed++
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -ne $Actual) {
        throw "$Message Expected '$Expected', got '$Actual'."
    }
}

function Assert-HasViolation {
    param($Report, [string]$Code)
    if ($null -eq $Report -or @($Report.blockers | Where-Object code -eq $Code).Count -eq 0) {
        throw "Expected violation '$Code' was not reported."
    }
}

function Assert-HasFinding {
    param($Report, [string]$Code, [string]$RuleId)
    $matches = @($Report.permittedTransitionFindings | Where-Object {
        $_.code -eq $Code -and ([string]::IsNullOrWhiteSpace($RuleId) -or $_.ruleId -eq $RuleId)
    })
    if ($null -eq $Report -or $matches.Count -eq 0) {
        throw "Expected permitted transition finding '$Code/$RuleId' was not reported."
    }
}

function Initialize-AuditFixture {
    param([Parameter(Mandatory)]$Fixture)

    & git -C $Fixture.Source init --quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Could not initialize audit fixture repository."
    }
}

try {
    Assert-Test "clean publication fixture passes" {
        $fixture = New-CleanFixture "clean"
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 0 $result.ExitCode $result.Output
        $report = Get-Report $result
        Assert-Equal 0 $report.summary.blockers "Clean fixture should have no blockers."
        Assert-Equal 2 $report.schemaVersion "Report schema should be version 2."
        Assert-Equal 64 $report.manifestSha256.Length "Report should identify the exact manifest."
        $rawReport = Get-Content -Raw -LiteralPath $result.ReportPath
        if ($rawReport.IndexOf($fixture.Source, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
            $rawReport.IndexOf($fixture.Artifact, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "The report leaked an inspected absolute path."
        }
    }

    Assert-Test "legacy identity is permitted only in Audit" {
        $fixture = New-CleanFixture "legacy-identity-audit"
        Write-Utf8File -Path (Join-Path $fixture.Source "Monopoly.Core/Engine.cs") -Content "namespace Monopoly.Core; public sealed class Engine { }"
        Initialize-AuditFixture $fixture

        $audit = Invoke-Verifier -Mode Audit -Fixture $fixture
        Assert-Equal 0 $audit.ExitCode $audit.Output
        $auditReport = Get-Report $audit
        Assert-HasFinding $auditReport "ContentRule" "current-legacy-identity"
        Assert-Equal "permitted-transition" $auditReport.permittedTransitionFindings[0].disposition "Audit findings need an explicit disposition."

        $publication = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $publication.ExitCode "Legacy identity must block Publication."
        Assert-HasViolation (Get-Report $publication) "ContentRule"
    }

    Assert-Test "product-shaped content blocks Audit" {
        $fixture = New-CleanFixture "product-content-audit"
        Write-Utf8File -Path (Join-Path $fixture.Source "src/JailSquare.cs") -Content "namespace NeutralApp; public sealed class JailSquare { }"
        Initialize-AuditFixture $fixture

        $result = Invoke-Verifier -Mode Audit -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Product-shaped content must block Audit."
        $report = Get-Report $result
        Assert-HasViolation $report "ContentRule"
        Assert-Equal "blocker" $report.blockers[0].disposition "Audit violations need an explicit disposition."
    }

    Assert-Test "audit material is permitted only in Audit" {
        $fixture = New-CleanFixture "audit-material"
        Write-Utf8File -Path (Join-Path $fixture.Source "docs/clean-publication-audit.md") -Content "# Evidence`nJailSquare"
        Initialize-AuditFixture $fixture

        $audit = Invoke-Verifier -Mode Audit -Fixture $fixture
        Assert-Equal 0 $audit.ExitCode $audit.Output
        Assert-HasFinding (Get-Report $audit) "FileDisposition" "raw-publication-audit"

        $publication = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $publication.ExitCode "Audit material must not enter Publication."
        Assert-HasViolation (Get-Report $publication) "FileDisposition"
    }

    Assert-Test "pending spelling governance is permitted only in Audit" {
        $fixture = New-CleanFixture "spelling-governance"
        Write-Utf8File -Path (Join-Path $fixture.Source ".github/actions/spelling/allow.txt") -Content "temporary"
        Initialize-AuditFixture $fixture

        $audit = Invoke-Verifier -Mode Audit -Fixture $fixture
        Assert-Equal 0 $audit.ExitCode $audit.Output
        Assert-HasFinding (Get-Report $audit) "FileDisposition" "derived-spelling-configuration"

        $publication = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $publication.ExitCode "Pending spelling governance must block Publication."
        Assert-HasViolation (Get-Report $publication) "FileDisposition"
    }

    Assert-Test "retired repository reference blocks Audit" {
        $fixture = New-CleanFixture "retired-repository"
        Write-Utf8File -Path (Join-Path $fixture.Source "README.md") -Content "https://github.com/CodeCraftersMR/CCMR-Monopoly"
        Initialize-AuditFixture $fixture

        $result = Invoke-Verifier -Mode Audit -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "A retired repository reference must block Audit outside evidence files."
        Assert-HasViolation (Get-Report $result) "ContentRule"
    }

    Assert-Test "unclassified binary is rejected" {
        $fixture = New-CleanFixture "unknown-binary"
        Write-Utf8File -Path (Join-Path $fixture.Source "nested/unknown.bin") -Content "unclassified"
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Unknown binary should fail."
        $report = Get-Report $result
        Assert-HasViolation $report "UnclassifiedFile"
        Assert-HasViolation $report "UnknownBinary"
    }

    Assert-Test "unclassified file blocks Audit" {
        $fixture = New-CleanFixture "unknown-audit-file"
        Write-Utf8File -Path (Join-Path $fixture.Source "src/unclassified.bin") -Content "unknown"
        Initialize-AuditFixture $fixture
        $result = Invoke-Verifier -Mode Audit -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Unknown files must block Audit."
        Assert-HasViolation (Get-Report $result) "UnclassifiedFile"
    }

    Assert-Test "denylisted filename is rejected" {
        $fixture = New-CleanFixture "deny-filename"
        Write-Utf8File -Path (Join-Path $fixture.Source "src/Monopoly.cs") -Content "namespace NeutralApp;"
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Denylisted filename should fail."
        Assert-HasViolation (Get-Report $result) "ContentRule"
    }

    Assert-Test "UTF-8 denylisted content is rejected" {
        $fixture = New-CleanFixture "deny-utf8"
        Write-Utf8File -Path (Join-Path $fixture.Source "src/Legacy.cs") -Content "// Monopoly presentation marker"
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "UTF-8 denylisted content should fail."
        Assert-HasViolation (Get-Report $result) "ContentRule"
    }

    Assert-Test "UTF-16 denylisted artifact content is rejected" {
        $fixture = New-CleanFixture "deny-utf16"
        Write-UnicodeFile -Path (Join-Path $fixture.Artifact "Legacy.pdb") -Content "Monopoly presentation marker"
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "UTF-16 denylisted content should fail."
        Assert-HasViolation (Get-Report $result) "ContentRule"
    }

    Assert-Test "UTF-8 binary content and binary filename are rejected" {
        $fixture = New-CleanFixture "deny-binary"
        [IO.File]::WriteAllBytes(
            (Join-Path $fixture.Artifact "Monopoly.Core.dll"),
            [Text.Encoding]::UTF8.GetBytes("JailSquare"))
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Binary names and UTF-8 binary content must be scanned."
        Assert-HasViolation (Get-Report $result) "ContentRule"
    }

    Assert-Test "embedded inspected roots are rejected" {
        $fixture = New-CleanFixture "embedded-roots"
        Write-Utf8File -Path (Join-Path $fixture.Source "src/diagnostic.txt") -Content "source=$($fixture.Source)"
        [IO.File]::WriteAllBytes(
            (Join-Path $fixture.Artifact "NeutralApp.pdb"),
            [Text.Encoding]::UTF8.GetBytes("artifact=$($fixture.Artifact)"))
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Inspected filesystem roots must not be embedded."
        Assert-HasViolation (Get-Report $result) "EmbeddedLocalPath"
    }

    Assert-Test "private profile paths and suffixes are rejected" {
        $fixture = New-CleanFixture "private-profile"
        Write-Utf8File -Path (Join-Path $fixture.Source "local-profiles/sample.private.profile.json") -Content '{}'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Private profile conventions must be blocked."
        Assert-HasViolation (Get-Report $result) "ForbiddenPath"
    }

    Assert-Test "user-specific path content is rejected" {
        $fixture = New-CleanFixture "user-path"
        Write-Utf8File -Path (Join-Path $fixture.Source "src/diagnostic.txt") -Content 'C:\Users\sample\PrivateGameProfiles\profiles\sample.json'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "User-specific paths must be blocked."
        Assert-HasViolation (Get-Report $result) "ContentRule"
    }

    Assert-Test "forbidden save directory is rejected" {
        $fixture = New-CleanFixture "forbidden-save"
        Write-Utf8File -Path (Join-Path $fixture.Source "saves/state.json") -Content '{}'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Forbidden save directory should fail."
        Assert-HasViolation (Get-Report $result) "ForbiddenPath"
    }

    Assert-Test "logs diagnostics and build residue are rejected" {
        $fixture = New-CleanFixture "residue"
        Write-Utf8File -Path (Join-Path $fixture.Source "logs/session.log") -Content "runtime output"
        Write-Utf8File -Path (Join-Path $fixture.Source "diagnostics/session.txt") -Content "diagnostic output"
        Write-Utf8File -Path (Join-Path $fixture.Source "obj/generated.txt") -Content "generated output"
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Logs, diagnostics and build residue must be blocked."
        Assert-HasViolation (Get-Report $result) "ForbiddenPath"
    }

    Assert-Test "fixed product structure is rejected in Audit" {
        $fixture = New-CleanFixture "fixed-structure"
        Write-Utf8File -Path (Join-Path $fixture.Source "src/Shape.cs") -Content "// fixed 40-space board"
        Initialize-AuditFixture $fixture
        $result = Invoke-Verifier -Mode Audit -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "A fixed product-shaped structure must block Audit."
        Assert-HasViolation (Get-Report $result) "ContentRule"
    }

    Assert-Test "regional Version 1 fields are rejected" {
        $fixture = New-CleanFixture "regional-save-fields"
        Write-Utf8File -Path (Join-Path $fixture.Source "state.json") -Content '{"ChanceDeck":[],"GameLanguage":"legacy"}'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Regional save fields must be blocked."
        Assert-HasViolation (Get-Report $result) "ContentRule"
    }

    Assert-Test "legacy save signature is rejected" {
        $fixture = New-CleanFixture "save-signature"
        Write-Utf8File -Path (Join-Path $fixture.Source "state.json") -Content '{"Version":1,"Players":[],"CurrentPlayerId":1}'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Legacy save signature should fail."
        Assert-HasViolation (Get-Report $result) "SaveSignature"
    }

    Assert-Test "save schema is not mistaken for a legacy save" {
        $fixture = New-CleanFixture "save-schema"
        Write-Utf8File -Path (Join-Path $fixture.Source "schema.json") -Content '{"$schema":"https://json-schema.org/draft/2020-12/schema","type":"object","properties":{"version":{"type":"integer"},"players":{"type":"array"},"currentPlayerId":{"type":"integer"}}}'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 0 $result.ExitCode $result.Output
        $report = Get-Report $result
        Assert-Equal 0 @($report.permittedTransitionFindings | Where-Object code -eq "SaveSignature").Count "A JSON Schema is reference material, not a saved match."
    }

    Assert-Test "unresolved dependency blocks Publication" {
        $fixture = New-CleanFixture "dependency-publication"
        New-ApprovedManifest -Path $fixture.Manifest -Mutate {
            param($manifest)
            $manifest.dependencies[0].licenseEvidence = $null
            $manifest.dependencies[0].licenseStatus = "review-required"
            $manifest.dependencies[0].noticeStatus = "review-required"
        }
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Unresolved dependency should fail Publication."
        Assert-HasViolation (Get-Report $result) "DependencyReview"
    }

    Assert-Test "undeclared direct dependency is rejected" {
        $fixture = New-CleanFixture "dependency-inventory"
        Write-Utf8File -Path (Join-Path $fixture.Source "src/Fixture.csproj") -Content '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Unreviewed.Package" Version="1.2.3" /></ItemGroup></Project>'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Unreviewed direct dependency should fail."
        Assert-HasViolation (Get-Report $result) "DependencyInventory"
    }

    Assert-Test "unresolved dependency is reported but allowed in Audit" {
        $fixture = New-CleanFixture "dependency-audit"
        Initialize-AuditFixture $fixture
        New-ApprovedManifest -Path $fixture.Manifest -Mutate {
            param($manifest)
            $manifest.dependencies[0].licenseEvidence = $null
            $manifest.dependencies[0].licenseStatus = "review-required"
            $manifest.dependencies[0].noticeStatus = "review-required"
        }
        $result = Invoke-Verifier -Mode Audit -Fixture $fixture
        Assert-Equal 0 $result.ExitCode $result.Output
        $report = Get-Report $result
        Assert-HasFinding $report "DependencyReview" "dependency-review"
        Assert-Equal 0 $report.summary.blockers "Explicit pending governance should not block Audit."
    }

    Assert-Test "report path inside snapshot is rejected" {
        $fixture = New-CleanFixture "report-inside"
        $insideReport = Join-Path $fixture.Source "publication-audit.json"
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture -ReportPath $insideReport
        Assert-Equal 2 $result.ExitCode "Report inside snapshot should be a tool error."
        if (Test-Path -LiteralPath $insideReport) {
            throw "Rejected report path was still written inside the snapshot."
        }
    }

    Assert-Test "report path inside artifact is rejected" {
        $fixture = New-CleanFixture "report-inside-artifact"
        $insideReport = Join-Path $fixture.Artifact "publication-audit.json"
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture -ReportPath $insideReport
        Assert-Equal 2 $result.ExitCode "Report inside artifact should be a tool error."
        if (Test-Path -LiteralPath $insideReport) {
            throw "Rejected report path was still written inside the artifact tree."
        }
    }

    Assert-Test "invalid manifest returns exit code 2" {
        $fixture = New-CleanFixture "invalid-manifest"
        Write-Utf8File -Path $fixture.Manifest -Content '{"schemaVersion":99}'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 2 $result.ExitCode "Invalid manifest should be a tool error."
    }
}
finally {
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if ($resolvedTestRoot.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedTestRoot)) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}

Write-Host "$($script:Passed) passed, $($script:Failed) failed"
if ($script:Failed -gt 0) {
    exit 1
}
exit 0
