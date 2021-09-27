param(
    [Parameter(Mandatory=$true)]
    [string]$HASH
)

$files = git diff --name-only --diff-filter=M $HASH

foreach($file in $files){
    $content = Get-Content $file;
    $new = @();
    $changed = $false;

    foreach($line in $content){
        $new += $line;

        if(-not $changed -and $line -match "copyright>"){
            $new += "`r`n// Modified by Splunk Inc."
            $changed = $true;
        }
    }

    if($changed) {
        Set-Content $file -Value $new
    }
}
