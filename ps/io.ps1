Function CreateDir ($path){

    if((Test-Path $path) -eq 0) {
        mkdir $path | out-null;
    }
}

function EnsureDirExistsAndIsEmpty ($path){

    if((Test-Path $path) -eq 0) {
        mkdir $path | out-null;
    } 
    else {
        Remove-Item $path -rec -force | out-null
        mkdir $path | out-null;
    }
}

function DownloadFileIfNotExists($src , $dstDirectory, $checkFile){
    $msg = "File src: '$src'; Dst dir: '$dstDirectory'; Check file: '$checkFile'"
    If (-not (Test-Path $checkFile)){
        Write-Host "$msg ; Check file not exists - processing"
        If (-not (Test-Path $dstDirectory)){
            New-Item -ItemType directory -Path $dstDirectory
        }
        Invoke-WebRequest $src -OutFile $checkFile
    } else {
        Write-Host "$msg ; Check file exists - exiting"
    }
}

function DownloadNugetIfNotExists ($nuget, $packageName, $dstDirectory, $checkFile) {
	$msg = "Package name: '$packageName'; Dst dir: '$dstDirectory'; Check file: '$checkFile'"
	If (-not (Test-Path  $checkFile)){
		Write-Host "$msg ; Check file not exists - processing"
		& $nuget install $packageName -excludeversion -outputdirectory $dstDirectory
	} else {
		Write-Host "$msg ; Check file exists - exiting"
	}
}