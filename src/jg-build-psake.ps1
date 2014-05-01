properties {
  $Build_Artifacts = 'output'
  $pwd = pwd
  $msbuild = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
  $nunit =  "$pwd\packages\NUnit.Runners.2.6.3\tools\nunit-console-x86.exe"
  $TestOutput = "$pwd\BuildOutput"
  $UnitTestOutputFolder = "$TestOutput\UnitTestOutput";
  $Company = "JustGiving";
  $version = Get-Version
  $year = Get-Year
  $Copyright = "$Company $year";
  $NugetPackages = @("JustGiving.EventStore.Http.Client", "JustGiving.EventStore.Http.SubscriberHost", "JustGiving.EventStore.Http.SubscriberHost.Ninject")
}

task default -depends Init, Clean, GetPackages, WriteNuspecNuget, Compile, Test, PackageNuget, PushNuget, Cleanup

task local -depends Init, Clean, GetPackages, WriteNuspecNuget, Compile, Test, PackageNuget, Cleanup 

task Init {
	
	if (!$PublicNugetApiKey) 
	{
		$PublicNugetApiKey = $Env:PublicNugetApiKey
	}
	
	Assert ($PublicNugetApiKey) 'PublicNugetApiKey was not passed in and is not in your environment...'
	$global:PublicNugetApiKey = $PublicNugetApiKey
	Generate-AllAssemblyInfo
}

