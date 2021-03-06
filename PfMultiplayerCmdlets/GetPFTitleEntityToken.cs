﻿namespace PFMultiplayerCmdlets
{
    using System;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get, "PFTitleEntityToken")]
    [Obsolete(@"This cmdlet is deprecated. Use Set-PFTitle instead.")]
    public class GetPFTitleEntityToken : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public string SecretKey { get; set; }

        [Parameter(Mandatory = true)]
        public string TitleId { get; set; }

        protected override void ProcessRecord()
        {
            PFTokenUtility.Instance.SetTitleSecretKey(TitleId, SecretKey);
            PFTokenUtility.Instance.GetPFTitleEntityToken();
        }
    }
}