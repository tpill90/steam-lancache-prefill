$cdns = @(
    "cache1-adl-edgx.steamcontent.com",
    "cache1-adl-iioa.steamcontent.com",
    "cache2-adl-edgx.steamcontent.com",
    "cache2-adl-iioa.steamcontent.com",
    "cache1-bne-edgx.steamcontent.com",
    "cache1-bne-iioa.steamcontent.com",
    "cache2-bne-edgx.steamcontent.com",
    "cache2-bne-iioa.steamcontent.com",
    "cache1-mel-edgx.steamcontent.com",
    "cache2-mel-edgx.steamcontent.com",
    "cache1-mel-iioa.steamcontent.com",
    "cache1-syd1.steamcontent.com",
    "cache2-syd1.steamcontent.com",
    "cache3-syd1.steamcontent.com",
    "cache4-syd1.steamcontent.com",
    "cache5-syd1.steamcontent.com",
    "cache6-syd1.steamcontent.com",
    "cache7-syd1.steamcontent.com",
    "cache8-syd1.steamcontent.com"
)



$uri = "/depot/3527291/chunk/5e385330290f274474a065226bf6ccf0042a8e2d"

foreach ($cdn in $cdns) 
{
    try 
    {
        $headers = @{
        "user-agent" = "Valve/Steam HTTP Client 1.0";
        }

        $response = Invoke-WebRequest -Uri "https://$cdn$uri" -Method HEAD -UseBasicParsing -Headers $headers
        Write-Host "$cdn : $($response.StatusCode)"
    }
    catch 
    {
        Write-Host "$cdn :  " -NoNewLine
        Write-Host "$($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    }
}