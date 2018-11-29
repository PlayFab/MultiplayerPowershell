Set-StrictMode -Version Latest
$global:tokenExpirationTimestamp = [System.DateTime]::MinValue;

<#
    .SYNOPSIS
        Internal helper to call a playFab API, wait for its task to complete, and handle some boilerplate error checking/logging of failures.
        Returns the API response (the PlayFabResult<T>.Result obj) on success; throws on errors.

    .PARAMETER ApiName
        Used for logging; typically the playfab method name (eg: ListQosServersAsync)

    .PARAMETER ApiCall
        The api call in a script block; expected to return a Task with a playfab result.
        eg: { [PlayFab.PlayFabMultiplayerAPI]::ListQosServersAsync($null) }

    .PARAMETER SkipTokenCheck
        Optional.  Set this switch to bypass the check for an entity token.
        Typically only used for the entity token API call; all other PlayFab APIs require a token.

    .PARAMETER WarnIfPaged
        Optional.  If set, we check for a NextLink field on a successful response and warn if the results have a next link (paged results).
#>
function CallPlayFabApi
{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $true)]
        [string] $ApiName,

        [Parameter(Mandatory = $true)]
        [scriptblock] $ApiCall,

        [Parameter(Mandatory = $false)]
        [switch] $SkipTokenCheck = $false,

        [Parameter(Mandatory = $false)]
        [switch] $WarnIfPaged = $false # most calls aren't paged in the first place
    )

    # login / entity token check
    if (-not $SkipTokenCheck -and [string]::IsNullOrEmpty([PlayFab.PlayFabSettings]::TitleId))
    {
        $msg = "Call Get-PFTitleEntityToken first to setup your credentials for a PlayFab title."
        Write-Warning $msg
        throw "PlayFabSettings doesn't have a configured titleId; $msg"
    }

    # If the token is near expiry, refresh it (to avoid opening a new powershell session).
    if (-not $SkipTokenCheck -and $global:tokenExpirationTimestamp -lt [System.DateTime]::UtcNow)
    {
        Write-Host "Refreshing Entity Token.."
        RefreshEntityToken;
        Write-Host "Refreshing Entity Token complete."
    }

    # send/wait for API response
    $apiTask = & $ApiCall
    $apiTask.Wait()
    
    if ($apiTask.IsFaulted)
    {
        $aggregateEx = $apiTask.Exception.Flatten()
        $errMsg = "PlayFab API $ApiName failed with exceptions:"
        foreach ($ex in $aggregateEx.InnerExceptions)
        {
            $errMsg += $ex.Message
        }
        Write-Warning $errMsg
        throw $ex
    }

    # boilerplate error handling
    $playFabResult = $apiTask.Result

    if ($playFabResult.Error -ne $null)
    {
        $pfError = $PlayFabResult.Error
        $errReport = $pfError.GenerateErrorReport()
        $errMsg = "PlayFab API $ApiName returned Http StatusCode $($pfError.HttpCode) ($($pfError.HttpStatus)); ErrorMsg: $($pfError.ErrorMessage)."
        Write-Warning "$errMsg `n  PlayFab Error Report: $errReport"
        throw $errMsg
    }

    $result = $PlayFabResult.Result

    if ($WarnIfPaged -and $result.SkipToken -ne $null)
    {
        Write-Warning "There are many results requiring paging, this operation may take some time."
    }

    return $result
}

function RefreshEntityToken()
{
    # This assumes that Get-PfTitleEntityToken has been called once with the appropriate parameter values. 
    $tokenRequest = new-object PlayFab.AuthenticationModels.GetEntityTokenRequest
    $entityTokenResult = CallPlayFabApi -ApiName GetEntityTokenAsync -ApiCall { [PlayFab.PlayFabAuthenticationAPI]::GetEntityTokenAsync($tokenRequest) } -SkipTokenCheck
    $global:tokenExpirationTimestamp = [System.DateTime]::UtcNow.AddHours(12);
}

