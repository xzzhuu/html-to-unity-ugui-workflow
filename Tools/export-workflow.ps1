param([string]$DrawioPath)
$ErrorActionPreference = 'Stop'
if (-not $DrawioPath) {
    $drawioCommand = Get-Command draw.io -ErrorAction SilentlyContinue
    if ($drawioCommand) { $DrawioPath = $drawioCommand.Source }
    else { throw 'Provide -DrawioPath with the path to your draw.io installation' }
}
$workspacePath = Split-Path $PSScriptRoot -Parent
$sourcePath = Join-Path $workspacePath 'Skills/ui-panel-design-to-html/assets/workflow.drawio'
if (-not (Test-Path -LiteralPath $DrawioPath)) { throw 'Install draw.io or provide -DrawioPath' }
[xml]$diagramDocument = Get-Content -LiteralPath $sourcePath -Raw -Encoding utf8
$profilePath = Join-Path $env:TEMP ('codex-drawio-export-' + [guid]::NewGuid().ToString('N'))
for ($pageNumber = 0; $pageNumber -lt $diagramDocument.mxfile.diagram.Count; $pageNumber++) {
    $pageDocument = [xml]$diagramDocument.OuterXml
    $diagrams = @($pageDocument.mxfile.diagram)
    for ($index = 0; $index -lt $diagrams.Count; $index++) {
        if ($index -ne $pageNumber) { $null = $pageDocument.mxfile.RemoveChild($diagrams[$index]) }
    }
    $temporaryPath = Join-Path $env:TEMP ('codex-workflow-' + [guid]::NewGuid().ToString('N') + '.drawio')
    $pageDocument.Save($temporaryPath)
    $fileName = if ($pageNumber -eq 0) { 'workflow.png' } else { 'workflow-design.png' }
    $outputPath = Join-Path (Split-Path $sourcePath -Parent) $fileName
    try {
        $process = Start-Process -FilePath $DrawioPath -ArgumentList @('--disable-gpu','--no-sandbox',('--user-data-dir="'+$profilePath+'"'),'--export','--format','png','--border=40','--output',('"'+$outputPath+'"'),('"'+$temporaryPath+'"')) -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $outputPath)) { throw 'draw.io export failed' }
        Write-Output $outputPath
    } finally { Remove-Item -LiteralPath $temporaryPath }
}
# The process profile is outside the workspace. Leave it for normal OS temp cleanup.
