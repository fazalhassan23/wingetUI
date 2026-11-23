[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$requiredFiles = @(
    'README.md',
    'CHANGELOG.md',
    'CONTRIBUTING.md',
    'SECURITY.md',
    'docs/architecture.md',
    'docs/configuration.md',
    'docs/development.md',
    'docs/diagrams.md',
    'docs/git-workflow.md',
    'docs/schema.md',
    'docs/testing.md',
    'docs/troubleshooting.md',
    'docs/user-guide.md',
    'docs/versioning-and-releases.md'
)

$errors = [System.Collections.Generic.List[string]]::new()
foreach ($relativePath in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot $relativePath) -PathType Leaf)) {
        $errors.Add("Missing required document: $relativePath")
    }
}

$documents = @(
    Get-Item -LiteralPath (Join-Path $RepositoryRoot 'README.md'),
        (Join-Path $RepositoryRoot 'CHANGELOG.md'),
        (Join-Path $RepositoryRoot 'CONTRIBUTING.md'),
        (Join-Path $RepositoryRoot 'SECURITY.md') -ErrorAction SilentlyContinue
    Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'docs') -Filter '*.md' -File -Recurse
    Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot '.changes') -Filter '*.md' -File -ErrorAction SilentlyContinue
) | Where-Object { $_ -is [System.IO.FileInfo] }

foreach ($document in $documents) {
    $content = Get-Content -LiteralPath $document.FullName -Raw
    $fenceCount = ([regex]::Matches($content, '(?m)^```')).Count
    if ($fenceCount % 2 -ne 0) {
        $errors.Add("Unbalanced fenced code block: $($document.FullName)")
    }

    foreach ($match in [regex]::Matches($content, '\[[^\]]+\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value.Trim()
        if ($target -match '^(https?://|mailto:|#)') {
            continue
        }

        $pathOnly = [System.Uri]::UnescapeDataString(($target -split '#', 2)[0])
        if ([string]::IsNullOrWhiteSpace($pathOnly)) {
            continue
        }

        $resolvedPath = Join-Path $document.DirectoryName $pathOnly
        if (-not (Test-Path -LiteralPath $resolvedPath)) {
            $relativeDocument = [System.IO.Path]::GetRelativePath($RepositoryRoot, $document.FullName)
            $errors.Add("Broken relative link in ${relativeDocument}: $target")
        }
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    throw "Documentation validation failed with $($errors.Count) error(s)."
}

Write-Host "Documentation validation passed for $($documents.Count) Markdown files."
