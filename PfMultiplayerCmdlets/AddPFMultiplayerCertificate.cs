namespace PFMultiplayerCmdlets
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.Add, "PFMultiplayerCertificate")]
    public class AddPFMultiplayerCertificate : PFBaseCmdlet
    {
        [Parameter(Mandatory = true)]
        public string FilePath { get; set; }

        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        [Parameter]
        public string Password { get; set; }

        protected override void ProcessRecord()
        {
            if (!File.Exists(FilePath))
            {
                throw new Exception($"File {FilePath} does not exist");
            }

            var certBytes = File.ReadAllBytes(FilePath);
            var certBase64 = Convert.ToBase64String(certBytes);

            CallPlayFabApi(() => Instance
                .UploadCertificateAsync(new UploadCertificateRequest
                {
                    GameCertificate = new Certificate {Base64EncodedValue = certBase64, Name = Name, Password = Password}
                }));

            WriteVerbose("$Completed uploading certificate {FilePath}.");
        }
    }
}