function Enable-PFMultiplayerServer
{
    [CmdletBinding( )]
    Param()
    
    Begin
    {}

    Process
    {
        
        $qosServersResult = CallPlayFabApi -ApiName EnableMultiplayerServersForTitleAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::EnableMultiplayerServersForTitleAsync($null) }
       
        $continueLoop = $True
        $timepassed = 0
        start-sleep -s 3
        while($continueLoop)
        {
            start-sleep -s 3
            $StatusResult = CallPlayFabApi -ApiName GetTitleEnabledForMultiplayerServersStatusAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::GetTitleEnabledForMultiplayerServersStatusAsync($null) }
            $progress = "Querying Status: " +  $StatusResult.Status + "; " + $timepassed + " seconds passed...."
            write-progress $progress
            $timepassed = $timepassed + 3
            $continueLoop = ($StatusResult.Status -ne "Enabled")
        }
     }

    <# 
.SYNOPSIS 
Enables PlayFab Multiplayer Server feature 
.DESCRIPTION 
Enables PlayFab Multiplayer Server feature for the title specified by the provided entity token.
#>
}





function Get-PFMultiplayerQosServer
{
    [CmdletBinding( )]
    Param()
    
    Begin
    {}

    Process
    {
        $qosServersResult = CallPlayFabApi -ApiName ListQosServersAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::ListQosServersAsync($null) }
        return $qosServersResult.QosServers
     }

    <# 
.SYNOPSIS 
Gets a list of QoS servers. 
 
.DESCRIPTION 
Returns list of QoS servers to use for network performance measurements and region selection during allocation. 
#>
}

function Get-PFTitleEntityToken
{
    [CmdletBinding( )]
    Param(
    [Parameter(Mandatory = $true)][string]$TitleID, 
    [Parameter(Mandatory = $true)][string]$SecretKey 
    )
    
    Begin
    {}

    Process
    {
 
        #Sets the public properties of the API's static class that configure which title target via entity tokens
        [PlayFab.PlayFabSettings]::TitleID = $TitleID
        [PlayFab.PlayFabSettings]::DeveloperSecretKey = $SecretKey

        $key = new-object PlayFab.AuthenticationModels.EntityKey
        $key.ID = $TitleID
        
        $tokenRequest = new-object PlayFab.AuthenticationModels.GetEntityTokenRequest
        $tokenRequest.Entity = $key

        $entityTokenResult = CallPlayFabApi -ApiName GetEntityTokenAsync -ApiCall { [PlayFab.PlayFabAuthenticationAPI]::GetEntityTokenAsync($tokenRequest) } -SkipTokenCheck
        $global:tokenExpirationTimestamp = [System.DateTime]::UtcNow.AddHours(12);
        return $entityTokenResult
     }
     <# 
.SYNOPSIS 
Gets an entity token using the provided title and secret key. Required for other Entity API interactions. 
 
.DESCRIPTION 
Using a secret key generated in Game Manager, this cmdlet generates and entity token for the specified title id. The entity token will be used for authenticating other PlayCompute cmdlets for the length of the PowerShell session. 
#>
}

function Get-PFMultiplayerAsset
{
    [CmdletBinding( )]
    Param( 
        [Parameter(Mandatory = $false)][string] $SkipToken)
    
    Begin
    {}

    Process
    {
        $assetReq = NEW-OBJECT PlayFab.MultiplayerModels.ListAssetSummariesRequest
        
        if ($SkipToken -ne "")
        {
            $assetReq.SkipToken = $SkipToken
            $assetReq.PageSize = 10
        }

        $assetsResult = CallPlayFabApi -ApiName ListAssetSummariesAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::ListAssetSummariesAsync($assetReq) } -WarnIfPaged
        $SkipToken = $assetsResult.SkipToken

        if ($SkipToken -ne "")
        {
            write-progress "Getting more assets" 
            $newresult = Get-PFMultiplayerAsset -SkipToken $SkipToken
            $result = $assetsResult.AssetSummaries +  $newresult
            return $result
        }

        return $assetsResult.AssetSummaries
     }

     <# 
.SYNOPSIS 
 
Gets the game server assets that have been uploaded 
 
.DESCRIPTION 
 
Gets the game server assets that have been added through Add-PFMultiplayerAssets or GetAssetUploadURL API. Command is run in the context of the title specified using Get-PFTitleEntityToken 
#>
}

