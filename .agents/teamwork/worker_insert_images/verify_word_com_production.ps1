$word = New-Object -ComObject Word.Application
$word.Visible = $false
try {
    foreach ($f in @("c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx", "c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx")) {
        $doc = $word.Documents.Open($f, [Type]::Missing, $true)
        Write-Host "File $f opened successfully via Microsoft Word 16.0 COM!"
        Write-Host "  InlineShapes count:" $doc.InlineShapes.Count
        Write-Host "  Tables count:" $doc.Tables.Count
        Write-Host "  Paragraphs count:" $doc.Paragraphs.Count
        Write-Host "  PL.1 Rows:" $doc.Tables.Item($doc.Tables.Count).Rows.Count
        Write-Host "  PL.1 Columns:" $doc.Tables.Item($doc.Tables.Count).Columns.Count
        $doc.Close([ref]$false)
    }
} catch {
    Write-Error "Word COM Verification Failed: $($_.Exception.Message)"
} finally {
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
