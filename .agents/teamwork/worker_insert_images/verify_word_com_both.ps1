$word = New-Object -ComObject Word.Application
$word.Visible = $false
try {
    foreach ($f in @("c:\IT_System\.agents\teamwork\worker_insert_images\test_doc1.docx", "c:\IT_System\.agents\teamwork\worker_insert_images\test_doc2.docx")) {
        $doc = $word.Documents.Open($f, [Type]::Missing, $true)
        Write-Host "File $f opened successfully!"
        Write-Host "  InlineShapes count:" $doc.InlineShapes.Count
        Write-Host "  Tables count:" $doc.Tables.Count
        Write-Host "  Paragraphs count:" $doc.Paragraphs.Count
        $doc.Close([ref]$false)
    }
} catch {
    Write-Error "Word COM Failed: $($_.Exception.Message)"
} finally {
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