function Add-PFMultiplayerAsset
{
    [CmdletBinding( )]
    Param(
        [Parameter(Mandatory = $true)]
        [string] $FilePath
    )
    
    Begin
    {}

    Process
    {
        if (-not (Test-Path $FilePath))
        {
            write-error "Provided file path is not valid"
            return
        }

        $assetFileItem = Get-Item $FilePath
        $assetHash = Get-FileHash $FilePath -Algorithm MD5

        $assetReq = NEW-OBJECT PlayFab.MultiplayerModels.GetAssetUploadUrlRequest

        $BlobName = $FilePath | Split-Path -Leaf
        $assetReq.FileName = $BlobName

        $metadata = New-Object 'System.Collections.Generic.Dictionary[String,String]'
        $metadata.Add("OriginalFilePath",$FilePath)
        $metadata.Add("sizeBytes", $assetFileItem.Length)
        $metadata.Add("md5", $assetHash.Hash)
        $metadata.Add("uploadTimeUtc", [DateTime]::UtcNow.ToString())
        
        $assetsResult = CallPlayFabApi -ApiName GetAssetUploadUrlAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::GetAssetUploadUrlAsync($assetReq) }

        $sastoken = $assetsResult.AssetUploadUrl
        $sastoken = $sastoken.Remove($sastoken.LastIndexOf("&api"))
        $storageaccountname = $sastoken.SubString(8,$sastoken.IndexOf("blob")-9)
        $sastoken = $sastoken.substring($sastoken.IndexOf("sv"))

        $accountContext = New-AzureStorageContext -SasToken $sasToken -StorageAccountName   $storageaccountname 
        ## $blob = Get-AzureStorageBlob -Container "gameassets" -Blob $ID -Context $accountContext 
        Set-AzureStorageBlobContent -File $FilePath -Container "gameassets" -Context $accountContext -Blob $BlobName -MetaData $MetaData -Force
     }

          <# 
.SYNOPSIS 
Uploads an asset to PlayFab. 
 
.DESCRIPTION 
Upload an asset (commonly a zip file) by providing a friendly name and file path. This cmdlet uses the GetAssetUploadURl API to get an Azure blob URL, and then uses Azure storage cmdlets to upload the asset. 
#>

}

function Add-PFMultiplayerCertificate
{
    [CmdletBinding( )]
    Param(
    [Parameter(Mandatory = $true)][string]$Name, 
    [Parameter(Mandatory = $true)][string]$FilePath,
    [Parameter(Mandatory = $false)][string]$Password 
    )
    
    Begin
    {}

    Process
    {
    
        $PathTest = Test-Path $FilePath
        if ($PathTest -eq $false)
        {
            write-error "Provided file path is not valid"
            return 
        }

        $certificateBytes = [System.IO.File]::ReadAllBytes($FilePath)
        $base64 = [System.Convert]::ToBase64String($certificateBytes)

        $cert = NEW-OBJECT PlayFab.MultiplayerModels.Certificate
        $cert.Base64EncodedValue = $base64 
        $cert.Name = $Name

        if($Password -ne $null)
        {
            $cert.Password = $Password
        }

        $certReq = NEW-OBJECT PlayFab.MultiplayerModels.UploadCertificateRequest
        $certReq.GameCertificate = $cert

        return CallPlayFabApi -ApiName UploadCertificateAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::UploadCertificateAsync($certReq) }
     }

          <# 
.SYNOPSIS 
Uploads a certificate to PlayFab. 
 
.DESCRIPTION 
Uploads a certificate to PlayFab for game server usage. Cmdlet does not support certificates with passwords but coming soon. 
#>

}


function Remove-PFMultiplayerAsset
{
    [CmdletBinding()]
    Param( 
    [Parameter(ParameterSetName = "SpecificName", Mandatory = $true,Position=0)][alias("AssetName","Asset")][string]$FileName
    )
    
    Begin
    {}

    Process
    {
        $assetReq = NEW-OBJECT PlayFab.MultiplayerModels.DeleteAssetRequest
        $assetReq.FileName = $FileName

        return CallPlayFabApi -ApiName DeleteAssetAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::DeleteAssetAsync($assetReq) }
    }

     <# 
.SYNOPSIS 
Removes the specified multiplayer asset
 
.DESCRIPTION 
Removes the specified multiplayer asset
 
#>
}

function Get-PFMultiplayerCertificate
{
    [CmdletBinding( )]
    Param()
    
    Begin
    {}

    Process
    {
        $listCertResult = CallPlayFabApi -ApiName ListCertificateSummariesAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::ListCertificateSummariesAsync($null) }
        return $listCertResult.CertificateSummaries
     }

     <# 
.SYNOPSIS 
 
Gets the game server certificates that have been uploaded 
 
.DESCRIPTION 
 
Gets the game server Certificate that have been added through Add-PFMultiplayerCertificate. 
#>
}

