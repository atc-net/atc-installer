function BuildDotnetAndPackageProject {
    param(
        [string] $projectName
    )

    $rootPath = $PSScriptRoot;
    $projectPath = "$rootPath\Code\$projectName"
    $outputDirectory = "$projectPath\bin\Debug\net7.0\publish"
	$installationDirectory = "$rootPath\InstallationFiles"
    $zipFileLocation = "$installationDirectory\$projectName.zip"

	if (!(Test-Path -Path $installationDirectory))
    {
        New-Item -Force -Type Directory $installationDirectory
	}

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

function BuildNodeJSAndPackageProject {
    # TODO: Imp. this.
}

BuildDotnetAndPackageProject "HelloWorldBlazor"
# BuildDotnetAndPackageProject "HelloWorldMaui"
BuildDotnetAndPackageProject "HelloWorldMinimalApi"
BuildNodeJSAndPackageProject "HelloWorldReact"
BuildDotnetAndPackageProject "HelloWorldWindowsNTServiceTopShelf"
BuildDotnetAndPackageProject "HelloWorldWpf"
