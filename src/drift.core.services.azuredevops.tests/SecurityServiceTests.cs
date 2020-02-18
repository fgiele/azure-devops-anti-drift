// -----------------------------------------------------------------------
// <copyright file="GraphServiceTests.cs" company="ALM | DevOps Rangers">
//    This code is licensed under the MIT License.
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
//    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR
//    A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// </copyright>
// -----------------------------------------------------------------------

namespace Rangers.Antidrift.Drift.Core.Services.AzureDevOps.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class SecurityServiceTests
    {
        public TestContext TestContext { get; set; }

        public string Organization
        {
            get { return this.TestContext.Properties["organization"].ToString(); }
        }

        public string PAT
        {
            get { return this.TestContext.Properties["pat"].ToString(); }
        }

        private Guid Antidrift => new Guid(this.TestContext.Properties["antidriftprojectguid"].ToString());

        [TestMethod]
        public async Task GetNameSpaces()
        {
            // Arrange
            var credentials = new VssBasicCredential(string.Empty, this.PAT);
            var url = $"https://dev.azure.com/{this.Organization}";
            var connection = new VssConnection(new Uri(url), credentials);

            var graphService = new GraphService(connection);
            var teamProject = new TeamProject { Id = this.Antidrift, Name = "Antidrift" };

            var applicationGroups = await graphService.GetApplicationGroups(teamProject).ConfigureAwait(false);

            var expected = JsonConvert.DeserializeObject<List<Namespace>>(System.IO.File.ReadAllText("./ContributorNamespaces.json"));

            var target = new SecurityService(connection);

            // Act
            var actual = await target.GetNamespaces(teamProject, applicationGroups.Single(grp => grp.Name == "Contributors")).ConfigureAwait(false);

            // Assert
            actual.Should().BeEquivalentTo(expected);
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "No descriptor available for ApplicationGroup")]
        public async Task GetNameSpacesFailsWhenNoIdentityInApplicationGroup()
        {
            // Arrange
            var credentials = new VssBasicCredential(string.Empty, this.PAT);
            var url = $"https://dev.azure.com/{this.Organization}";
            var connection = new VssConnection(new Uri(url), credentials);

            var teamProject = new TeamProject { Id = this.Antidrift, Name = "Antidrift" };
            var applicationGroup = new ApplicationGroup { Name = "Contributors" };

            var target = new SecurityService(connection);

            // Act
            await target.GetNamespaces(teamProject, applicationGroup).ConfigureAwait(false);

            // Assert
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}