function Get-PFMultiplayerBuild
{
    [CmdletBinding( DefaultParameterSetName="All")]
    Param( 
    [Parameter(ParameterSetName = "SpecificName", Mandatory = $true)][string]$BuildName,
    [Parameter(ParameterSetName = "All")][Switch]$All,
    [Parameter()][Switch]$Detailed
    )
    
    Begin
    {}

    Process
    {
        $buildsResult = CallPlayFabApi -ApiName ListBuildSummariesAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::ListBuildSummariesAsync($null) }

         $SummaryReturn =  $buildsResult.BuildSummaries 

        if (($PSCmdlet.ParameterSetName -eq "All") -and (-not $Detailed) )
        {
            return  $SummaryReturn
        }

        if ($PSCmdlet.ParameterSetName -eq "SpecificName")
        { 
            Write-Host "$($SummaryReturn.GetType())";
            $SummaryReturn = $SummaryReturn | Where-Object -Property BuildName -Contains $BuildName
        }

        Write-Host "Summary type: $($SummaryReturn.GetType())";
        if($Detailed)
        {
            $a = 12
            $result = New-Object System.Collections.ArrayList
            foreach ($build in $SummaryReturn)
            {
                $buildReq = NEW-OBJECT PlayFab.MultiplayerModels.GetBuildRequest
                $buildReq.BuildID = $build.BuildID

                $buildsResult = CallPlayFabApi -ApiName GetBuildAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::GetBuildAsync($buildreq) }
                Write-Host "$($buildsResult.GetType())";
                Write-Host "Result count before : $($result.Count)";
                $result.Add($buildsResult)
                Write-Host "Result count after : $($result.Count)";
            }

            Write-Host "Length : $($result.Count)"
            $SummaryReturn = $result;
            #return $result
        }

        Write-Host "returning summary"
        return $SummaryReturn
     }

     <# 
.SYNOPSIS 
 
Gets the game server builds that have been created 
 
.DESCRIPTION 
 
Gets the game server builds 
 
#>
}

function New-PFMultiplayerBuild
{
    [CmdletBinding( )]
    Param(
    [Parameter(Mandatory = $true)][string]$BuildName, 
    [Parameter(Mandatory = $true)][string]$AssetFileName, 
    [Parameter(Mandatory = $true)][string]$AssetMountPath,
    [Parameter(Mandatory = $true)][string]$StartMultiplayerServerCommand,
    [Parameter(Mandatory = $true)]$MappedPorts,
    [Parameter(Mandatory = $true)]$VMSize,
    [Parameter(Mandatory = $false)]$BuildCerts
    )
    
    Begin
    {}

    Process
    {

        $BuildReq = NEW-OBJECT PlayFab.MultiplayerModels.CreateBuildWithManagedContainerRequest
        $BuildReq.BuildName = $BuildName
        $BuildReq.ContainerFlavor = [PlayFab.MultiplayerModels.ContainerFlavor]::ManagedWindowsServerCore
 
        $BuildReq.StartMultiplayerServerCommand = $StartMultiplayerServerCommand
        $BuildReq.Ports = $MappedPorts
        $BuildReq.VMSize = $VMSize

        $BuildReq.GameCertificateReferences =  $BuildCerts
 
        $BuildReq.MultiplayerServerCountPerVm = 1 

        $Asset = NEW-OBJECT PlayFab.MultiplayerModels.AssetReferenceParams
        $Asset.FileName = $AssetFileName
        $Asset.MountPath = $AssetMountPath
        $BuildReq.GameAssetReferences = $Asset

        $Regions = NEW-OBJECT PlayFab.MultiplayerModels.BuildRegionParams
        $Regions.MaxServers = 10
        $Regions.Region = [PlayFab.MultiplayerModels.AzureRegion]::EastUs
        $Regions.StandbyServers = 2
        $BuildReq.RegionConfigurations = $Regions
    
        
        $metadata = New-Object 'System.Collections.Generic.Dictionary[String,String]'
        $metadata.Add("CreatedBy","PowerShell")
        $BuildReq.MetaData = $MetaData

        $buildResult = CallPlayFabApi -ApiName CreateBuildWithManagedContainerAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::CreateBuildWithManagedContainerAsync($BuildReq) }
        return $buildResult
     }

          <# 
.SYNOPSIS 
Creates a game server build. 
 
