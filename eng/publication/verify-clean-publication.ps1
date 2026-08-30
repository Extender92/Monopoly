[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("Audit", "Publication")]
    [string]$Mode,

    [Parameter(Mandatory)]
    [string]$Root,

    [string]$ArtifactRoot,

    [Parameter(Mandatory)]
    [string]$ReportPath,

    [string]$ManifestPath
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $PSScriptRoot "manifest.json"
}

$script:Findings = [System.Collections.Generic.List[object]]::new()
$script:Violations = [System.Collections.Generic.List[object]]::new()

function ConvertTo-NormalizedPath {
    param([Parameter(Mandatory)][string]$Path)

    $normalized = $Path.Replace("\", "/")
    while ($normalized.StartsWith("./", [StringComparison]::Ordinal)) {
        $normalized = $normalized.Substring(2)
    }
    return $normalized.TrimStart([char]"/")
}

function Get-RelativeNormalizedPath {
    param(
        [Parameter(Mandatory)][string]$BasePath,
        [Parameter(Mandatory)][string]$Path
    )

    $baseFull = [IO.Path]::GetFullPath($BasePath).TrimEnd([char[]]"\/") + [IO.Path]::DirectorySeparatorChar
    $pathFull = [IO.Path]::GetFullPath($Path)
    $baseUri = [Uri]$baseFull
    $pathUri = [Uri]$pathFull
    return ConvertTo-NormalizedPath ([Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()))
}

function Test-Glob {
    param(
        [Parameter(Mandatory)][string]$Value,
        [Parameter(Mandatory)][string]$Pattern
    )

    $normalizedValue = ConvertTo-NormalizedPath $Value
    $normalizedPattern = ConvertTo-NormalizedPath $Pattern
    if ($normalizedPattern -eq "**") {
        return $true
    }

    $expression = [Regex]::Escape($normalizedPattern)
    $expression = $expression.Replace("\*\*", ".*")
    $expression = $expression.Replace("\*", "[^/]*")
    $expression = $expression.Replace("\?", "[^/]")
    return [Regex]::IsMatch($normalizedValue, "^$expression$", [Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

function Test-AnyGlob {
    param(
        [Parameter(Mandatory)][string]$Value,
        [Parameter(Mandatory)][object[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        if (Test-Glob -Value $Value -Pattern ([string]$pattern)) {
            return $true
        }
    }

    return $false
}

function Test-PathInside {
    param(
        [Parameter(Mandatory)][string]$Candidate,
        [Parameter(Mandatory)][string]$Container
    )

    $candidateFull = [IO.Path]::GetFullPath($Candidate)
    $containerFull = [IO.Path]::GetFullPath($Container).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if ($candidateFull.Equals($containerFull, [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $prefix = $containerFull + [IO.Path]::DirectorySeparatorChar
    return $candidateFull.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
}

function Add-Result {
    param(
        [Parameter(Mandatory)][ValidateSet("finding", "violation")][string]$Kind,
        [Parameter(Mandatory)][string]$Code,
        [Parameter(Mandatory)][string]$Scope,
        [string]$Path,
        [Parameter(Mandatory)][string]$Message,
        [string]$Classification,
        [object[]]$OwnerIssues = @()
    )

    $item = [ordered]@{
        code = $Code
        scope = $Scope
        path = $Path
        message = $Message
        classification = $Classification
        ownerIssues = @($OwnerIssues | ForEach-Object { [int]$_ })
    }

    if ($Kind -eq "violation") {
        $script:Violations.Add([pscustomobject]$item)
    }
    else {
        $script:Findings.Add([pscustomobject]$item)
    }
}

function Assert-OwnerIssues {
    param(
        [Parameter(Mandatory)]$Value,
        [Parameter(Mandatory)][string]$Context
    )

    if ($null -eq $Value.ownerIssues -or @($Value.ownerIssues).Count -eq 0) {
        throw "$Context must declare at least one owner issue."
    }

    foreach ($issue in @($Value.ownerIssues)) {
        if ([int]$issue -le 0) {
            throw "$Context contains an invalid owner issue."
        }
    }
}

function Assert-Manifest {
    param([Parameter(Mandatory)]$Manifest)

    if ($Manifest.schemaVersion -ne 1) {
        throw "Unsupported manifest schema version '$($Manifest.schemaVersion)'."
    }

    foreach ($property in "policy", "fileRules", "contentRules", "allowRules", "dependencies", "forbiddenPaths", "requiredPublicationFiles", "textExtensions", "textFileNames", "artifactBinaryRules") {
        if ($null -eq $Manifest.$property) {
            throw "Manifest property '$property' is required."
        }
    }

    $validClassifications = @("allow", "replace", "remove", "review")
    $fileRuleIds = @{}
    foreach ($rule in @($Manifest.fileRules)) {
        if ([string]::IsNullOrWhiteSpace($rule.id) -or $fileRuleIds.ContainsKey([string]$rule.id)) {
            throw "File-rule IDs must be non-empty and unique."
        }
        $fileRuleIds[[string]$rule.id] = $true
        if (@($rule.patterns).Count -eq 0 -or $rule.classification -notin $validClassifications) {
            throw "File rule '$($rule.id)' has invalid patterns or classification."
        }
        Assert-OwnerIssues -Value $rule -Context "File rule '$($rule.id)'"
    }

    $contentRuleIds = @{}
    foreach ($rule in @($Manifest.contentRules)) {
        if ([string]::IsNullOrWhiteSpace($rule.id) -or $contentRuleIds.ContainsKey([string]$rule.id)) {
            throw "Content-rule IDs must be non-empty and unique."
        }
        $contentRuleIds[[string]$rule.id] = $true
        if ($rule.matchType -notin @("literal", "regex") -or @($rule.patterns).Count -eq 0 -or @($rule.targets).Count -eq 0 -or $rule.classification -notin $validClassifications) {
            throw "Content rule '$($rule.id)' has an invalid match contract."
        }
        if (@($rule.targets | Where-Object { $_ -notin @("path", "content") }).Count -gt 0) {
            throw "Content rule '$($rule.id)' has an unsupported target."
        }
        Assert-OwnerIssues -Value $rule -Context "Content rule '$($rule.id)'"
        if ($rule.matchType -eq "regex") {
            foreach ($pattern in @($rule.patterns)) {
                try {
                    [void][Regex]::new([string]$pattern)
                }
                catch {
                    throw "Content rule '$($rule.id)' has invalid regex '$pattern'."
                }
            }
        }
    }

    foreach ($rule in @($Manifest.allowRules)) {
        if ([string]::IsNullOrWhiteSpace($rule.id) -or @($rule.patterns).Count -eq 0 -or [string]::IsNullOrWhiteSpace($rule.rationale)) {
            throw "Every allow rule needs an ID, path patterns and a rationale."
        }
    }

    foreach ($rule in @($Manifest.forbiddenPaths)) {
        if (@($rule.patterns).Count -eq 0 -or $rule.classification -ne "remove") {
            throw "Forbidden-path rules must have patterns and classification 'remove'."
        }
        Assert-OwnerIssues -Value $rule -Context "Forbidden-path rule"
    }

    foreach ($requiredFile in @($Manifest.requiredPublicationFiles)) {
        if ([string]::IsNullOrWhiteSpace($requiredFile.path)) {
            throw "Required publication files need a path."
        }
        Assert-OwnerIssues -Value $requiredFile -Context "Required publication file '$($requiredFile.path)'"
    }

    foreach ($dependency in @($Manifest.dependencies)) {
        foreach ($property in "ecosystem", "name", "version", "upstream", "licenseStatus", "noticeStatus") {
            if ([string]::IsNullOrWhiteSpace($dependency.$property)) {
                throw "Every dependency must declare '$property'."
            }
        }
        Assert-OwnerIssues -Value $dependency -Context "Dependency '$($dependency.name)'"
    }
}

function Get-AuditFiles {
    param([Parameter(Mandatory)][string]$RootPath)

    $paths = & git -C $RootPath ls-files --cached --others --exclude-standard 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-files failed: $($paths -join [Environment]::NewLine)"
    }

    foreach ($relativePath in @($paths)) {
        if ([string]::IsNullOrWhiteSpace($relativePath)) {
            continue
        }
        $fullPath = Join-Path $RootPath $relativePath
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            [pscustomobject]@{
                FullPath = [IO.Path]::GetFullPath($fullPath)
                RelativePath = ConvertTo-NormalizedPath $relativePath
            }
        }
    }
}

function Get-PublicationFiles {
    param(
        [Parameter(Mandatory)][string]$RootPath,
        [Parameter(Mandatory)]$Manifest,
        [Parameter(Mandatory)][string]$Scope
    )

    $rootFull = [IO.Path]::GetFullPath($RootPath)
    $directories = [System.Collections.Generic.Stack[string]]::new()
    $directories.Push($rootFull)

    while ($directories.Count -gt 0) {
        $directory = $directories.Pop()

        foreach ($childDirectory in [IO.Directory]::EnumerateDirectories($directory)) {
            $relativeDirectory = Get-RelativeNormalizedPath -BasePath $rootFull -Path $childDirectory
            $blocked = $false
            foreach ($rule in @($Manifest.forbiddenPaths)) {
                if (Test-AnyGlob -Value $relativeDirectory -Patterns @($rule.patterns)) {
                    Add-Result -Kind violation -Code "ForbiddenPath" -Scope $Scope -Path $relativeDirectory -Message "Forbidden directory is present." -Classification $rule.classification -OwnerIssues @($rule.ownerIssues)
                    $blocked = $true
                    break
                }
            }
            if (-not $blocked) {
                $directories.Push($childDirectory)
            }
        }

        foreach ($file in [IO.Directory]::EnumerateFiles($directory)) {
            [pscustomobject]@{
                FullPath = [IO.Path]::GetFullPath($file)
                RelativePath = Get-RelativeNormalizedPath -BasePath $rootFull -Path $file
            }
        }
    }
}

function Test-RulePattern {
    param(
        [Parameter(Mandatory)]$Rule,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Value
    )

    foreach ($pattern in @($Rule.patterns)) {
        if ($Rule.matchType -eq "regex") {
            if ([Regex]::IsMatch($Value, [string]$pattern)) {
                return [string]$pattern
            }
        }
        else {
            $comparison = if ($Rule.caseSensitive -eq $true) { [StringComparison]::Ordinal } else { [StringComparison]::OrdinalIgnoreCase }
            if ($Value.IndexOf([string]$pattern, $comparison) -ge 0) {
                return [string]$pattern
            }
        }
    }

    return $null
}

function Get-FileRepresentations {
    param([Parameter(Mandatory)][string]$Path)

    $bytes = [IO.File]::ReadAllBytes($Path)
    return @(
        [Text.Encoding]::UTF8.GetString($bytes),
        [Text.Encoding]::Unicode.GetString($bytes)
    )
}

function Get-MatchingFileRules {
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [Parameter(Mandatory)]$Manifest
    )

    return @($Manifest.fileRules | Where-Object { Test-AnyGlob -Value $RelativePath -Patterns @($_.patterns) })
}

function Test-KnownArtifactBinary {
    param(
        [Parameter(Mandatory)][string]$Extension,
        [Parameter(Mandatory)]$Manifest
    )

    foreach ($rule in @($Manifest.artifactBinaryRules)) {
        if ($Extension -in @($rule.extensions)) {
            return $true
        }
    }
    return $false
}

function Test-FileCollection {
    param(
        [Parameter(Mandatory)][object[]]$Files,
        [Parameter(Mandatory)][string]$Scope,
        [Parameter(Mandatory)]$Manifest,
        [Parameter(Mandatory)][string]$SelectedMode
    )

    $publicationBlocks = @($Manifest.policy.publicationBlocks)
    foreach ($file in $Files) {
        $relativePath = [string]$file.RelativePath

        foreach ($forbiddenRule in @($Manifest.forbiddenPaths)) {
            if (Test-AnyGlob -Value $relativePath -Patterns @($forbiddenRule.patterns)) {
                $kind = if ($SelectedMode -eq "Publication") { "violation" } else { "finding" }
                Add-Result -Kind $kind -Code "ForbiddenPath" -Scope $Scope -Path $relativePath -Message "Forbidden file path is present." -Classification $forbiddenRule.classification -OwnerIssues @($forbiddenRule.ownerIssues)
            }
        }

        $fileRules = Get-MatchingFileRules -RelativePath $relativePath -Manifest $Manifest
        $extension = [IO.Path]::GetExtension($relativePath).ToLowerInvariant()
        $knownArtifactBinary = $Scope -eq "artifact" -and (Test-KnownArtifactBinary -Extension $extension -Manifest $Manifest)

        if ($fileRules.Count -eq 0 -and -not $knownArtifactBinary) {
            Add-Result -Kind violation -Code "UnclassifiedFile" -Scope $Scope -Path $relativePath -Message "No file rule classifies this file." -Classification "unclassified"
        }

        foreach ($rule in $fileRules) {
            if ($rule.classification -in $publicationBlocks) {
                $kind = if ($SelectedMode -eq "Publication") { "violation" } else { "finding" }
                Add-Result -Kind $kind -Code "FileDisposition" -Scope $Scope -Path $relativePath -Message "File rule '$($rule.id)' requires $($rule.classification)." -Classification $rule.classification -OwnerIssues @($rule.ownerIssues)
            }
        }

        $isKnownText = $extension -in @($Manifest.textExtensions) -or [IO.Path]::GetFileName($relativePath) -in @($Manifest.textFileNames)
        if (-not $isKnownText -and -not $knownArtifactBinary) {
            Add-Result -Kind violation -Code "UnknownBinary" -Scope $Scope -Path $relativePath -Message "The file extension is not explicitly classified as text or an allowed artifact binary." -Classification "unclassified"
        }

        $representations = Get-FileRepresentations -Path $file.FullPath
        foreach ($rule in @($Manifest.contentRules)) {
            $matchedPattern = $null
            if (@($rule.targets) -contains "path") {
                $matchedPattern = Test-RulePattern -Rule $rule -Value $relativePath
            }
            if ($null -eq $matchedPattern -and @($rule.targets) -contains "content") {
                foreach ($representation in $representations) {
                    $matchedPattern = Test-RulePattern -Rule $rule -Value $representation
                    if ($null -ne $matchedPattern) {
                        break
                    }
                }
            }

            if ($null -ne $matchedPattern) {
                $kind = if ($SelectedMode -eq "Publication" -and $rule.classification -in $publicationBlocks) { "violation" } else { "finding" }
                Add-Result -Kind $kind -Code "ContentRule" -Scope $Scope -Path $relativePath -Message "Content rule '$($rule.id)' matched '$matchedPattern'." -Classification $rule.classification -OwnerIssues @($rule.ownerIssues)
            }
        }

        if ($extension -eq ".json") {
            $utf8 = $representations[0]
            $isJsonSchema = $utf8 -match '"\$schema"\s*:'
            if (-not $isJsonSchema -and $utf8 -match '"Version"\s*:' -and $utf8 -match '"Players"\s*:' -and $utf8 -match '"CurrentPlayerId"\s*:') {
                $kind = if ($SelectedMode -eq "Publication") { "violation" } else { "finding" }
                Add-Result -Kind $kind -Code "SaveSignature" -Scope $Scope -Path $relativePath -Message "JSON content has the legacy game-save signature." -Classification "remove" -OwnerIssues @(52, 58)
            }
        }
    }
}

function Test-Dependencies {
    param(
        [Parameter(Mandatory)]$Manifest,
        [Parameter(Mandatory)][string]$SelectedMode
    )

    foreach ($dependency in @($Manifest.dependencies)) {
        $approved = $dependency.licenseStatus -eq "approved" -and $dependency.noticeStatus -in @("approved", "not-required") -and -not [string]::IsNullOrWhiteSpace($dependency.licenseEvidence)
        if (-not $approved) {
            $kind = if ($SelectedMode -eq "Publication") { "violation" } else { "finding" }
            Add-Result -Kind $kind -Code "DependencyReview" -Scope "dependency" -Path "$($dependency.ecosystem)/$($dependency.name)@$($dependency.version)" -Message "License evidence or notice review is incomplete." -Classification "review" -OwnerIssues @($dependency.ownerIssues)
        }
    }
}

function Test-DeclaredDependencies {
    param(
        [Parameter(Mandatory)][object[]]$Files,
        [Parameter(Mandatory)]$Manifest
    )

    $nugetDependencies = @($Manifest.dependencies | Where-Object ecosystem -eq "NuGet")
    foreach ($file in @($Files | Where-Object { [IO.Path]::GetExtension($_.RelativePath) -eq ".csproj" })) {
        [xml]$project = Get-Content -Raw -LiteralPath $file.FullPath
        foreach ($reference in @($project.SelectNodes("//*[local-name()='PackageReference']"))) {
            $name = if ($reference.Include) { [string]$reference.Include } else { [string]$reference.Update }
            $version = if ($reference.Version) { [string]$reference.Version } elseif ($reference.SelectSingleNode("*[local-name()='Version']")) { [string]$reference.SelectSingleNode("*[local-name()='Version']").InnerText } else { $null }
            $manifestEntry = @($nugetDependencies | Where-Object { $_.name -eq $name -and $_.version -eq $version })
            if ($manifestEntry.Count -eq 0) {
                Add-Result -Kind violation -Code "DependencyInventory" -Scope "dependency" -Path "NuGet/$name@$version" -Message "Direct package reference is missing from the dependency inventory or has a different version." -Classification "unclassified" -OwnerIssues @(57)
            }
        }
    }

    $actionDependencies = @($Manifest.dependencies | Where-Object ecosystem -eq "github-action")
    foreach ($file in @($Files | Where-Object { [IO.Path]::GetExtension($_.RelativePath) -in @(".yml", ".yaml") })) {
        $workflow = Get-Content -Raw -LiteralPath $file.FullPath
        foreach ($match in [Regex]::Matches($workflow, '(?m)^\s*-?\s*uses:\s*([^@\s]+)@([^\s#]+)')) {
            $name = $match.Groups[1].Value
            $version = $match.Groups[2].Value
            if ($name.StartsWith("./", [StringComparison]::Ordinal)) {
                continue
            }
            $manifestEntry = @($actionDependencies | Where-Object {
                $_.name -eq $name -and $version -in @(([string]$_.version).Split(",") | ForEach-Object { $_.Trim() })
            })
            if ($manifestEntry.Count -eq 0) {
                Add-Result -Kind violation -Code "DependencyInventory" -Scope "dependency" -Path "github-action/$name@$version" -Message "Workflow action is missing from the dependency inventory or has a different reference." -Classification "unclassified" -OwnerIssues @(57)
            }
        }
    }
}

function Write-AuditReport {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$SelectedMode,
        [Parameter(Mandatory)][string]$RootPath,
        [string]$ArtifactPath,
        [Parameter(Mandatory)][int]$ExitCode,
        [string]$ToolError
    )

    $report = [ordered]@{
        schemaVersion = 1
        generatedAtUtc = [DateTime]::UtcNow.ToString("O")
        mode = $SelectedMode
        root = $RootPath
        artifactRoot = $ArtifactPath
        exitCode = $ExitCode
        toolError = $ToolError
        summary = [ordered]@{
            findings = $script:Findings.Count
            violations = $script:Violations.Count
        }
        findings = @($script:Findings)
        violations = @($script:Violations)
    }

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        [IO.Directory]::CreateDirectory($parent) | Out-Null
    }
    [IO.File]::WriteAllText($Path, ($report | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
}

$exitCode = 2
$toolError = $null
$rootFull = $null
$artifactFull = $null
$reportFull = $null
$reportAllowed = $false

try {
    $rootFull = [IO.Path]::GetFullPath($Root)
    $manifestFull = [IO.Path]::GetFullPath($ManifestPath)
    $reportFull = [IO.Path]::GetFullPath($ReportPath)

    if (-not (Test-Path -LiteralPath $rootFull -PathType Container)) {
        throw "Root '$rootFull' does not exist."
    }
    if (-not (Test-Path -LiteralPath $manifestFull -PathType Leaf)) {
        throw "Manifest '$manifestFull' does not exist."
    }
    if (Test-PathInside -Candidate $reportFull -Container $rootFull) {
        throw "ReportPath must be outside Root."
    }

    $manifest = Get-Content -Raw -LiteralPath $manifestFull | ConvertFrom-Json
    Assert-Manifest -Manifest $manifest

    if ($Mode -eq "Publication") {
        if ([string]::IsNullOrWhiteSpace($ArtifactRoot)) {
            throw "ArtifactRoot is required in Publication mode."
        }
        $artifactFull = [IO.Path]::GetFullPath($ArtifactRoot)
        if (-not (Test-Path -LiteralPath $artifactFull -PathType Container)) {
            throw "ArtifactRoot '$artifactFull' does not exist."
        }
        if ((Test-PathInside -Candidate $artifactFull -Container $rootFull) -or (Test-PathInside -Candidate $rootFull -Container $artifactFull)) {
            throw "Root and ArtifactRoot must be separate directory trees."
        }
        if (Test-PathInside -Candidate $reportFull -Container $artifactFull) {
            throw "ReportPath must be outside ArtifactRoot."
        }
    }

    $reportAllowed = $true

    $sourceFiles = if ($Mode -eq "Audit") {
        @(Get-AuditFiles -RootPath $rootFull)
    }
    else {
        @(Get-PublicationFiles -RootPath $rootFull -Manifest $manifest -Scope "source")
    }

    Test-FileCollection -Files $sourceFiles -Scope "source" -Manifest $manifest -SelectedMode $Mode
    Test-DeclaredDependencies -Files $sourceFiles -Manifest $manifest

    if ($Mode -eq "Publication") {
        $artifactFiles = @(Get-PublicationFiles -RootPath $artifactFull -Manifest $manifest -Scope "artifact")
        Test-FileCollection -Files $artifactFiles -Scope "artifact" -Manifest $manifest -SelectedMode $Mode

        foreach ($requiredFile in @($manifest.requiredPublicationFiles)) {
            $requiredPath = Join-Path $rootFull ([string]$requiredFile.path)
            if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
                Add-Result -Kind violation -Code "RequiredPublicationFile" -Scope "source" -Path ([string]$requiredFile.path) -Message "Required publication file is missing." -Classification "review" -OwnerIssues @($requiredFile.ownerIssues)
            }
        }
    }

    Test-Dependencies -Manifest $manifest -SelectedMode $Mode
    $exitCode = if ($script:Violations.Count -eq 0) { 0 } else { 1 }
}
catch {
    $toolError = $_.Exception.Message
    $exitCode = 2
}

if ($null -ne $reportFull -and $reportAllowed) {
    try {
        Write-AuditReport -Path $reportFull -SelectedMode $Mode -RootPath $rootFull -ArtifactPath $artifactFull -ExitCode $exitCode -ToolError $toolError
    }
    catch {
        Write-Error "Could not write audit report: $($_.Exception.Message)"
        exit 2
    }
}

if ($exitCode -eq 2) {
    [Console]::Error.WriteLine($toolError)
}
else {
    Write-Host "Clean-publication $Mode completed: $($script:Findings.Count) finding(s), $($script:Violations.Count) violation(s)."
    Write-Host "Report: $reportFull"
}

exit $exitCode
