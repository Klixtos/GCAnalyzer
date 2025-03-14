param($installPath, $toolsPath, $package, $project)

$analyzersPaths = Join-Path (Join-Path $toolsPath "..") "analyzers"
$analyzersPath = Join-Path $analyzersPaths "dotnet"
$analyzersPath = Join-Path $analyzersPath "cs"

# Uninstall the language agnostic analyzers
$languageAgnosticAnalyzers = Join-Path $analyzersPath "*.dll"
foreach($analyzer in Get-ChildItem $languageAgnosticAnalyzers)
{
    if($project.Object.AnalyzerReferences)
    {
        try
        {
            $project.Object.AnalyzerReferences.Remove($analyzer.FullName)
        }
        catch
        {
        }
    }
} 