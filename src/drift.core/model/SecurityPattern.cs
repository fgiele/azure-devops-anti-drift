// -----------------------------------------------------------------------
// <copyright file="SecurityPattern.cs" company="ALM | DevOps Rangers">
//    This code is licensed under the MIT License.
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
//    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR
//    A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// </copyright>
// -----------------------------------------------------------------------

namespace Rangers.Antidrift.Drift.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class SecurityPattern : Pattern
    {
        private readonly IGraphService graphService;
        private readonly ISecurityService securityService;

        public SecurityPattern(IGraphService graphService, ISecurityService securityService)
        {
            this.graphService = graphService;
            this.securityService = securityService;
        }

        public IList<ApplicationGroup> ApplicationGroups { get; set; } = new List<ApplicationGroup>();

        public async override Task<IEnumerable<Deviation>> CollectDeviations(TeamProject teamProject)
        {
            var results = (await base.CollectDeviations(teamProject).ConfigureAwait(false)).ToList();
            var currentApplicationGroups = await this.graphService.GetApplicationGroups(teamProject).ConfigureAwait(false);

            // Check if the application group exists
            var missingApplicationGroupDeviations = this.ApplicationGroups
                .Where(ag => currentApplicationGroups.All(cag => !cag.Name.Equals(ag.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(ag => new ApplicationGroupDeviation { ApplicationGroup = ag, TeamProject = teamProject, Type = DeviationType.Missing })
                .ToList();

            // Check for obsolete application groups.
            var obsoleteApplicationGroupDeviations = currentApplicationGroups
                .Where(cag => !cag.IsSpecial)
                .Where(cag => this.ApplicationGroups.All(ag => !ag.Name.Equals(cag.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(cag => new ApplicationGroupDeviation { ApplicationGroup = cag, TeamProject = teamProject, Type = DeviationType.Obsolete })
                .ToList();

            // Only need to check additional information on those applicationgroups that match
            var matchingGroups = this.ApplicationGroups
                .Where(ag => currentApplicationGroups.Select(cag => cag.Name).Contains(ag.Name, StringComparer.OrdinalIgnoreCase));

            foreach (var applicationGroup in matchingGroups)
            {
                // Set the descriptor of the applicationgroup
                applicationGroup.Descriptor = currentApplicationGroups.Single(cag => cag.Name.Equals(applicationGroup.Name, StringComparison.OrdinalIgnoreCase)).Descriptor;

                var currentMembers = currentApplicationGroups.Any(ag => ag.Name.Equals(applicationGroup.Name, StringComparison.OrdinalIgnoreCase))
                                     ? (await this.graphService.GetMembers(teamProject, applicationGroup).ConfigureAwait(false))
                                     : new List<string>();

                // Check if the application group contains the correct members
                var missingApplicationGroupMemberDeviations = applicationGroup.Members
                    .Where(member => currentMembers.All(cm => !cm.Equals(member, StringComparison.OrdinalIgnoreCase)))
                    .Select(m => new ApplicationGroupMemberDeviation { ApplicationGroup = applicationGroup, Member = m, TeamProject = teamProject, Type = DeviationType.Missing })
                    .ToList();

                // Check for obsolete members
                var obsoleteApplictionGroupMemberDeviations = currentMembers
                    .Where(cm => applicationGroup.Members.All(m => !m.Equals(cm, StringComparison.OrdinalIgnoreCase)))
                    .Select(cm => new ApplicationGroupMemberDeviation { ApplicationGroup = applicationGroup, Member = cm, TeamProject = teamProject, Type = DeviationType.Obsolete })
                    .ToList();

                results.AddRange(missingApplicationGroupMemberDeviations);
                results.AddRange(obsoleteApplictionGroupMemberDeviations);

                if (!applicationGroup.IsSpecial)
                {
                    results.AddRange(await this.CollectApplicationGroupDeviations(applicationGroup, teamProject).ConfigureAwait(false));
                }
            }

            results.AddRange(missingApplicationGroupDeviations);
            results.AddRange(obsoleteApplicationGroupDeviations);

            return results;
        }

        private async Task<IEnumerable<Deviation>> CollectApplicationGroupDeviations(ApplicationGroup applicationGroup, TeamProject teamProject)
        {
            var results = new List<Deviation>();

            var currentNamespaces = await this.securityService.GetNamespaces(teamProject, applicationGroup).ConfigureAwait(false);

            // Check if namespaces are all present
            var missingNamespaces = applicationGroup.Namespaces
                .Where(ns => currentNamespaces.All(cns => !cns.Name.Equals(ns.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(ns => new NamespaceDeviation { Name = ns.Name, ApplicationGroup = applicationGroup, TeamProject = teamProject, Type = DeviationType.Missing })
                .ToList();

            // Check for obsolete namespaces.
            var obsoleteNamespaces = currentNamespaces
                .Where(cns => applicationGroup.Namespaces.All(ns => !ns.Name.Equals(cns.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(cns => new NamespaceDeviation { Name = cns.Name, ApplicationGroup = applicationGroup, TeamProject = teamProject, Type = DeviationType.Obsolete })
                .ToList();

            // Only need to check additional information on those namespaces that match
            var matchingNamespaces = applicationGroup.Namespaces
                .Where(ns => currentNamespaces.Select(cns => cns.Name).Contains(ns.Name, StringComparer.OrdinalIgnoreCase));

            foreach (var namesp in matchingNamespaces)
            {
                var currentAllowList = currentNamespaces.SingleOrDefault(cns => cns.Name.Equals(namesp.Name, StringComparison.OrdinalIgnoreCase))?.Allow ?? Array.Empty<string>();
                var currentDenyList = currentNamespaces.SingleOrDefault(cns => cns.Name.Equals(namesp.Name, StringComparison.OrdinalIgnoreCase))?.Deny ?? Array.Empty<string>();

                // Check for missing allow permissions
                var missingAllowPermissions = namesp.Allow
                    .Where(al => !currentAllowList.Contains(al, StringComparer.OrdinalIgnoreCase))
                    .Select(al => new NamespacePermissionDeviation
                    {
                        ApplicationGroup = applicationGroup,
                        AutorizationType = NamespaceAutorization.Allow,
                        Namespace = namesp,
                        Permission = al,
                        TeamProject = teamProject,
                        Type = DeviationType.Missing,
                    })
                    .ToList();

                // Check for obsolete allow permissions
                var obsoleteAllowPermissions = currentAllowList
                    .Where(cal => !namesp.Allow.Contains(cal, StringComparer.OrdinalIgnoreCase))
                    .Select(cal => new NamespacePermissionDeviation
                    {
                        ApplicationGroup = applicationGroup,
                        AutorizationType = NamespaceAutorization.Allow,
                        Namespace = namesp,
                        Permission = cal,
                        TeamProject = teamProject,
                        Type = DeviationType.Obsolete,
                    })
                    .ToList();

                // Check for missing deny permissions
                var missingDenyPermissions = namesp.Deny
                    .Where(dn => !currentDenyList.Contains(dn, StringComparer.OrdinalIgnoreCase))
                    .Select(dn => new NamespacePermissionDeviation
                    {
                        ApplicationGroup = applicationGroup,
                        AutorizationType = NamespaceAutorization.Deny,
                        Namespace = namesp,
                        Permission = dn,
                        TeamProject = teamProject,
                        Type = DeviationType.Missing,
                    })
                    .ToList();

                // Check for obsolete deny permissions
                var obsoleteDenyPermissions = currentDenyList
                    .Where(cdn => !namesp.Deny.Contains(cdn, StringComparer.OrdinalIgnoreCase))
                    .Select(cdn => new NamespacePermissionDeviation
                    {
                        ApplicationGroup = applicationGroup,
                        AutorizationType = NamespaceAutorization.Deny,
                        Namespace = namesp,
                        Permission = cdn,
                        TeamProject = teamProject,
                        Type = DeviationType.Obsolete,
                    })
                    .ToList();

                results.AddRange(missingAllowPermissions);
                results.AddRange(obsoleteAllowPermissions);
                results.AddRange(missingDenyPermissions);
                results.AddRange(obsoleteDenyPermissions);
            }

            results.AddRange(missingNamespaces);
            results.AddRange(obsoleteNamespaces);

            return results;
        }

        public override Pattern Expand(TeamProject teamProject)
        {
            var result = new SecurityPattern(this.graphService, this.securityService);
            this.Expand(result, teamProject);
            result.ApplicationGroups = this.ApplicationGroups.Select(ag => ag.Expand(teamProject)).ToList();

            return result;
        }
    }
}