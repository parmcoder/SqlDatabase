SqlDatabase
===========

[![NuGet](https://img.shields.io/nuget/v/SqlDatabase.svg?style=flat-square&label=nuget%20net%204.5.2)](https://www.nuget.org/packages/SqlDatabase/)
[![NuGet](https://img.shields.io/nuget/v/SqlDatabase.GlobalTool.svg?style=flat-square&label=nuget%20dotnet%20tool)](https://www.nuget.org/packages/SqlDatabase.GlobalTool/)
[![PowerShell Gallery](https://img.shields.io/powershellgallery/v/SqlDatabase.svg?style=flat-square)](https://www.powershellgallery.com/packages/SqlDatabase)
[![GitHub release](https://img.shields.io/github/release/max-ieremenko/SqlDatabase.svg?style=flat-square&label=manual%20download)](https://github.com/max-ieremenko/SqlDatabase/releases)

Command line and PowerShell tool for SQL Server to execute scripts, export data and database migrations.

Table of Contents
-----------------

<!-- toc -->

- [Installation](#installation)
- [Execute script(s) (file)](#execute-script)
- [Export data from a database to sql script (file)](#export-data)
- [Create a database](#create-database)
- [Upgrade an existing database](#upgrade-database)
- [Scripts](#scripts)
- [Variables](#variables)
- [*.zip files](#zip-files)
- [Examples](#examples)
- [License](#license)

<!-- tocstop -->

Installation
------------

### PowerShell, from gallery

[![PowerShell Gallery](https://img.shields.io/powershellgallery/v/SqlDatabase.svg?style=flat-square)](https://www.powershellgallery.com/packages/SqlDatabase)

```powershell
PS> Install-Module -Name SqlDatabase
```

### PowerShell, manual [release](https://github.com/max-ieremenko/SqlDatabase/releases) download

[![GitHub release](https://img.shields.io/github/release/max-ieremenko/SqlDatabase.svg?style=flat-square&label=manual%20download)](https://github.com/max-ieremenko/SqlDatabase/releases)

```powershell
PS> Import-Module .\SqlDatabase.psm1
```

### Dotnet tool

[![NuGet](https://img.shields.io/nuget/v/SqlDatabase.GlobalTool.svg?style=flat-square&label=nuget%20dotnet%20tool)](https://www.nuget.org/packages/SqlDatabase.GlobalTool/)

```bash
$ dotnet tool install --global SqlDatabase.GlobalTool
```

[Back to ToC](#table-of-contents)

Execute script(s) (file) <a name="execute-script"></a>
--------------

execute script from file "c:\Scripts\script.sql" on *[MyDatabase]* on server *[MyServer]* with "Variable1=value1" and "Variable2=value2"

```bash
$ SqlDatabase execute ^
      "-database=Data Source=server;Initial Catalog=database;Integrated Security=True" ^
      -from=c:\Scripts\script.sql ^
      -varVariable1=value1 ^
      -varVariable2=value2

PS> Execute-SqlDatabase `
      -database "Data Source=server;Initial Catalog=database;Integrated Security=True" `
      -from c:\Scripts\script.sql `
      -var Variable1=value1,Variable2=value2 `
      -InformationAction Continue
```

 See more details [here](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/ExecuteScriptsFolder).

[Back to ToC](#table-of-contents)

Export data from a database to sql script (file) <a name="export-data"></a>
--------------

export data from sys.databases view into "c:\databases.sql" from "MyDatabase" on "server"

```bash
$ SqlDatabase export ^
      "-database=Data Source=server;Initial Catalog=database;Integrated Security=True" ^
      "-fromSql=SELECT * FROM sys.databases" ^
      -toFile=c:\databases.sql

PS> Export-SqlDatabase `
      -database "Data Source=server;Initial Catalog=database;Integrated Security=True" `
      -fromSql "SELECT * FROM sys.databases" `
      -toFile c:\databases.sql `
      -InformationAction Continue
```

 See more details [here](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/ExportData).

[Back to ToC](#table-of-contents)

Create a database <a name="create-database"></a>
---------------

create new database *[MyDatabase]* on server *[MyServer]* from scripts in *[Examples\CreateDatabaseFolder]* with "Variable1=value1" and "Variable2=value2"

```bash
$ SqlDatabase create ^
      "-database=Data Source=MyServer;Initial Catalog=MyDatabase;Integrated Security=True" ^
      -from=Examples\CreateDatabaseFolder ^
      -varVariable1=value1 ^
      -varVariable2=value2

PS> Create-SqlDatabase `
      -database "Data Source=MyServer;Initial Catalog=MyDatabase;Integrated Security=True" `
      -from Examples\CreateDatabaseFolder `
      -var Variable1=value1,Variable2=value2 `
      -InformationAction Continue
```

 See more details [here](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/CreateDatabaseFolder).

[Back to ToC](#table-of-contents)

Upgrade an existing database <a name="upgrade-database"></a>
----------------

upgrade existing database *[MyDatabase]* on server *[MyServer]* from scripts in *Examples\MigrationStepsFolder* with "Variable1=value1" and "Variable2=value2"

```bash
$ SqlDatabase upgrade ^
      "-database=Data Source=server;Initial Catalog=MyDatabase;Integrated Security=True" ^
      -from=Examples\MigrationStepsFolder ^
      -varVariable1=value1 ^
      -varVariable2=value2

PS> Upgrade-SqlDatabase `
      -database "Data Source=MyServer;Initial Catalog=MyDatabase;Integrated Security=True" `
      -from Examples\MigrationStepsFolder `
      -var Variable1=value1,Variable2=value2 `
      -InformationAction Continue
```

 See more details [here](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/MigrationStepsFolder).

[Back to ToC](#table-of-contents)

Scripts
-------

- *.sql* a text file with Sql Server scripts
- *.dll* or *.exe* an .NET assembly with a script implementation, details are [here](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/CSharpMirationStep)

[Back to ToC](#table-of-contents)

Variables
---------

In a sql text file any entry like *{{VariableName}}* or *$(VariableName)* is interpreted as variable and has to be changed (text replacement) with a value before script execution.
The variable name is
- a word from characters a-z, A-Z, 0-9, including the _ (underscore) character
- case insensitive

#### Example

```sql
-- script.sql
PRINT 'drop table {{Schema}}.{{Table}}'
DROP TABLE [{{Schema}}].[{{Table}}]
```

```bash
# execute script.sql
$ SqlDatabase execute -from=script.sql -varSchema=dbo -varTable=Person
PS> Execute-SqlDatabase -from script.sql -var Schema=dbo,Table=Person -InformationAction Continue

# output
script.sql ...
   variable Schema was replaced with dbo
   variable Table was replaced with Person
```

```sql
-- script at runtime
PRINT 'drop table dbo.Person'
DROP TABLE [dbo].[Person]
```

#### Example how to hide variable value from a log output

If a name of variable starts with _ (underscore) character, for instance *_Password*, the value of variable will not be shown in the log output.

```sql
-- script.sql
ALTER LOGIN [sa] WITH PASSWORD=N'{{_Password}}'
```

```bash
# execute script.sql
$ SqlDatabase execute -from=script.sql -var_Password=P@ssw0rd
PS> Execute-SqlDatabase -from script.sql -var _Password=P@ssw0rd -InformationAction Continue

# output
script.sql ...
   variable _Password was replaced with [value is hidden]
```

```sql
-- script at runtime
ALTER LOGIN [sa] WITH PASSWORD=N'{{P@ssw0rd}}'
```

A non defined variable`s value leads to an error and stops script execution process.

The variable value is resolving in the following order:

1. check command line
2. check environment variable (Environment.GetEnvironmentVariable())
3. check [configuration file](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/ConfigurationFile)

### Predefined variables

- *DatabaseName* - the target database name (-database=...Initial Catalog=MyDatabase...)
- *CurrentVersion* - the database version before execution of a [migration step](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/MigrationStepsFolder)
- *TargetVersion* - the database version after execution of a [migration step](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/MigrationStepsFolder)

[Back to ToC](#table-of-contents)

*.zip files <a name="zip-files"></a>
------------------------------------

Parameters *-from* and *-configuration* in the command line interpret .zip files in the path as folders, for example

* -from=c:\scripts.zip\archive\tables.zip\demo
* -from=c:\scripts.zip\archive\tables.zip\table1.sql
* -configuration=c:\scripts.zip\app.config

[Back to ToC](#table-of-contents)

Examples
--------

* [execute script(s)](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/ExecuteScriptsFolder)
* [export data](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/ExportData)
* [create a database](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/CreateDatabaseFolder)
* [upgrade an existing database](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/CreateDatabaseFolder)
* [configuration file](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/ConfigurationFile)
* [assembly script](https://github.com/max-ieremenko/SqlDatabase/tree/master/Examples/CSharpMirationStep)

[Back to ToC](#table-of-contents)

License
-------

This tool is distributed under the [MIT](https://github.com/max-ieremenko/SqlDatabase/tree/master/LICENSE) license.

[Back to ToC](#table-of-contents)