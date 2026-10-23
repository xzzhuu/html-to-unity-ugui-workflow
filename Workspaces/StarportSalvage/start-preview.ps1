$taskRoot = $PSScriptRoot
Write-Host 'Open http://127.0.0.1:8148/index.html ; press Ctrl+C to stop.'
python -m http.server 8148 --bind 127.0.0.1 --directory $taskRoot
