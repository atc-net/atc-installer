function HelloWorldBlazor {
    $rootPath = $PSScriptRoot;
    $projectPath = "$rootPath\Code\HelloWorldBlazor"
    $outputDirectory = "$projectPath\bin\Debug\net7.0\publish"
    $zipFileLocation = "$rootPath\InstallationFiles\HelloWorldBlazor.zip"

    Set-Location $projectPath
    Write-Host $projectPath

    dotnet publish --configuration Debug

    if (Test-Path $outputDirectory) {
        if (Test-Path $zipFileLocation) {
            Remove-Item $zipFileLocation
        }

        Get-ChildItem -Path $outputDirectory -Recurse | Where-Object { $_.Name -ne "appsettings.Development.json" } | Compress-Archive -DestinationPath $zipFileLocation

        Write-Host "Build and compression completed successfully!"
    }
    else
    {
        Write-Host "Build failed or output directory doesn't exist."
    }

    Set-Location $rootPath
}

function HelloWorldMaui {
    $rootPath = $PSScriptRoot;
    $projectPath = "$rootPath\Code\HelloWorldMaui"
    $outputDirectory = "$projectPath\bin\Debug\net7.0\publish"
    $zipFileLocation = "$rootPath\InstallationFiles\HelloWorldMaui.zip"

    Set-Location $projectPath
    
    dotnet publish --configuration Debug

    if (Test-Path $outputDirectory) {
        if (Test-Path $zipFileLocation) {
            Remove-Item $zipFileLocation
        }

        Get-ChildItem -Path $outputDirectory -Recurse | Where-Object { $_.Name -ne "appsettings.Development.json" } | Compress-Archive -DestinationPath $zipFileLocation

        Write-Host "Build and compression completed successfully!"
    }
    else
    {
        Write-Host "Build failed or output directory doesn't exist."
    }

    Set-Location $rootPath
}

function HelloWorldMinimalApi {
    $rootPath = $PSScriptRoot;
    $projectPath = "$rootPath\Code\HelloWorldMinimalApi"
    $outputDirectory = "$projectPath\bin\Debug\net7.0\publish"
    $zipFileLocation = "$rootPath\InstallationFiles\HelloWorldMinimalApi.zip"

    Set-Location $projectPath

    dotnet publish --configuration Debug

    if (Test-Path $outputDirectory) {
        if (Test-Path $zipFileLocation) {
            Remove-Item $zipFileLocation
        }

        Get-ChildItem -Path $outputDirectory -Recurse | Where-Object { $_.Name -ne "appsettings.Development.json" } | Compress-Archive -DestinationPath $zipFileLocation

        Write-Host "Build and compression completed successfully!"
    }
    else
    {
        Write-Host "Build failed or output directory doesn't exist."
    }

    Set-Location $rootPath
}

function HelloWorldWindowsNTServiceTopShelf {
    $rootPath = $PSScriptRoot;
    $projectPath = "$rootPath\Code\HelloWorldWindowsNTServiceTopShelf"
    $outputDirectory = "$projectPath\bin\Debug\net7.0\publish"
    $zipFileLocation = "$rootPath\InstallationFiles\HelloWorldWindowsNTServiceTopShelf.zip"

    Set-Location $projectPath

    dotnet publish --configuration Debug

    if (Test-Path $outputDirectory) {
        if (Test-Path $zipFileLocation) {
            Remove-Item $zipFileLocation
        }
        
        Get-ChildItem -Path $outputDirectory -Recurse | Where-Object { $_.Name -ne "appsettings.Development.json" } | Compress-Archive -DestinationPath $zipFileLocation

        Write-Host "Build and compression completed successfully!"
    }
    else
    {
        Write-Host "Build failed or output directory doesn't exist."
    }

    Set-Location $rootPath
}

function HelloWorldWpf {
    $rootPath = $PSScriptRoot;
    $projectPath = "$rootPath\Code\HelloWorldWpf"
    $outputDirectory = "$projectPath\bin\Debug\net7.0-windows\publish"
    $zipFileLocation = "$rootPath\InstallationFiles\HelloWorldWpf.zip"

    Set-Location $projectPath

    dotnet publish --configuration Debug

    if (Test-Path $outputDirectory) {
        if (Test-Path $zipFileLocation) {
            Remove-Item $zipFileLocation
        }

        Get-ChildItem -Path $outputDirectory -Recurse | Where-Object { $_.Name -ne "appsettings.Development.json" } | Compress-Archive -DestinationPath $zipFileLocation

        Write-Host "Build and compression completed successfully!"
    }
    else
    {
        Write-Host "Build failed or output directory doesn't exist."
    }

    Set-Location $rootPath
}

# HelloWorldBlazor
HelloWorldMaui
# HelloWorldMinimalApi
# HelloWorldWindowsNTServiceTopShelf
# HelloWorldWpf