task Clean {
  Remove-Item -Force *.nupkg
  Remove-Item -Force -Recurse $TestOutput -ErrorAction SilentlyContinue
  if((test-path  $Build_Artifacts -pathtype container))
  {
	   rmdir -Force -Recurse $Build_Artifacts
  }     
  if (Test-Path $TestOutput) 
  {
	   Remove-Item -force -recurse $TestOutput
  }  
  Exec {  & $msbuild /m:4 /verbosity:normal /nologo /p:OutDir=""$Build_Artifacts"" /t:Clean "$(Get-FirstSlnFile)" }  
  Get-ChildItem * -recurse | Where-Object {$_.PSIsContainer -eq $True} | where-object {$_.Name -eq "output"} | where-object {$_.Fullname.Contains("output\") -eq $false}| Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
}

task GetPackages {
  $sln = Get-FirstSlnFile
  Exec { .\nuget.exe restore "$sln" -OutputDirectory packages }
}


task WriteNuspecNuget{
	foreach($package in $NugetPackages){
		Write-Nuspec -PackageName $package;
	}
}

function Write-Nuspec {
	param($packageName)
	Write-Host "Writing NuSpec with package name $packageName $version"
	
	$file = Get-Item "$packageName.nuspec"
	$x = [xml] (Get-Content $file)     
	$x.package.metadata.version = [string]$version
	$x.package.metadata.copyright = "$company $year"
	$x.Save($file)
}

task Compile {  
   Exec {  & $msbuild /m:4 /verbosity:quiet /nologo /p:OutDir=""$Build_Artifacts\"" /t:Rebuild /p:Configuration=Release "$(Get-FirstSlnFile)" }   	
}

task Test { 			
	$sinkoutput = mkdir $TestOutput -Verbose:$false;  
    $sinkoutput = mkdir $UnitTestOutputFolder -Verbose:$false;  
	
	$unitTestFolders = Get-ChildItem *.Tests -recurse | Where-Object {$_.PSIsContainer -eq $True} | where-object {$_.Fullname.Contains("output")} | select-object FullName
	#Write-Host $unitTestFolders
	foreach($folder in $unitTestFolders)
	{
		$x = [string] $folder.FullName
		copy-item -force -path $x\* -Destination "$UnitTestOutputFolder\" 
	}
	cd $UnitTestOutputFolder
	$TestAssemblies = Get-ChildItem -Path "$UnitTestOutputFolder\"  -Filter *.Tests.dll -Recurse
	
	if (Is-TCBuild -eq $true)
	{
		Write-Host "Using TC test runner $(Get-TCTestRunner) v4.0 x86 NUnit-2.6.2 $TestAssemblies"
		$tcr = Get-TCTestRunner
		Exec { & $tcr v4.0 x86 NUnit-2.6.2 $TestAssemblies }	
	}
	else 
	{
		Exec { & $nunit $TestAssemblies /nologo /labels /framework=net-4.0 }
	}
	cd $pwd	
}

task PackageNuget{

	foreach($package in $NugetPackages){
		Create-Package -PackageName $package;
	}
}

function Create-Package{
	param($packageName)
	$fullFolder = "$Build_Artifacts\$packageName"
	$nuspec = "$packageName.nuspec"
		
	Copy-item -Recurse .\$packageName\output $fullFolder\
	Copy-item -Recurse -Force -Filter *.cs .\$packageName\ $Build_Artifacts\
	Copy-item $nuspec $fullFolder\
	Write-Host "Packaging: $fullFolder\$nuspec"
	Exec { .\NuGet.exe pack "$fullFolder\$nuspec" -BasePath $fullFolder -outputdirectory . -Symbols}
}

task PushNuget -precondition { return ($NugetPackages -ne $null) } {
	
	foreach ($package in $NugetPackages)
	{
		Push-Package -PackageName $package -Type "nuget"
	}
}

task Cleanup {
	$version = "1.0.0.0"
	foreach ($package in $NugetPackages)
	{
		Write-Nuspec -PackageName $package
	}
	
	Generate-AllAssemblyInfo
}

function Push-Package
{
	param($packageName, $type)
	
	$packages = gci *.nupkg | Where-Object {$_.name.StartsWith($packageName)} | `
	Foreach-Object{ 
		Write-Host "Pushing package $($_.name)..."
		Exec { .\nuget.exe setApiKey $global:PublicNugetApiKey }
		Exec { .\nuget.exe push $_.name }
	}
}

task ? -Description "Helper to display task info" {
    Write-Documentation
}

function Get-FirstSlnFile {
   return @(Get-Item *.sln)[0]  
}

function Make-Folder {
  param($path)
  if ((Test-path -path $path -pathtype container) -eq $false)
  {   
    mkdir $path -Verbose:$false
  }
}

function Get-Version {
	$buildNumber = Get-BuildVersion 
  
	if ($buildNumber -ne $null)
	{
		Write-Host "Using TC build number $buildNumber";
		return $buildNumber
	}
	
	$year = Get-Date -format "yyMM"
	$day = Get-Date -format "ddHH"
	$time = Get-Date -format "mmss"
	return "0.$year.$day.$time"
}

function Get-BuildVersion {
  return $env:TC_BUILD_NUMBER
}

function Get-TCTestRunner {
  return $env:TC_NUNIT_RUNNER
}

function Generate-AllAssemblyInfo {
	$files = Get-ChildItem * -recurse | Where-Object {$_.Fullname.Contains("AssemblyInfo.cs")}
	foreach ($file in $files)
	{
		Generate-Assembly-Info `
        -file $file.Fullname `
        -title "$ApplicationName $version" `
        -description $ApplicationName `
        -company $Company `
        -product "$ApplicationName $version" `
        -version $version `
        -copyright $Copyright
	}
}

function Generate-Assembly-Info
{
param(
    [string]$clsCompliant = "true",
    [string]$title, 
    [string]$description, 
    [string]$company, 
    [string]$product, 
    [string]$copyright, 
    [string]$version,
    [string]$file = $(Throw "file is a required parameter.")
)
  $asmInfo = "using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: CLSCompliantAttribute($clsCompliant)]
[assembly: ComVisibleAttribute(false)]
[assembly: AssemblyTitleAttribute(""$title"")]
[assembly: AssemblyDescriptionAttribute(""$description"")]
[assembly: AssemblyCompanyAttribute(""$company"")]
[assembly: AssemblyProductAttribute(""$product"")]
[assembly: AssemblyCopyrightAttribute(""$copyright"")]
[assembly: AssemblyVersionAttribute(""$version"")]
[assembly: AssemblyInformationalVersionAttribute(""$version"")]
[assembly: AssemblyFileVersionAttribute(""$version"")]
[assembly: AssemblyDelaySignAttribute(false)]
"

    $dir = [System.IO.Path]::GetDirectoryName($file)
    if ([System.IO.Directory]::Exists($dir) -eq $false)
    {
        Write-Host "Creating directory $dir"
        [System.IO.Directory]::CreateDirectory($dir)
    }
   # Write-Host "Generating assembly info file: $file"
    out-file -filePath $file -encoding UTF8 -inputObject $asmInfo
}

function Get-Year {
  $yyyy = get-date -Format yyyy
  return "$yyyy" -replace "`n",", " -replace "`r",", "
}

function Is-TCBuild 
{
  Test-Path env:\TEAMCITY_VERSION 
}