.DESCRIPTION 
Creates a game server build. Currently hard-coded to create a Windows Server Core build. 
 
.EXAMPLE 
        
$VMSelection = [PlayFab.MultiplayerModels.AzureVMSize]::Standard_D2_v2 
 
$Ports = New-object PlayFab.MultiplayerModels.Port 
$Ports.Name = "Test Port" 
$Ports.Num = 3600 
$Ports.Protocol = [PlayFab.MultiplayerModels.ProtocolType]::TCP
 
$BuildCert = New-Object System.Collections.Generic.List[PlayFab.MultiplayerModels.GameCertificateReferenceParams]
$BuildCertParams = New-Object PlayFab.MultiplayerModels.GameCertificateReferenceParams
$BuildCertParams.Name = "FakeCert"
$BuildCertParams.GsdkAlias = "FakeCert"

$BuildCert.Add($BuildCertParams)
 
New-PFMultiplayerBuild -BuildName "PowerShellTest902" -AssetFileName "winrunnerasset_notimeout.zip" -AssetMountPath "C:\Assets\" -StartMultiplayerServerCommand "C:\Assets\WinTestRunnerGame.exe" -MappedPorts $Ports -VMSize $VMSelection -BuildCerts $BuildCert 
 
#>

}

function Remove-PFMultiplayerBuild
{
    [CmdletBinding(DefaultParameterSetName="BuildName")]
    Param(
    [Parameter(Mandatory = $true, ParameterSetName="BuildName", Position=0)][string]$BuildName,
    [Parameter(Mandatory = $true, ParameterSetName="BuildID")][string]$BuildID

    )

    Begin
    {}

    Process
    {
        if($PSCmdlet.ParameterSetName -eq "BuildName")
        {   
            $BuildID = Get-PFMultiplayerBuild -BuildName $BuildName
            $BuildID = $BuildID.BuildID
        }

        $BuildReq = NEW-OBJECT PlayFab.MultiplayerModels.DeleteBuildRequest
        $BuildReq.BuildId = $BuildId

        $result = CallPlayFabApi -ApiName DeleteBuildAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::DeleteBuildAsync($BuildReq) }
        return $result.BuildSummary
     }

          <# 
.SYNOPSIS 
Deletes a game server build. 
 
.DESCRIPTION 
Deletes a game server build. 
 
.EXAMPLE 
 
Remove-PFMultiplayerBuild -BuildId <build_id> 
 
#>

}

function Get-PFMultiplayerServer
{
    [CmdletBinding(DefaultParameterSetName="AllRegions")]
    Param(   
    [Parameter(Mandatory = $true)][string]$BuildName,
    [Parameter(ParameterSetName = "SpecificRegion", Mandatory = $True)][PlayFab.MultiplayerModels.AzureRegion]$Region,
    [Parameter(ParameterSetName = "AllRegions")][Switch] $AllRegions
    )
    
    Begin
    {}

    Process
    {
        $Build = Get-PFMultiplayerBuild -BuildName $BuildName
        $BuildID = $BuildID.BuildID

        $RegionList = New-Object 'System.Collections.Generic.List[PlayFab.MultiplayerModels.AzureRegion]'
         

        if($PSCmdlet.ParameterSetName -eq "AllRegions")
        {
            $RegionList = $Build.
            $regions = [PlayFab.MultiplayerModels.AzureRegion[]] @("EastUs", "WestUs", "SouthCentralUs", "NorthEurope", "WestEurope")
            $RegionList.AddRange($regions)
        }
        else
        {
            $RegionList.Add($Region)
        }

        $result = New-Object System.Collections.ArrayList
        foreach ($region in $RegionList)
        {
            $SessionReq = NEW-OBJECT PlayFab.MultiplayerModels.ListMultiplayerServersRequest
            $SessionReq.BuildID = $BuildID
            $SessionReq.Region =  $region

            $apiResult = CallPlayFabApi -ApiName ListMultiplayerServersAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::ListMultiplayerServersAsync($SessionReq) } -WarnIfPaged
            $unsortedresult = $apiResult.MultiplayerServerSummaries
            $apiResult = $unsortedresult | Sort-Object region, state
            $result.AddRange($apiResult)
        }

        return $result;
     }

     <# 
.SYNOPSIS 
 
Gets the servers for a build 
 
.DESCRIPTION 
 
Gets the servers for a build and specified region 
 
.Example 
 
