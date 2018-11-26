namespace PFMultiplayerCmdlets
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Add, "PFMultiplayerAsset")]
    public class AddPFMultiplayerAsset : PFBaseCmdlet
    {
        [Parameter(Mandatory = true)]
        public string FilePath { get; set; }

        protected override void ProcessRecord()
        {
            if (!File.Exists(FilePath))
            {
                ThrowTerminatingError(new ErrorRecord(new Exception($"File {FilePath} does not exist"), "FileDoesNotExist", ErrorCategory.ObjectNotFound,
                    null));
            }

            var response = PlayFabMultiplayerAPI
                .GetAssetUploadUrlAsync(new GetAssetUploadUrlRequest {FileName = Path.GetFileName(FilePath)}).Result.Result;
            WriteObject("SasToken retrieved, uploading file.");
            var blob = new CloudBlockBlob(new Uri(response.AssetUploadUrl));
            blob.UploadFromFile(FilePath, null, new BlobRequestOptions {RetryPolicy = new ExponentialRetry()});
            WriteObject($"Completed adding asset {FilePath}.");
        }
    }
}