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
    if ($null -eq $Report -or @($Report.violations | Where-Object code -eq $Code).Count -eq 0) {
        throw "Expected violation '$Code' was not reported."
    }
}

try {
    Assert-Test "clean publication fixture passes" {
        $fixture = New-CleanFixture "clean"
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 0 $result.ExitCode $result.Output
        $report = Get-Report $result
        Assert-Equal 0 $report.summary.violations "Clean fixture should have no violations."
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

    Assert-Test "forbidden save directory is rejected" {
        $fixture = New-CleanFixture "forbidden-save"
        Write-Utf8File -Path (Join-Path $fixture.Source "saves/state.json") -Content '{}'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Forbidden save directory should fail."
        Assert-HasViolation (Get-Report $result) "ForbiddenPath"
    }

    Assert-Test "legacy save signature is rejected" {
        $fixture = New-CleanFixture "save-signature"
        Write-Utf8File -Path (Join-Path $fixture.Source "state.json") -Content '{"Version":1,"Players":[],"CurrentPlayerId":1}'
        $result = Invoke-Verifier -Mode Publication -Fixture $fixture
        Assert-Equal 1 $result.ExitCode "Legacy save signature should fail."
        Assert-HasViolation (Get-Report $result) "SaveSignature"
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
        & git -C $fixture.Source init --quiet
        if ($LASTEXITCODE -ne 0) { throw "Could not initialize audit fixture repository." }
        New-ApprovedManifest -Path $fixture.Manifest -Mutate {
            param($manifest)
            $manifest.dependencies[0].licenseEvidence = $null
            $manifest.dependencies[0].licenseStatus = "review-required"
            $manifest.dependencies[0].noticeStatus = "review-required"
        }
        $result = Invoke-Verifier -Mode Audit -Fixture $fixture
        Assert-Equal 0 $result.ExitCode $result.Output
        $report = Get-Report $result
        if (@($report.findings | Where-Object code -eq "DependencyReview").Count -eq 0) {
            throw "Audit did not report the unresolved dependency."
        }
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
