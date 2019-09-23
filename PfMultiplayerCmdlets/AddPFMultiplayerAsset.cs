namespace PFMultiplayerCmdlets
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Management.Automation;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Add, "PFMultiplayerAsset")]
    public class AddPFMultiplayerAsset : PFBaseCmdlet
    {
        [Parameter(Mandatory = true)]
        [ValidatePattern(".*\\.(zip|tar\\.gz|tar)")]
        public string FilePath { get; set; }

        [Parameter]
        [ValidatePattern(".*\\.(zip|tar\\.gz|tar)")]
        public string AssetName { get; set; }

        [Parameter]
        public IDictionary<string, string> Metadata { get; set; }

        protected override void ProcessRecord()
        {
            //cd in powershell does not set the Path.CurrentDirectory to the specified path. 
            //Therefore, to resolve relative paths we need to find the current file system location (which gets updated by cd) and combine it with the relative path
            FilePath = Path.IsPathRooted(FilePath) ? FilePath : Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, FilePath);

            if (!File.Exists(FilePath))
            {
                throw new Exception($"File {FilePath} does not exist");
            }

            GetAssetUploadUrlResponse response = CallPlayFabApi(() => Instance
                .GetAssetUploadUrlAsync(new GetAssetUploadUrlRequest {FileName = AssetName ?? Path.GetFileName(FilePath)}));

            WriteVerbose($"SasToken retrieved {response.AssetUploadUrl}, uploading file.");
            var blob = new CloudBlockBlob(new Uri(response.AssetUploadUrl));
            if (Metadata?.Count > 0)
            {
                foreach (KeyValuePair<string, string> kvPair in Metadata)
                {
                    blob.Metadata.Add(kvPair);
                }

                blob.SetMetadata();
            }

            blob.UploadFromFile(FilePath, null, new BlobRequestOptions {RetryPolicy = new ExponentialRetry()});
            WriteVerbose($"Completed adding asset {FilePath}.");
        }
    }
}