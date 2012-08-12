properties {
    $base_directory = Resolve-Path .. 
	$publish_directory = "$base_directory\publish"
	$build_directory = "$base_directory\build"
	$src_directory = "$base_directory\Src"
	$output_directory = "$base_directory\output"
	$packages_directory = "$src_directory\packages"

	$sln_file = "$base_directory\src\EventStore.sln"
	$keyfile = "$src_directory/EventStore.snk"
	$target_config = "Release"
	$framework_version = "v4.0"

	$mspec_path = "$packages_directory\Machine.Specifications.0.5.8\tools\mspec-x86-clr4.exe"
	$ilMergeModule.ilMergePath = "$base_directory\bin\ilmerge-bin\ILMerge.exe"

	if($runPersistenceTests -eq $null) {
		$runPersistenceTests = $false
	}
}

task default -depends Build

task Build -depends Clean, Compile, Test

task Compile {
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /t:Clean }

	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /p:TargetFrameworkVersion=v4.0 }
}

task Test -depends RunUnitTests, RunPersistenceTests, RunSerializationTests

task RunUnitTests {
	write-host "Unit Tests"

	exec { &$mspec_path "$src_directory/tests/EventStore.Core.UnitTests/bin/$target_config/EventStore.Core.UnitTests.dll" }
}

task RunPersistenceTests -precondition { $runPersistenceTests } {
	write-host "Acceptance Tests: Persistence Tests"

	exec { &$mspec_path "$src_directory/tests/EventStore.Persistence.AcceptanceTests/bin/$target_config/EventStore.Persistence.AcceptanceTests.dll" }
}

task RunSerializationTests {
	exec { &$mspec_path "$src_directory\tests\EventStore.Serialization.AcceptanceTests\bin\$target_config\EventStore.Serialization.AcceptanceTests.dll" }
}

task Package -depends Build, PackageEventStore, PackageMongoPersistence, PackageRavenPersistence, PackageJsonSerialization, PackageServiceStackSerialization, PackageNLogLogging, PackageLog4NetLogging {
	move $output_directory $publish_directory
}

task PackageEventStore -depends Clean, Compile {
	mkdir "$output_directory\bin" | out-null
	Merge-Assemblies -outputFile "$output_directory\bin\EventStore.dll" -exclude "EventStore.*" -keyfile $keyFile -files @(
		"$src_directory\proj\EventStore\bin\$target_config\EventStore.dll", 
		"$src_directory\proj\EventStore.Core\bin\$target_config\EventStore.Core.dll",
		"$src_directory\proj\EventStore.Serialization\bin\$target_config\EventStore.Serialization.dll",
		"$src_directory\proj\EventStore.Persistence.SqlPersistence\bin\$target_config\EventStore.Persistence.SqlPersistence.dll",
		"$src_directory\proj\EventStore.Wireup\bin\$target_config\EventStore.Wireup.dll"
	)
	
	write-host Rereferencing Merged Assembly
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /t:Clean }
	
	exec { msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /p:ILMerged=true /p:TargetFrameworkVersion=v4.0 }
}

task PackageMongoPersistence -depends Clean, Compile,PackageEventStore {
	mkdir $output_directory\plugins\persistence\mongo | out-null

	Merge-Assemblies -outputFile "$output_directory/plugins/persistence/mongo/EventStore.Persistence.MongoPersistence.dll" -exclude "MongoDB.*" -keyfile $keyFile -files @(
		"$src_directory/proj/EventStore.Persistence.MongoPersistence/bin/$target_config/EventStore.Persistence.MongoPersistence.dll",
		"$src_directory/proj/EventStore.Persistence.MongoPersistence.Wireup/bin/$target_config/EventStore.Persistence.MongoPersistence.Wireup.dll"
	)

	copy "$src_directory\proj\EventStore.Persistence.MongoPersistence\bin\$target_config\MongoDB*.dll" "$output_directory\plugins\persistence\mongo"
}

task PackageRavenPersistence -depends Clean, Compile,PackageEventStore {
	mkdir $output_directory\plugins\persistence\raven | out-null
	
	Merge-Assemblies -outputFile "$output_directory/plugins/persistence/raven/EventStore.Persistence.RavenPersistence.dll" -exclude "Raven.*" -keyfile $keyFile -files @(
		"$src_directory/proj/EventStore.Persistence.RavenPersistence/bin/$target_config/EventStore.Persistence.RavenPersistence.dll",
		"$src_directory/proj/EventStore.Persistence.RavenPersistence.Wireup/bin/$target_config/EventStore.Persistence.RavenPersistence.Wireup.dll"
	)

	copy "$src_directory\proj\EventStore.Persistence.RavenPersistence\bin\$target_config\Raven*.dll" "$output_directory\plugins\persistence\raven"
}

task PackageJsonSerialization -depends Clean, Compile,PackageEventStore {
	mkdir $output_directory\plugins\serialization\json-net | out-null

	Merge-Assemblies -outputFile "$output_directory/plugins/serialization/json-net/EventStore.Serialization.Json.dll" -exclude "EventStore.*" -keyfile $keyFile -files @(
		"$src_directory/proj/EventStore.Serialization.Json/bin/$target_config/EventStore.Serialization.Json.dll", 
		"$src_directory/proj/EventStore.Serialization.Json/bin/$target_config/Newtonsoft.Json*.dll",
		"$src_directory/proj/EventStore.Serialization.Json.Wireup/bin/$target_config/EventStore.Serialization.Json.Wireup.dll"
	)
}

task PackageServiceStackSerialization -depends Clean, Compile, PackageEventStore {
	mkdir $output_directory\plugins\serialization\servicestack | out-null

	Merge-Assemblies -outputFile "$output_directory/plugins/serialization/servicestack/EventStore.Serialization.ServiceStack.dll" -exclude "EventStore.*" -keyfile $keyFile -files @(
		"$src_directory/proj/EventStore.Serialization.ServiceStack/bin/$target_config/EventStore.Serialization.ServiceStack.dll",
		"$src_directory/proj/EventStore.Serialization.ServiceStack/bin/$target_config/ServiceStack.Text.dll", 
		"$src_directory/proj/EventStore.Serialization.ServiceStack.Wireup/bin/$target_config/EventStore.Serialization.ServiceStack.Wireup.dll"
	)
}

task PackageNLogLogging -depends Clean, Compile,PackageEventStore {
	mkdir $output_directory\plugins\logging\nlog | out-null
	copy "$src_directory\proj\EventStore.Logging.NLog\bin\$target_config\EventStore.Logging.NLog.*" "$output_directory\plugins\logging\nlog"

	copy "$src_directory\proj\EventStore.Logging.NLog\bin\$target_config\NLog.dll" "$output_directory\plugins\logging\nlog"
}

task PackageLog4NetLogging -depends Clean, Compile,PackageEventStore {
	mkdir $output_directory\plugins\logging\log4net | out-null

	copy "$src_directory\proj\EventStore.Logging.Log4Net\bin\$target_config\EventStore.Logging.Log4Net.*" "$output_directory\plugins\logging\log4net"
	
	copy "$src_directory\proj\EventStore.Logging.Log4Net\bin\$target_config\log4net.dll" "$output_directory\plugins\logging\log4net"
}

task PackageDocs {
	mkdir "$output_directory\doc"
	copy "$base_directory\doc\*.*" "$output_directory\doc"
}

task Clean {
	Clean-Item $publish_directory -ea SilentlyContinue
    Clean-Item $output_directory -ea SilentlyContinue
}