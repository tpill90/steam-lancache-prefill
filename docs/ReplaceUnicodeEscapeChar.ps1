$directoryPath = "C:\Users\Tim\Dropbox\Programming\Lancache-Prefills\SteamPrefill\docs\img\"

# Get all .ansi files in the directory
$files = Get-ChildItem -Path $directoryPath -Filter "*.ansi"

# Define a regular expression pattern to match Unicode characters
$pattern = '\\u([0-9a-fA-F]{4})'

# Define a function to replace Unicode characters with their actual characters
function ReplaceUnicodeCharacters($match) {
    $unicode = [char]::ConvertFromUtf32([Convert]::ToInt32($match.Groups[1].Value, 16))
    return $unicode
}

# Process each file
foreach ($file in $files) {
    # Read the content of the file
    $content = Get-Content -Raw -Path $file.FullName
    
    # Replace Unicode characters using the regular expression and the defined function
    $newContent = [regex]::Replace($content, $pattern, { param($match) ReplaceUnicodeCharacters $match })

    # Write the modified content back to the file
    $newContent | Set-Content -Path $file.FullName
    
    Write-Host "Unicode characters replaced in $($file.Name) successfully."
}

Write-Host "All files processed."