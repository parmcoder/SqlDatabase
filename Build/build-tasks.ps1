Include ".\build-scripts.ps1"

Task default -Depends Initialize, Clean, Build, Pack, Test
Task Pack -Depends PackGlobalTool, PackNet452, PackManualDownload
Task Test -Depends InitializeTests `
    , TestPublishModule `
    , TestPowerShellDesktop `
    , TestPowerShellCore610 `
    , TestPowerShellCore611 `
    , TestPowerShellCore612 `
    , TestPowerShellCore613 `
    , TestPowerShellCore620 `
    , TestPowerShellCore621 `
    , TestPowerShellCore624 `
    , TestPowerShellCore70 `
    , TestGlobalTool22 `
    , TestNetCore22

Task Initialize {
    $script:nugetexe = Join-Path $PSScriptRoot "nuget.exe"
    $script:sourceDir = Join-Path $PSScriptRoot "..\Sources"
    $script:binDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\bin"))
    $script:packageVersion = Get-AssemblyVersion (Join-Path $sourceDir "GlobalAssemblyInfo.cs")
    $script:repositoryCommitId = Get-RepositoryCommitId

    $script:moduleBin = Join-Path $binDir "SqlDatabase.PowerShell\netstandard2.0\"
    $script:moduleIntegrationTests = Join-Path $binDir "IntegrationTests"

    $mssql = Resolve-SqlServerIp $sqlContainer
    $script:connectionString = "Data Source=$mssql;Initial Catalog=SqlDatabaseTest;User Id=sa;Password=P@ssw0rd;"

    Write-Host "PackageVersion: $packageVersion"
    Write-Host "CommitId: $repositoryCommitId"
}

Task Clean {
    if (Test-Path $binDir) {
        Remove-Item -Path $binDir -Recurse -Force
    }
}

Task Build {
    $solutionFile = Join-Path $sourceDir "SqlDatabase.sln"
    Exec { dotnet restore $solutionFile }
    Exec { dotnet build $solutionFile -t:Rebuild -p:Configuration=Release }

    # .psd1 set module version
    $psdFiles = Get-ChildItem -Path $binDir -Filter "SqlDatabase.psd1" -Recurse
    foreach ($psdFile in $psdFiles) {
        ((Get-Content -Path $psdFile.FullName -Raw) -replace '{{ModuleVersion}}', $packageVersion) | Set-Content -Path $psdFile.FullName
    }

    # copy to pwershell net452
    $net45Dest = Join-Path $moduleBin "net452"
    $net45Source = Join-Path $binDir "SqlDatabase\net452"
    New-Item -Path $net45Dest -ItemType Directory
    Copy-Item -Path (Join-Path $net45Source "SqlDatabase.exe") -Destination $net45Dest
    Copy-Item -Path (Join-Path $net45Source "SqlDatabase.pdb") -Destination $net45Dest
}

Task PackGlobalTool {
    $projectFile = Join-Path $sourceDir "SqlDatabase\SqlDatabase.csproj"
    Exec {
        dotnet pack `
            -c Release `
            -p:PackAsTool=true `
            -p:TargetFrameworks=netcoreapp2.2 `
            -p:PackageVersion=$packageVersion `
            -p:RepositoryCommit=$repositoryCommitId `
            -o $binDir `
            $projectFile
    }
}

Task PackNet452 {
    $nuspec = Join-Path $sourceDir "SqlDatabase.Package\package.nuspec"
    Exec { 
        & $nugetexe pack `
            -NoPackageAnalysis `
            -verbosity detailed `
            -OutputDirectory $binDir `
            -Version $packageVersion `
            -p RepositoryCommit=$repositoryCommitId `
            -p bin=$moduleBin `
            $nuspec
    }
}

