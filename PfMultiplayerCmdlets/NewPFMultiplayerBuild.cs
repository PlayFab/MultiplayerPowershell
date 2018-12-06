namespace PFMultiplayerCmdlets
{
    using System.Collections.Generic;
    using System.Management.Automation;
    using PlayFab;
    using PlayFab.MultiplayerModels;

    [Cmdlet(VerbsCommon.New, "PFMultiplayerBuild")]
    public class NewPFMultiplayerBuild : PFBaseCmdlet
    {
        [Parameter(Mandatory = true)]
        public string BuildName { get; set; }

        [Parameter(Mandatory = true)]
        public string StartMultiplayerServerCommand { get; set; }

        [Parameter]
        public Dictionary<string, string> Metadata { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public List<Port> Ports { get; set; }

        [Parameter(Mandatory = true)]
        public int MultiplayerServerCountPerVm { get; set; } = 1;

        [Parameter(Mandatory = true)]
        public AzureVmSize? VmSize { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public List<BuildRegionParams> RegionConfiguration { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public List<AssetReferenceParams> AssetReferences { get; set; }

        [Parameter]
        public List<GameCertificateReferenceParams> CertificateReferences { get; set; }

        protected override void ProcessRecord()
        {
            CreateBuildWithManagedContainerRequest buildRequest = new CreateBuildWithManagedContainerRequest()
            {
                BuildName = BuildName,
                ContainerFlavor = ContainerFlavor.ManagedWindowsServerCore,
                GameAssetReferences = AssetReferences,
                GameCertificateReferences = CertificateReferences,
                Metadata = Metadata,
                Ports = Ports,
                MultiplayerServerCountPerVm = MultiplayerServerCountPerVm,
                StartMultiplayerServerCommand = StartMultiplayerServerCommand,
                VmSize = VmSize,
                RegionConfigurations = RegionConfiguration
            };

            PlayFabMultiplayerAPI.CreateBuildWithManagedContainerAsync(buildRequest).Wait();

            WriteObject($"Created build {BuildName}");
        }
    }
}