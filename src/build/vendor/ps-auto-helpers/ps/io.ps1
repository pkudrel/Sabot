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

function DownloadFileIfNotExists($src , $dst){

    If (-not (Test-Path $dst)){
        $dir = [System.IO.Path]::GetDirectoryName($dst)
        If (-not (Test-Path $dir)){
            New-Item -ItemType directory -Path $dir
        }
        Invoke-WebRequest $src -OutFile $dst
    }
}