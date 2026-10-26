$ErrorActionPreference = "Stop"
try {
    Write-Host "Initializing Microsoft Word COM object..."
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = [Microsoft.Office.Interop.Word.WdAlertLevel]::wdAlertsNone

    $files = @(
        "c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx",
        "c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx"
    )

    foreach ($file in $files) {
        Write-Host "Opening: $file"
        $doc = $word.Documents.Open($file, $false, $true)
        Write-Host "Opened successfully! Paragraphs: $($doc.Paragraphs.Count), Tables: $($doc.Tables.Count)"
        $doc.Close($false)
    }

    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    Write-Host "MICROSOFT WORD COM TEST COMPLETED SUCCESSFULLY."
} catch {
    Write-Error "Microsoft Word COM Automation Failed: $_"
    exit 1
}
