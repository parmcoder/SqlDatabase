function Get-AssemblyVersion($assemblyInfoCsPath) {
    $Anchor = "AssemblyVersion(""";
    $lines = Get-Content -Path $assemblyInfoCsPath

    foreach ($line in $lines) {
        $index = $line.IndexOf($Anchor);
        if ($index -lt 0) {
            continue;
        }
        
        $text = $line.Substring($index + $Anchor.Length);

        $index = $text.IndexOf('"');
        $text = $text.Substring(0, $index);
    
        $version = New-Object -TypeName System.Version -ArgumentList $text
        $build = $version.Build
        if ($build -le 0) {
            $build = 0
        }
    
        $text = (New-Object -TypeName System.Version -ArgumentList $version.Major, $version.Minor, $build).ToString();
        return $text;
    }
}

function Get-RepositoryCommitId {
    $response = (Invoke-RestMethod -Uri "https://api.github.com/repos/max-ieremenko/SqlDatabase/commits/master")
    return $response.sha
}

function Start-Mssql {
    # https://github.com/docker/for-win/issues/3171
    $containerId = exec { 
        docker run `
            -d `
            -p 1433 `
            sqldatabase/mssql:2017
    }
    
    $ip = exec { 
        docker inspect `
            --format "{{.NetworkSettings.Networks.bridge.IPAddress}}"  `
            $containerId
    }

    $port = exec { 
        docker inspect `
            --format "{{(index (index .NetworkSettings.Ports \""1433/tcp\"") 0).HostPort}}"  `
            $containerId
    }

    $builder = New-Object -TypeName System.Data.SqlClient.SqlConnectionStringBuilder
    $builder["Initial Catalog"] = "SqlDatabaseTest"
    $builder["User Id"] = "sa"
    $builder["Password"] = "P@ssw0rd"
    $builder["Connect Timeout"] = 5

    $builder["Data Source"] = ".,$port"
    $connectionString = $builder.ToString()
    
    $builder["Data Source"] = $ip
    $remoteConnectionString = $builder.ToString()

    return @{
        containerId            = $containerId
        connectionString       = $connectionString
        remoteConnectionString = $remoteConnectionString
    }
}

function Wait-Mssql($connectionString) {
    $connection = New-Object -TypeName System.Data.SqlClient.SqlConnection -ArgumentList $connectionString
    try {
        for ($i = 0; $i -lt 20; $i++) {
            try {
                $connection.Open()
                return
            }
            catch {
                Start-Sleep -Seconds 1
            }
        }

        $connection.Open()
    }
    finally {
        $connection.Dispose()
    }
}

function Start-Pgsql {
    $npgsqldll = Join-Path $env:USERPROFILE ".nuget\packages\npgsql\4.0.11\lib\netstandard2.0\Npgsql.dll"
    Add-Type -Path $npgsqldll

    $containerId = exec { 
        docker run `
            -d `
            -p 5432 `
            sqldatabase/postgres:13.3
    }
    
    $ip = exec { 
        docker inspect `
            --format "{{.NetworkSettings.Networks.bridge.IPAddress}}"  `
            $containerId
    }

    $port = exec { 
        docker inspect `
            --format "{{(index (index .NetworkSettings.Ports \""5432/tcp\"") 0).HostPort}}"  `
            $containerId
    }

    $builder = New-Object -TypeName Npgsql.NpgsqlConnectionStringBuilder
    $builder["Database"] = "sqldatabasetest"
    $builder["Username"] = "postgres"
    $builder["Password"] = "qwerty"
    $builder["Timeout"] = 5

    $builder.Host = "localhost"
    $builder.Port = $port.ToString()
    $connectionString = $builder.ToString()
    
    $builder.Host = $ip.ToString()
    $builder.Port = 5432
    $remoteConnectionString = $builder.ToString()

    return @{
        containerId            = $containerId
        connectionString       = $connectionString
        remoteConnectionString = $remoteConnectionString
    }
}

function Wait-Pgsql($connectionString) {
    $connection = New-Object -TypeName Npgsql.NpgsqlConnection -ArgumentList $connectionString
    try {
        for ($i = 0; $i -lt 20; $i++) {
            try {
                $connection.Open()
                return
            }
            catch {
                Start-Sleep -Seconds 1
            }
        }

        $connection.Open()
    }
    finally {
        $connection.Dispose()
    }
}
