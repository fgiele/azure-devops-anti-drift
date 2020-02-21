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
    public class NamespaceDeviation : Deviation
    {
        public Namespace Namespace { get; set; }

        public ApplicationGroup ApplicationGroup { get; set; }

        public DeviationType Type { get; set; }

        public override string ToString()
        {
            return $"Namespace {this.Namespace.Name} is {this.Type} for {this.ApplicationGroup.Name} in Team Project {this.TeamProject.Name}.";
        }
    }
}