task Default `
    BuildDotnetSdk31 `
    , BuildDotnetRuntime31 `
    , BuildDotnetSdk50 `
    , BuildDotnetRuntime50 `
    , BuildDotnetSdk60 `
    , BuildDotnetRuntime60 `
    , BuildDotnetSdk70 `
    , BuildDotnetRuntime70 `
    , BuildMsSqlDatabase `
    , BuildPgSqlDatabase `
    , BuildMySqlDatabase

task BuildMsSqlDatabase {
    $context = Join-Path $PSScriptRoot "..\Sources\SqlDatabase.Test\Docker"
    exec {
        docker build `
            -f image-mssql-2017.dockerfile `
            -t sqldatabase/mssql:2017 `
            $context
    }
}

task BuildPgSqlDatabase {
    $context = Join-Path $PSScriptRoot "..\Sources\SqlDatabase.Test\Docker"
    exec {
        docker build `
            -f image-postgres-133.dockerfile `
            -t sqldatabase/postgres:13.3 `
            $context
    }
}

task BuildMySqlDatabase {
    $context = Join-Path $PSScriptRoot "..\Sources\SqlDatabase.Test\Docker"
    exec {
        docker build `
            -f image-mysql-8025.dockerfile `
            -t sqldatabase/mysql:8.0.25 `
            $context
    }
}

task BuildDotnetSdk31 {
    exec {
        docker build `
            -f image-dotnet-sdk-3.1.dockerfile `
            -t sqldatabase/dotnet_pwsh:3.1-sdk `
            .
    }
}

task BuildDotnetRuntime31 {
    exec {
        docker build `
            -f image-dotnet-runtime-3.1.dockerfile `
            -t sqldatabase/dotnet_pwsh:3.1-runtime `
            .
    }
}

task BuildDotnetSdk50 {
    exec {
        docker build `
            -f image-dotnet-sdk-5.0.dockerfile `
            -t sqldatabase/dotnet_pwsh:5.0-sdk `
            .
    }
}

task BuildDotnetRuntime50 {
    exec {
        docker build `
            -f image-dotnet-runtime-5.0.dockerfile `
            -t sqldatabase/dotnet_pwsh:5.0-runtime `
            .
    }
}

task BuildDotnetSdk60 {
    exec {
        docker build `
            -f image-dotnet-sdk-6.0.dockerfile `
            -t sqldatabase/dotnet_pwsh:6.0-sdk `
            .
    }
}

task BuildDotnetRuntime60 {
    exec {
        docker build `
            -f image-dotnet-runtime-6.0.dockerfile `
            -t sqldatabase/dotnet_pwsh:6.0-runtime `
            .
    }
}

task BuildDotnetSdk70 {
    exec {
        docker build `
            -f image-dotnet-sdk-7.0.dockerfile `
            -t sqldatabase/dotnet_pwsh:7.0-sdk `
            .
    }
}

task BuildDotnetRuntime70 {
    exec {
        docker build `
            -f image-dotnet-runtime-7.0.dockerfile `
            -t sqldatabase/dotnet_pwsh:7.0-runtime `
            .
    }
}