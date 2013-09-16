properties {
    $base_directory = Resolve-Path .. 
	$publish_directory = "$base_directory\publish-net40"
	$build_directory = "$base_directory\build"
	$src_directory = "$base_directory\src"
	$output_directory = "$base_directory\output"
	$packages_directory = "$src_directory\packages"
	$sln_file = "$src_directory\NEventStore.sln"
	$target_config = "Release"
	$signed_config = "Release-Signed"
	$framework_version = "v4.0"
	$version = "0.0.0.0"

	$xunit_path = "$base_directory\bin\xunit.runners.1.9.1\tools\xunit.console.clr4.exe"
	$ilMergeModule.ilMergePath = "$base_directory\bin\ilmerge-bin\ILMerge.exe"
	$nuget_dir = "$src_directory\.nuget"

	if($runPersistenceTests -eq $null) {
		$runPersistenceTests = $false
	}
}

task default -depends Build

task Build -depends Clean, UpdateVersion, Compile, Test

task UpdateVersion {
	$vSplit = $version.Split('.')
	if($vSplit.Length -ne 4)
	{
		throw "Version number is invalid. Must be in the form of 0.0.0.0"
	}
	$major = $vSplit[0]
	$minor = $vSplit[1]
	$assemblyFileVersion = $version
	$assemblyVersion = "$major.$minor.0.0"
	$versionAssemblyInfoFile = "$src_directory/VersionAssemblyInfo.cs"
	"using System.Reflection;" > $versionAssemblyInfoFile
	"" >> $versionAssemblyInfoFile
	"[assembly: AssemblyVersion(""$assemblyVersion"")]" >> $versionAssemblyInfoFile
	"[assembly: AssemblyFileVersion(""$assemblyFileVersion"")]" >> $versionAssemblyInfoFile
}

task Compile {
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /t:Clean }
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$signed_config /t:Clean }
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /p:TargetFrameworkVersion=v4.0 }
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$signed_config /p:TargetFrameworkVersion=v4.0 }
}

task Test -depends RunUnitTests, RunPersistenceTests, RunSerializationTests

task RunUnitTests {
	"Unit Tests"
	EnsureDirectory $output_directory
	Invoke-XUnit -Path $src_directory -TestSpec '*NEventStore.Tests.dll' `
    -SummaryPath $output_directory\unit_tests.xml `
    -XUnitPath $xunit_path
}

task RunPersistenceTests -precondition { $runPersistenceTests } {
	"Persistence Tests"
	EnsureDirectory $output_directory
	Invoke-XUnit -Path $src_directory -TestSpec '*Persistence.*.Tests.dll' `
    -SummaryPath $output_directory\persistence_tests.xml `
    -XUnitPath $xunit_path
}

task RunSerializationTests {
	"Serialization Tests"
	EnsureDirectory $output_directory
	Invoke-XUnit -Path $src_directory -TestSpec '*Serialization.*.Tests.dll' `
    -SummaryPath $output_directory\serialization_tests.xml `
    -XUnitPath $xunit_path
}

task Package -depends Build, PackageNEventStore, PackageMongoPersistence, PackageRavenPersistence, PackageJsonSerialization {
	move $output_directory $publish_directory
}

task PackageNEventStore -depends Clean, Compile {
	mkdir $publish_directory\bin\unsigned | out-null
	copy "$src_directory\NEventStore\bin\$target_config\NEventStore.???" "$publish_directory\bin\unsigned"
	
	mkdir $publish_directory\bin\signed | out-null
	copy "$src_directory\NEventStore\bin\$signed_config\NEventStore.???" "$publish_directory\bin\signed"
}

task PackageMongoPersistence -depends Clean, Compile {
	mkdir $publish_directory\plugins\persistence\mongo\unsigned | out-null
	copy "$src_directory\NEventStore.Persistence.MongoPersistence\bin\$target_config\NEventStore.Persistence.MongoPersistence.???" "$publish_directory\plugins\persistence\mongo\unsigned"

	mkdir $publish_directory\plugins\persistence\mongo\signed | out-null
	copy "$src_directory\NEventStore.Persistence.MongoPersistence\bin\$signed_config\NEventStore.Persistence.MongoPersistence.???" "$publish_directory\plugins\persistence\mongo\signed"
}

task PackageRavenPersistence -depends Clean, Compile {
	mkdir $publish_directory\plugins\persistence\raven\unsigned | out-null
	copy "$src_directory\NEventStore.Persistence.RavenPersistence\bin\$target_config\NEventStore.Persistence.RavenPersistence.???" "$publish_directory\plugins\persistence\raven\unsigned"
	
	mkdir $publish_directory\plugins\persistence\raven\signed | out-null
	copy "$src_directory\NEventStore.Persistence.RavenPersistence\bin\$signed_config\NEventStore.Persistence.RavenPersistence.???" "$publish_directory\plugins\persistence\raven\signed"
}

task PackageJsonSerialization -depends Clean, Compile {
	mkdir $publish_directory\plugins\serialization\json-net\unsigned | out-null

	Merge-Assemblies -outputFile "$publish_directory/plugins/serialization/json-net/unsigned/NEventStore.Serialization.Json.dll" -exclude "NEventStore.*"  -files @(
		"$src_directory/NEventStore.Serialization.Json/bin/$target_config/NEventStore.Serialization.Json.dll", 
		"$src_directory/NEventStore.Serialization.Json/bin/$target_config/Newtonsoft.Json*.dll"
	)
	
	mkdir $publish_directory\plugins\serialization\json-net\signed | out-null

	Merge-Assemblies -outputFile "$publish_directory/plugins/serialization/json-net/signed/NEventStore.Serialization.Json.dll" -exclude "NEventStore.*"  -files @(
		"$src_directory/NEventStore.Serialization.Json/bin/$signed_config/NEventStore.Serialization.Json.dll", 
		"$src_directory/NEventStore.Serialization.Json/bin/$signed_config/Newtonsoft.Json*.dll"
	)
}

task Clean {
	Clean-Item $publish_directory -ea SilentlyContinue
    Clean-Item $output_directory -ea SilentlyContinue
}

task NuGetPack -depends Package {
	gci -r -i *.nuspec "$nuget_dir" |% { .$nuget_dir\nuget.exe pack $_ -basepath $base_directory -o $publish_directory -version $version }
}

function EnsureDirectory {
	param($directory)

	if(!(test-path $directory))
	{
		mkdir $directory
	}
}