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

        public SecurityPattern(IGraphService graphService)
        {
            this.graphService = graphService;
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
                .Select(ag => new ApplicationGroupDeviation { ApplicationGroup = ag, TeamProject = teamProject, Type = DeviationType.Obsolete })
                .ToList();

            foreach (var applicationGroup in this.ApplicationGroups)
            {
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

                // TODO: Iterate through the namespaces and check the permissions
            }

            results.AddRange(missingApplicationGroupDeviations);
            results.AddRange(obsoleteApplicationGroupDeviations);

            return results;
        }

        public override Pattern Expand(TeamProject teamProject)
        {
            var result = new SecurityPattern(this.graphService);
            this.Expand(result, teamProject);
            result.ApplicationGroups = this.ApplicationGroups.Select(ag => ag.Expand(teamProject)).ToList();

            return result;
        }
    }
}