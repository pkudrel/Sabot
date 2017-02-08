<#
.Synopsis
	Build script (https://github.com/nightroman/Invoke-Build)
#>


param(
		
		$configPath,
		$scriptsPath,
		$buildTarget,
		$buildPath,
		$buildEnv,
		$buildVersion,
		$repoPath,
		$buildDateTime,
		$conf = (Get-Content $configPath | Out-String | ConvertFrom-Json),
		$publishRepoDir = (Join-Path (Split-Path $repoPath -Parent) $conf.PublishRepoSubDir), 
		$functionRoot = (Join-Path $repoPath "src\SimpleFun"), 
		$toolsPath,
		$buildTmpDir = (Join-Path $buildPath "tmp"),
		$projects = ([PSCustomObject]$conf.Projects),
		$buildDir = "build",
		$readyDir =  (Join-Path $buildPath "ready"),
		$srcDir = (Join-Path  $repoPath $conf.SrcDir),
		$packagesDir = (Join-Path  $repoPath $conf.PackagesDir),
		$nuget = (Join-Path $scriptsPath "tools\nuget\nuget.exe"),
		$gitversion = (Join-Path $repoPath $conf.Gitversion),
		$gitBranch,
		$gitCommitNumber,
		$buildMiscInfo

    )


# inser tools
. (Join-Path $scriptsPath "vendor\ps-auto-helpers\ps\misc.ps1")
. (Join-Path $scriptsPath "vendor\ps-auto-helpers\ps\io.ps1")
use 14.0 MSBuild

$scriptsPath

# Synopsis: Remove temp files.
task Clean {

	
	Write-Host "Clean: $buildPath"
	EnsureDirExistsAndIsEmpty $buildPath

}

# Synopsis: Package-Restore
task RestorePackage {

	Push-Location  $repoPath
	$slnFile = Join-Path $repoPath  $conf.SlnFilePath
	exec {  &$nuget restore $slnFile  }
	Pop-Location

}

# Synopsis: Update-TeamCity
task Update-TeamCity -If (($env:TEAMCITY_VERSION).Length -ne 0) {
		$tvc = $env:TEAMCITY_VERSION
		Write-Host "Setup TeamCity: $tvc" 
		$s = $buildVersion.SemVer
		Write-Host  "##teamcity[buildNumber '$s']"
		try {
			$max = $host.UI.RawUI.MaxPhysicalWindowSize
			if($max) {
			$host.UI.RawUI.BufferSize = New-Object System.Management.Automation.Host.Size(9999,9999)
			$host.UI.RawUI.WindowSize = New-Object System.Management.Automation.Host.Size($max.Width,$max.Height)
		}
		} catch {}
	
}


# Synopsis: Build the project.
task Build {
	
	Write-Host "*** Build *** $projects"

	$out = $buildTmpDir
	$srcWorkDir = Join-Path $srcDir "HostingAnalyzer"
	$currentAssemblyInfo = "AssemblyInfo.cs"
	$oldAssemblyInfo = "AssemblyInfo.cs.old"

	Update-AssemblyInfo $srcWorkDir $currentAssemblyInfo $buildVersion.AssemblyVersion $buildVersion.AssemblyFileVersion $buildVersion.AssemblyInformationalVersion $conf.ProductName $conf.CompanyName $conf.Copyright

	

	try {

		foreach ($p in $projects) {
			Write-Host "Build; Project: $($p.Name)"
			$projectFile =  $projectFile = Join-Path $repoPath $p.Path 
			$out = Join-Path (Join-Path $buildTmpDir $p.OutputPathDirSufix) $buildDir;
			Write-Host "Build; Project file: $projectFile"
			Write-Host "Build; Out dir: $out"
			exec { msbuild $projectFile /t:Build /p:Configuration=$buildTarget /v:quiet /p:OutDir=$out     } 
		}
	}
	catch {
		Restore-AssemblyInfo $srcWorkDir $currentAssemblyInfo $oldAssemblyInfo
		throw $_.Exception
		exit 1
	}
	finally {
		Restore-AssemblyInfo $srcWorkDir $currentAssemblyInfo $oldAssemblyInfo
	}

}




task CopyToPublishRepo {
	Write-Host "PublishRepoDir: $publishRepoDir"
	Write-Host "FunctionRoot: $functionRoot"

	Copy-Item "$functionRoot\host.json" -destination $publishRepoDir
	Copy-Item "$functionRoot\appsettings.json" -destination $publishRepoDir

	$funDirs =  (Get-ChildItem -Path $functionRoot -Filter "function.json" -Recurse) | Split-Path  | Convert-Path 


	Write-Host 	$funDirs 
	foreach ($fd in $funDirs) {
		Write-Host "Processing dir: $fd"
		$dirName = Split-Path $fd -Leaf
		$dst = Join-Path $publishRepoDir $dirName
		If (-not (Test-Path $dst)){New-Item -ItemType directory -Path $dst }
		Copy-Item "$fd\function.json" -destination $dst
		$runItem = (Get-ChildItem -Path $fd -Filter "run.*" )  | select -last 1 
		Copy-Item "$fd\$runItem" -destination $dst

		## local function config
		$cPath = "$fd\deneblab.json"
		If (-not (Test-Path $cPath)){ throw "Config not found: $cPath" }
		$c = (Get-Content $cPath  | Out-String | ConvertFrom-Json)

	  

		 # add solution reference
		 # array 
		 $c.soulutionRef | % {
			 # of custom objects
			 $_.psobject.properties | % {
				$project = $_.Name
				$projectRefs = $_.Value
				# array 
				$projectRefs | % {
					 # of custom objects
					 $_.psobject.properties | % {
					 	$src1 = [io.path]::combine($buildPath, $project, $_.Name) 
						$dst1 = [io.path]::combine($dst, $_.Value)
				
						If (Test-Path $src1 ){
							Create-Dir $dst1
							Copy-Item "$src1/*" -destination $dst1
						}
					 }
				}
			 }
		 }
	}



}




task CheckTools {
	Write-Host "Check: Nuget"
	DownloadIfNotExists "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" $nuget 

}

function DownloadIfNotExists($src , $dst){

	If (-not (Test-Path $dst)){
		$dir = [System.IO.Path]::GetDirectoryName($dst)
		If (-not (Test-Path $dir)){
			New-Item -ItemType directory -Path $dir
		}
	 	Invoke-WebRequest $src -OutFile $dst
	}
}


# Synopsis: Build and clean.



task Pre CheckTools, Update-TeamCity, Clean, RestorePackage, Build
task . Pre