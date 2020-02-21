// -----------------------------------------------------------------------
// <copyright file="NamespaceDeviation.cs" company="ALM | DevOps Rangers">
//    This code is licensed under the MIT License.
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
//    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR
//    A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// </copyright>
// -----------------------------------------------------------------------

namespace Rangers.Antidrift.Drift.Core
{
    public class NamespacePermissionDeviation : Deviation
    {
        public enum Autorization
        {
            Allow,
            Deny,
            Unset
        }

        public string Permission { get; set; }

        public Autorization AutorizationType { get; set; }

        public Namespace Namespace { get; set; }

        public ApplicationGroup ApplicationGroup { get; set; }

        public DeviationType Type { get; set; }

        public override string ToString()
        {
            return $"{this.AutorizationType} {this.Permission} is {this.Type} for {this.Namespace.Name} in {this.ApplicationGroup.Name} in Team Project {this.TeamProject.Name}.";
        }
    }
}