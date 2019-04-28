@{
	RootModule = "SqlDatabase"

	ModuleVersion = "{{ModuleVersion}}"
	GUID = "f19e643e-f998-4767-b2f9-958daf96137b"

	Author = "Max Ieremenko"
	Copyright = "(C) 2018-2019 Max Ieremenko."

	Description = "This module for SQL Server, allows executing scripts and database migrations."

	# for the PowerShell Desktop edition only.
	DotNetFrameworkVersion = '4.5.2'
	ProcessorArchitecture = 'None'

	CmdletsToExport = (
		"New-SqlDatabase",
		"Invoke-SqlDatabase",
		"Update-SqlDatabase",
		"Export-SqlDatabase"
	)

	AliasesToExport = @("Create-SqlDatabase", "Execute-SqlDatabase", "Upgrade-SqlDatabase")

	PrivateData = @{
		PSData = @{
			Tags = 'sql', 'SqlServer', 'sqlcmd', 'migration-tool', 'miration-step', 'sql-script', 'sql-database', 'database-migrations', 'export-data'
			LicenseUri = 'https://github.com/max-ieremenko/SqlDatabase/blob/master/LICENSE'
			ProjectUri = 'https://github.com/max-ieremenko/SqlDatabase'
			IconUri = 'https://raw.githubusercontent.com/max-ieremenko/SqlDatabase/master/icon-32.png'
			ReleaseNotes = 'https://github.com/max-ieremenko/SqlDatabase/releases'
		}
	 }
}