function BuildDotnetAndPackageProject {
    param(
        [string] $projectName
    )

    $rootPath = $PSScriptRoot;
    $projectPath = "$rootPath\Code\$projectName"
    $outputDirectory = "$projectPath\bin\Debug\net7.0\publish"
    $zipFileLocation = "$rootPath\InstallationFiles\$projectName.zip"

    if ($projectName -eq "HelloWorldWpf") {
        $outputDirectory = "$projectPath\bin\Debug\net7.0-windows\publish"
    }

    Set-Location $projectPath

    dotnet publish --configuration Debug

    if (Test-Path $outputDirectory) {
        if (Test-Path $zipFileLocation) {
            Remove-Item $zipFileLocation
        }

        Get-ChildItem -Path $outputDirectory -Recurse | Where-Object { $_.Name -ne "appsettings.Development.json" } | Compress-Archive -DestinationPath $zipFileLocation

        Write-Host "Build and compression of $projectName completed successfully!"
    }
    else
    {
        Write-Host "Build of $projectName failed or output directory doesn't exist."
    }

    Set-Location $rootPath
}


BuildDotnetAndPackageProject "HelloWorldBlazor"
# BuildDotnetAndPackageProject "HelloWorldMaui"
BuildDotnetAndPackageProject "HelloWorldMinimalApi"
BuildDotnetAndPackageProject "HelloWorldWindowsNTServiceTopShelf"
BuildDotnetAndPackageProject "HelloWorldWpf"
