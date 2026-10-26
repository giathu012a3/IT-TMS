$ErrorActionPreference = "Stop"
$word = New-Object -ComObject Word.Application
$word.Visible = $false
try {
    Write-Host "Opening BaoCao_ITTMS_TKPMHDT (4).docx..."
    $doc1 = $word.Documents.Open('c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx', [Type]::Missing, $true)
    Write-Host "Doc1 opened successfully. Paragraphs: $($doc1.Paragraphs.Count), Tables: $($doc1.Tables.Count)"
    $doc1.Close([ref]$false)

    Write-Host "Opening BaoCao_ITTMS_TKPMHDT_DotNet9.docx..."
    $doc2 = $word.Documents.Open('c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx', [Type]::Missing, $true)
    Write-Host "Doc2 opened successfully. Paragraphs: $($doc2.Paragraphs.Count), Tables: $($doc2.Tables.Count)"
    $doc2.Close([ref]$false)

    Write-Host "RESULT: SUCCESS_BOTH_DOCUMENTS"
} catch {
    Write-Error "Failed to open document: $_"
    exit 1
} finally {
    $word.Quit([ref]$false)
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