Task PackManualDownload {
    $out = Join-Path $binDir "ManualDownload"
    New-Item -Path $out -ItemType Directory | Out-Null

    $lic = Join-Path $sourceDir "..\LICENSE.md"
    
    $destination = Join-Path $out "SqlDatabase.$packageVersion-net452.zip"
    $source = Join-Path $binDir "SqlDatabase\net452\*"
    Compress-Archive -Path $source, $lic -DestinationPath $destination

    $destination = Join-Path $out "SqlDatabase.$packageVersion-PowerShell.zip"
    $source = Join-Path $moduleBin "*"
    Compress-Archive -Path $source, $lic -DestinationPath $destination

    $destination = Join-Path $out "SqlDatabase.$packageVersion-netcore22.zip"
    $source = Join-Path $binDir "SqlDatabase\netcoreapp2.2\publish\*"
    Compress-Archive -Path $source, $lic -DestinationPath $destination
}

Task InitializeTests {
    Copy-Item -Path (Join-Path $sourceDir "SqlDatabase.Test\IntegrationTests") -Destination $binDir -Force -Recurse
    Copy-Item -Path (Join-Path $binDir "Tests\net452\2.1_2.2.*") -Destination (Join-Path $binDir "IntegrationTests\Upgrade") -Force -Recurse
}

Task TestPowerShellCore611 {
    Test-PowerShellCore "mcr.microsoft.com/powershell:6.1.1-alpine-3.8"
}

Task TestPowerShellCore610 {
    Test-PowerShellCore "mcr.microsoft.com/powershell:6.1.0-ubuntu-18.04"
}

Task TestPowerShellCore612 {
    Test-PowerShellCore "mcr.microsoft.com/powershell:6.1.2-alpine-3.8"
}

Task TestPowerShellCore613 {
    Test-PowerShellCore "mcr.microsoft.com/powershell:6.1.3-alpine-3.8"
}

Task TestPowerShellCore620 {
    Test-PowerShellCore "mcr.microsoft.com/powershell:6.2.0-alpine-3.8"
}

Task TestPowerShellCore621 {
    Test-PowerShellCore "mcr.microsoft.com/powershell:6.2.1-alpine-3.8"
}

Task TestPowerShellCore624 {
    Test-PowerShellCore "mcr.microsoft.com/powershell:6.2.4-alpine-3.8"
}

Task TestPowerShellCore70 {
    Test-PowerShellCore "mcr.microsoft.com/powershell:7.0.0-ubuntu-18.04"
}

Task TestPublishModule {
    $log = Join-Path $binDir "Publish-Module.whatif.log"

    Test-PowerShellDesktop "Publish-Module -Name SqlDatabase -WhatIf -Verbose -NuGetApiKey 123 *> $log"
}

Task TestPowerShellDesktop {
    $env:test = $moduleIntegrationTests

    $builder = New-Object -TypeName System.Data.SqlClient.SqlConnectionStringBuilder -ArgumentList $connectionString
    $builder["Data Source"] = "."
    $env:connectionString = $builder.ToString()

    $testScript = Join-Path $moduleIntegrationTests "Test.ps1"

    Test-PowerShellDesktop ". $testScript"
}

Task TestGlobalTool22 {
    $packageName = "SqlDatabase.GlobalTool.$packageVersion.nupkg"
    $app = (Join-Path ([System.IO.Path]::GetFullPath($binDir)) $packageName) + ":/app/$packageName"
    $test = [System.IO.Path]::GetFullPath($moduleIntegrationTests) + ":/test"

    Exec {
        docker run --rm `
            -v $app `
            -v $test `
            --env connectionString=$connectionString `
            --env test=/test `
            --env app=/app `
            --env packageVersion=$packageVersion `
            "microsoft/dotnet:2.2-sdk" `
            bash /test/TestGlobalTool.sh
    }
}

Task TestNetCore22 {
    $bin = Join-Path $binDir "SqlDatabase\netcoreapp2.2\publish"
    $app = $bin + ":/app"
    $test = $moduleIntegrationTests + ":/test"

    Exec {
        docker run --rm `
            -v $app `
            -v $test `
            --env connectionString=$connectionString `
            --env test=/test `
            -w "/app" `
            "microsoft/dotnet:2.2-runtime" `
            bash /test/Test.sh
    }
}