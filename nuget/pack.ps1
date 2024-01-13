#the "dotnet pack" command won't work for xamarin apps. Use desktop msbuild instead.
if ($IsMacOS) {
    $msbuild = "msbuild"
} else {
    $vswhere = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    $msbuild = join-path $msbuild 'MSBuild\Current\Bin\MSBuild.exe'
}

#####################
#Build release config
$version="1.0.7"
$versionSuffix=""
$nugetVersion="$version$versionSuffix"
#$versionSuffix=".$env:BUILD_NUMBER" 

cd $PSScriptRoot
del *.nupkg
& $msbuild "FluentRest.slnf" /restore /p:Configuration=Release /p:Platform="Any CPU" /p:Version="$version" /p:VersionSuffix="$versionSuffix" /p:Deterministic=false /p:PackageOutputPath="$PSScriptRoot" /p:IsHotRestartBuild=true --% /t:Clean;Build
& $msbuild "FluentRest.slnf" /p:Configuration=Release /p:Platform="Any CPU" /p:Version="$version" /p:VersionSuffix="$versionSuffix" /p:Deterministic=false /p:PackageOutputPath="$PSScriptRoot" /p:IsHotRestartBuild=true --% /t:FluentRest:Pack
if ($lastexitcode -ne 0) { exit $lastexitcode; }

####################
# PUSH
dotnet nuget push "Softlion.FluentRest.$nugetVersion.nupkg" 
