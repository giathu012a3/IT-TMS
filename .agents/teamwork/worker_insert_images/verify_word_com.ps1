$word = New-Object -ComObject Word.Application
$word.Visible = $false
try {
    $filePath = "c:\IT_System\.agents\teamwork\worker_insert_images\test_doc1.docx"
    $doc = $word.Documents.Open($filePath, [Type]::Missing, $true)
    Write-Host "Word COM Opened Successfully!"
    Write-Host "InlineShapes count:" $doc.InlineShapes.Count
    Write-Host "Tables count:" $doc.Tables.Count
    Write-Host "Paragraphs count:" $doc.Paragraphs.Count
    $doc.Close([ref]$false)
} catch {
    Write-Error "Word COM Failed: $($_.Exception.Message)"
} finally {
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
