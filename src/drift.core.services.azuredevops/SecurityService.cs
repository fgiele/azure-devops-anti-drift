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
    using Microsoft.VisualStudio.Services.Security;
    using Microsoft.VisualStudio.Services.Security.Client;
    using Microsoft.VisualStudio.Services.WebApi;
    using Rangers.Antidrift.Drift.Core;

    public class SecurityService : ISecurityService
    {
        private readonly SecurityHttpClient client;

        public SecurityService(VssConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            this.client = connection.GetClient<SecurityHttpClient>();
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
                throw new ArgumentNullException(nameof(applicationGroup.Descriptor));
            }

            var securityNamespaces = await this.client.QuerySecurityNamespacesAsync(Guid.Empty).ConfigureAwait(false);

            var namespaceTasks = new HashSet<Task<Namespace>>();

            foreach (var securityNamespace in securityNamespaces)
            {
                namespaceTasks.Add(this.GetControlList(securityNamespace, applicationGroup.Descriptor));
            }

            var namespaces = (await Task.WhenAll(namespaceTasks).ConfigureAwait(false)).Where(ns => ns != null);

            if (!namespaces.Any())
            {
                throw new InvalidOperationException($"Cannot get the namespaces for application group {applicationGroup.Name}. The application group is not available.");
            }

            return namespaces;
        }

        private async Task<Namespace> GetControlList(SecurityNamespaceDescription securityNamespace, string applicationGroupDescriptor)
        {
            var acls = await this.client.QueryAccessControlListsAsync(securityNamespace.NamespaceId, string.Empty, null, true, false).ConfigureAwait(false);

            var aces = acls.SelectMany(acl => acl.AcesDictionary.ToArray())
                            .Where(entry => entry.Key.Identifier == applicationGroupDescriptor)
                            .Select(entry => entry.Value);
            var allows = aces.Select(ac => ac.ExtendedInfo.EffectiveAllow).Distinct();
            var denies = aces.Select(ac => ac.ExtendedInfo.EffectiveDeny).Distinct();

            if (allows.Count() == 1 && denies.Count() == 1)
            {
                var allowList = securityNamespace.Actions.Where(act => (act.Bit & allows.Single()) > 0).Select(act => act.DisplayName).ToArray();
                var denyList = securityNamespace.Actions.Where(act => (act.Bit & denies.Single()) > 0).Select(act => act.DisplayName).ToArray();

                return new Namespace { Name = securityNamespace.Name, Allow = allowList, Deny = denyList };
            }
            else
            {
                Console.WriteLine($"For {securityNamespace.Name} found Allows: {allows.Count()}  & Denies: {denies.Count()}");
                return null;
            }
        }
    }
}
