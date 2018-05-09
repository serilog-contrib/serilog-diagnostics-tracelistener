param(
    [String] $majorMinor = "0.0",  # 2.0
    [String] $patch = "0",         # $env:APPVEYOR_BUILD_VERSION
    [Switch] $notouch,
    [String] $sln                  # e.g serilog-sink-name
)

function Set-AssemblyVersions($informational, $assembly)
{
    (Get-Content assets/CommonAssemblyInfo.cs) |
        ForEach-Object { $_ -replace """1.0.0.0""", """$assembly""" } |
        ForEach-Object { $_ -replace """1.0.0""", """$informational""" } |
        ForEach-Object { $_ -replace """1.1.1.1""", """$($informational).0""" } |
        Set-Content assets/CommonAssemblyInfo.cs
}

function Invoke-DotNetBuild()
{
    dotnet build --verbosity minimal -c Release
    if ($LASTEXITCODE -ne 0)
    {
    	throw "Build failed with exit code $LASTEXITCODE"
    }
}

function Invoke-DotNetTest()
{
    # Due to https://github.com/Microsoft/vstest/issues/1129 we have to be explicit here 
    ls test/**/*.csproj |
        ForEach-Object {
            dotnet test $_ -c Release --logger:Appveyor
            if ($LASTEXITCODE -ne 0)
            {
            	throw "Testing $_ failed with exit code $LASTEXITCODE"
            }
        }
}

function Invoke-DotNetPackProj($csproj)
{
    dotnet pack $csproj -c Release --include-symbols 
    if ($LASTEXITCODE -ne 0)
    {
	    throw "Packing $csproj failed with exit code $LASTEXITCODE"
	}
}

function Invoke-DotNetPack($version)
{
    ls src/**/*.csproj |
        ForEach-Object { Invoke-DotNetPackProj $_ }
}

function Invoke-Build($majorMinor, $patch, $customLogger, $notouch, $sln)
{
    $package="$majorMinor.$patch"
    $slnfile = "$sln.sln"

    Write-Output "$sln $package"

    if (-not $notouch)
    {
        $assembly = "$majorMinor.0.0"

        Write-Output "Assembly version will be set to $assembly"
        Set-AssemblyVersions $package $assembly
    }

    Invoke-DotNetBuild
    Invoke-DotNetTest
    Invoke-DotNetPack $package
}

$ErrorActionPreference = "Stop"

if (-not $sln)
{
    $slnfull = ls *.sln |
        Where-Object { -not ($_.Name -like "*net40*") } |
        Select -first 1

    $sln = $slnfull.BaseName
}

Invoke-Build $majorMinor $patch $customLogger $notouch $sln
