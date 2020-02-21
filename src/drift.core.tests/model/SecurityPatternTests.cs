// -----------------------------------------------------------------------
// <copyright file="SecurityPatternTests.cs" company="ALM | DevOps Rangers">
//    This code is licensed under the MIT License.
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
//    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR
//    A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// </copyright>
// -----------------------------------------------------------------------

namespace Rangers.Antidrift.Drift.Core.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class SecurityPatternTests
    {
        [TestMethod]
        public async Task CollectDeviations()
        {
            // Arrange
            var applicationGroup = new ApplicationGroup { Name = "ApplicationGroup" };
            var graphService = new Mock<IGraphService>();
            var securityService = new Mock<ISecurityService>();
            var teamProject = new TeamProject();

            graphService.Setup(s => s.GetApplicationGroups(teamProject)).ReturnsAsync(new List<ApplicationGroup>());

            var target = new SecurityPattern(graphService.Object, securityService.Object);
            target.ApplicationGroups.Add(applicationGroup);

            // Act
            var actual = await target.CollectDeviations(teamProject).ConfigureAwait(false);

            // Assert
            actual
                .Should()
                .SatisfyRespectively(
                    first =>
                    {
                        first.Should().BeOfType<ApplicationGroupDeviation>();
                        ((ApplicationGroupDeviation)first).ApplicationGroup.Should().Be(applicationGroup);
                    });

            await Task.CompletedTask;
        }

        [TestMethod]
        public async Task CollectDeviations_MissingMembers()
        {
            // Arrange
            var applicationGroup = new ApplicationGroup { Name = "ApplicationGroup", Members = new[] { "Member 1" } };
            var current = new List<ApplicationGroup> { new ApplicationGroup { Name = "ApplicationGroup" } };
            var graphService = new Mock<IGraphService>();
            var securityService = new Mock<ISecurityService>();
            var teamProject = new TeamProject();

            graphService.Setup(s => s.GetApplicationGroups(teamProject)).ReturnsAsync(current);
            graphService.Setup(s => s.GetMembers(teamProject, applicationGroup)).ReturnsAsync(new List<string> { "Member 2" });

            var target = new SecurityPattern(graphService.Object, securityService.Object);
            target.ApplicationGroups.Add(applicationGroup);

            // Act
            var actual = await target.CollectDeviations(teamProject).ConfigureAwait(false);

            // Assert
            actual
                .Should()
                .SatisfyRespectively(
                    first =>
                    {
                        first.Should().BeOfType<ApplicationGroupMemberDeviation>();
                        ((ApplicationGroupMemberDeviation)first).ApplicationGroup.Should().Be(applicationGroup);
                        ((ApplicationGroupMemberDeviation)first).Member.Should().Be("Member 1");
                        ((ApplicationGroupMemberDeviation)first).Type.Should().Be(DeviationType.Missing);
                    },
                    second =>
                    {
                        second.Should().BeOfType<ApplicationGroupMemberDeviation>();
                        ((ApplicationGroupMemberDeviation)second).ApplicationGroup.Should().Be(applicationGroup);
                        ((ApplicationGroupMemberDeviation)second).Member.Should().Be("Member 2");
                        ((ApplicationGroupMemberDeviation)second).Type.Should().Be(DeviationType.Obsolete);
                    });

            await Task.CompletedTask.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CollectDeviations_MissingNamespaces()
        {
            // Arrange
            var members = new[] { "Member 1" };
            var applicationGroup = new ApplicationGroup
            {
                Name = "ApplicationGroup 1",
                Members = members,
                Namespaces = new[]
                {
                    new Namespace
                    {
                        Name = "Namespace 1",
                        Allow = new[] { "Allow 1", "Allow 2" },
                        Deny = new[] { "Deny 1", "Deny 2"},
                    },
                },
            };
            var currentApplicationGroup = new List<ApplicationGroup>
            {
                new ApplicationGroup
                {
                    Name = "ApplicationGroup 1",
                    Members = members,
                },
            };
            var currentNamespace = new Namespace
            {
                Name = "Namespace 2",
                Allow = new[] { "Allow 2", "Allow 3" },
                Deny = new[] { "Deny 2", "Deny 3" },
            };

            var graphService = new Mock<IGraphService>();
            var securityService = new Mock<ISecurityService>();
            var teamProject = new TeamProject();

            graphService.Setup(s => s.GetApplicationGroups(teamProject)).ReturnsAsync(currentApplicationGroup);
            graphService.Setup(s => s.GetMembers(teamProject, applicationGroup)).ReturnsAsync(members);
            securityService.Setup(s => s.GetNamespaces(teamProject, applicationGroup)).ReturnsAsync(new[] { currentNamespace });

            var target = new SecurityPattern(graphService.Object, securityService.Object);
            target.ApplicationGroups.Add(applicationGroup);

            // Act
            var actual = await target.CollectDeviations(teamProject).ConfigureAwait(false);

            // Assert
            actual
                .Should()
                .SatisfyRespectively(
                    first =>
                    {
                        first.Should().BeOfType<NamespaceDeviation>();
                        ((NamespaceDeviation)first).Namespace.Should().Be(applicationGroup.Namespaces.First());
                        ((NamespaceDeviation)first).ApplicationGroup.Should().Be(applicationGroup);
                        ((NamespaceDeviation)first).Type.Should().Be(DeviationType.Missing);
                    },
                    second =>
                    {
                        second.Should().BeOfType<NamespaceDeviation>();
                        ((NamespaceDeviation)second).Namespace.Should().Be(currentNamespace);
                        ((NamespaceDeviation)second).ApplicationGroup.Should().Be(applicationGroup);
                        ((NamespaceDeviation)second).Type.Should().Be(DeviationType.Obsolete);
                    });

            await Task.CompletedTask.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CollectDeviations_MissingNamespacePermissions()
        {
            // Arrange
            var members = new[] { "Member 1" };
            var applicationGroup = new ApplicationGroup
            {
                Name = "ApplicationGroup",
                Members = members,
                Namespaces = new[]
                {
                    new Namespace
                    {
                        Name = "Namespace 1",
                        Allow = new[] { "Allow 1", "Allow 2" },
                        Deny = new[] { "Deny 1", "Deny 2"},
                    },
                },
            };
            var currentApplicationGroup = new List<ApplicationGroup>
            {
                new ApplicationGroup
                {
                    Name = "ApplicationGroup",
                    Members = members,
                },
            };
            var currentNamespace = new Namespace
            {
                Name = "Namespace 1",
                Allow = new[] { "Allow 2", "Allow 3" },
                Deny = new[] { "Deny 2", "Deny 3" },
            };

            var graphService = new Mock<IGraphService>();
            var securityService = new Mock<ISecurityService>();
            var teamProject = new TeamProject();

            graphService.Setup(s => s.GetApplicationGroups(teamProject)).ReturnsAsync(currentApplicationGroup);
            graphService.Setup(s => s.GetMembers(teamProject, applicationGroup)).ReturnsAsync(members);
            securityService.Setup(s => s.GetNamespaces(teamProject, applicationGroup)).ReturnsAsync(new[] { currentNamespace });

            var target = new SecurityPattern(graphService.Object, securityService.Object);
            target.ApplicationGroups.Add(applicationGroup);

            // Act
            var actual = await target.CollectDeviations(teamProject).ConfigureAwait(false);

            // Assert
            actual
                .Should()
                .SatisfyRespectively(
                    first =>
                    {
                        first.Should().BeOfType<NamespacePermissionDeviation>();
                        ((NamespacePermissionDeviation)first).Namespace.Should().Be(applicationGroup.Namespaces.First());
                        ((NamespacePermissionDeviation)first).ApplicationGroup.Should().Be(applicationGroup);
                        ((NamespacePermissionDeviation)first).AutorizationType.Should().Be(NamespacePermissionDeviation.Autorization.Allow);
                        ((NamespacePermissionDeviation)first).Permission.Should().Be("Allow 1");
                        ((NamespacePermissionDeviation)first).Type.Should().Be(DeviationType.Missing);
                    },
                    second =>
                    {
                        second.Should().BeOfType<NamespacePermissionDeviation>();
                        ((NamespacePermissionDeviation)second).Namespace.Should().Be(applicationGroup.Namespaces.First());
                        ((NamespacePermissionDeviation)second).ApplicationGroup.Should().Be(applicationGroup);
                        ((NamespacePermissionDeviation)second).AutorizationType.Should().Be(NamespacePermissionDeviation.Autorization.Allow);
                        ((NamespacePermissionDeviation)second).Permission.Should().Be("Allow 3");
                        ((NamespacePermissionDeviation)second).Type.Should().Be(DeviationType.Obsolete);
                    },
                    third =>
                    {
                        third.Should().BeOfType<NamespacePermissionDeviation>();
                        ((NamespacePermissionDeviation)third).Namespace.Should().Be(applicationGroup.Namespaces.First());
                        ((NamespacePermissionDeviation)third).ApplicationGroup.Should().Be(applicationGroup);
                        ((NamespacePermissionDeviation)third).AutorizationType.Should().Be(NamespacePermissionDeviation.Autorization.Deny);
                        ((NamespacePermissionDeviation)third).Permission.Should().Be("Deny 1");
                        ((NamespacePermissionDeviation)third).Type.Should().Be(DeviationType.Missing);
                    },
                    fourth =>
                    {
                        fourth.Should().BeOfType<NamespacePermissionDeviation>();
                        ((NamespacePermissionDeviation)fourth).Namespace.Should().Be(applicationGroup.Namespaces.First());
                        ((NamespacePermissionDeviation)fourth).ApplicationGroup.Should().Be(applicationGroup);
                        ((NamespacePermissionDeviation)fourth).AutorizationType.Should().Be(NamespacePermissionDeviation.Autorization.Deny);
                        ((NamespacePermissionDeviation)fourth).Permission.Should().Be("Deny 3");
                        ((NamespacePermissionDeviation)fourth).Type.Should().Be(DeviationType.Obsolete);
                    });

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}