Get servers from a specific build in East US 
 
$Region = [PlayFab.MultiplayerModels.AzureRegion]::EastUS 
$Name = "ZIP_AllocationTest" 
Get-PFMultiplayerServer -BuildName $Name -Region $Region 
 
.Example 
Get servers from a specific build across all regions 
 
 
$Name = "ZIP_AllocationTest" 
Get-PFMultiplayerServer -BuildName $Name -AllRegions 
 
#>
}

function New-PFMultiplayerServer
{
    [CmdletBinding( )]
    Param(
    [Parameter(Mandatory = $true)][string]$BuildName,
    [Parameter(Mandatory = $true)][string]$SessionId,
    [Parameter(Mandatory = $true)][string]$SessionCookie,
    [Parameter(Mandatory = $true)][System.Collections.Generic.List[PlayFab.MultiplayerModels.AzureRegion]]$PreferredRegions
    )

    Begin
    {}

    Process
    {
        $AllocationReq = NEW-OBJECT PlayFab.MultiplayerModels.RequestMultiplayerServerRequest
        
        $BuildID = Get-PFMultiplayerBuild -BuildName $BuildName
        $BuildID = $BuildID.BuildID

        
        $AllocationReq.BuildId = $BuildId
        $AllocationReq.SessionId = $SessionId
        $AllocationReq.SessionCookie = $SessionCookie
        $AllocationReq.PreferredRegions = $PreferredRegions

        return CallPlayFabApi -ApiName RequestMultiplayerServerAsync -ApiCall { [PlayFab.PlayFabMultiplayerAPI]::RequestMultiplayerServerAsync($AllocationReq) }
    }


     <# 
.SYNOPSIS 
 
Allocates a new multiplayer server
 
.DESCRIPTION 
 
Allocates a new multiplayer server using the specified build and region preferences. 
 
.Example 
 
$regions = new-object 'System.Collections.Generic.List[PlayFab.MultiplayerModels.AzureRegion]'
$regions.Add("EastUS");

New-PFMultiplayerServer -BuildName "MyBuild" -SessionId "00000000-0000-0000-0000-000000000001" -SessionCookie "test cookie" -PreferredRegions $regions

#>
}


##Export cmdlet and alias to module

New-Alias Get-MPQosServer Get-PFMultiplayerQosServer
Export-ModuleMember -Alias Get-MPQosServer -Function Get-PFMultiplayerQosServer

New-Alias Get-PFToken Get-PFTitleEntityToken
Export-ModuleMember -Alias Get-PFToken -Function Get-PFTitleEntityToken

New-Alias Get-MPAsset Get-PFMultiplayerAsset
Export-ModuleMember -Alias Get-MPAsset -Function Get-PFMultiplayerAsset



New-Alias Remove-MPAsset Remove-PFMultiplayerAsset
Export-ModuleMember -Alias Remove-MPAsset -Function Remove-PFMultiplayerAsset

New-Alias Add-MPAsset Add-PFMultiplayerAsset
Export-ModuleMember -Alias Add-MPAsset -Function Add-PFMultiplayerAsset

New-Alias Add-MPCertificate Add-PFMultiplayerCertificate
Export-ModuleMember -Alias Add-MPCertificate -Function Add-PFMultiplayerCertificate

New-Alias Get-MPCertificate Get-PFMultiplayerCertificate
Export-ModuleMember -Alias Get-MPCertificate -Function Get-PFMultiplayerCertificate

New-Alias New-MPBuild New-PFMultiplayerBuild
Export-ModuleMember -Alias New-MPBuild -Function New-PFMultiplayerBuild


New-Alias Remove-MPBuild Remove-PFMultiplayerBuild
Export-ModuleMember -Alias Remove-MPBuild -Function Remove-PFMultiplayerBuild

New-Alias Get-MPBuild Get-PFMultiplayerBuild
Export-ModuleMember -Alias Get-MPBuild -Function Get-PFMultiplayerBuild

New-Alias Get-MPServer Get-PFMultiplayerServer
Export-ModuleMember -Alias Get-MPServer -Function Get-PFMultiplayerServer

New-Alias New-MPServer New-PFMultiplayerServer
Export-ModuleMember -Alias New-MPServer -Function New-PFMultiplayerServer

New-Alias Enable-MP Enable-PFMultiplayerServer
Export-ModuleMember -Alias Enable-MP -Function Enable-PFMultiplayerServer

 