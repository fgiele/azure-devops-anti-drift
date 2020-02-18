// -----------------------------------------------------------------------
// <copyright file="GraphService.cs" company="ALM | DevOps Rangers">
//    This code is licensed under the MIT License.
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
//    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR
//    A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// </copyright>
// -----------------------------------------------------------------------

namespace Rangers.Antidrift.Drift.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Services.Security.Client;
    using Microsoft.VisualStudio.Services.WebApi;
    using Rangers.Antidrift.Drift.Core;

    public class SecurityService : ISecurityService
    {
        private readonly VssConnection connection;

        public SecurityService(VssConnection connection)
        {
            this.connection = connection;
        }

        public async Task<IEnumerable<Namespace>> GetNamespaces(TeamProject teamProject, ApplicationGroup applicationGroup)
        {
            if (teamProject == null)
            {
                throw new ArgumentNullException(nameof(teamProject));
            }

            if (applicationGroup == null)
            {
                throw new ArgumentNullException(nameof(applicationGroup));
            }

            if (string.IsNullOrWhiteSpace(applicationGroup.Descriptor))
            {
                throw new ArgumentException("No descriptor available for ApplicationGroup", nameof(applicationGroup));
            }

            var client = this.connection.GetClient<SecurityHttpClient>();

            var securityNamespaces = await client.QuerySecurityNamespacesAsync(Guid.Empty).ConfigureAwait(false);

            var namespaces = new HashSet<Namespace>();
            foreach (var securityNamespace in securityNamespaces)
            {
                var allowActions = new HashSet<string>();
                var denyActions = new HashSet<string>();
                var acls = await client.QueryAccessControlListsAsync(securityNamespace.NamespaceId, string.Empty, null, true, false).ConfigureAwait(false);

                var aces = acls.SelectMany(acl => acl.AcesDictionary.ToArray())
                                .Where(entry => entry.Key.Identifier == applicationGroup.Descriptor)
                                .Select(entry => entry.Value);
                var allows = aces.Select(ac => ac.ExtendedInfo.EffectiveAllow).Distinct();
                var denies = aces.Select(ac => ac.ExtendedInfo.EffectiveDeny).Distinct();

                if (allows.Count() == 1 && denies.Count() == 1)
                {
                    var allowList = securityNamespace.Actions.Where(act => (act.Bit & allows.Single()) > 0).Select(act => act.DisplayName).ToArray();
                    var denyList = securityNamespace.Actions.Where(act => (act.Bit & denies.Single()) > 0).Select(act => act.DisplayName).ToArray();

                    namespaces.Add(new Namespace { Name = securityNamespace.Name, Allow = allowList, Deny = denyList });
                }
                else
                {
                    Console.WriteLine($"For {securityNamespace.Name} found Allows: {allows.Count()}  & Denies: {denies.Count()}");
                }
            }

            if (namespaces.Count == 0)
            {
                throw new InvalidOperationException($"Cannot get the namespaces for application group {applicationGroup.Name}. The application group is not available.");
            }

            return namespaces;
        }
    }
}
