// -----------------------------------------------------------------------
// <copyright file="OrganizationTests.cs" company="ALM | DevOps Rangers">
//    This code is licensed under the MIT License.
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
//    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR
//    A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// </copyright>
// -----------------------------------------------------------------------

namespace Rangers.Antidrift.Drift.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class OrganizationTests
    {
        [TestMethod]
        public async Task CollectDeviations()
        {
            // Arrange
            var teamProject1 = new Mock<TeamProject>();
            var teamProject2 = new Mock<TeamProject>();

            var deviation1 = new Deviation();
            var deviation2 = new Deviation();

            teamProject1.Setup(t => t.CollectDeviations()).ReturnsAsync(new List<Deviation> { deviation1 });
            teamProject2.Setup(t => t.CollectDeviations()).ReturnsAsync(new List<Deviation> { deviation2 });

            var target = new Organization(Mock.Of<IProjectService>());
            target.TeamProjects.Add(teamProject1.Object);
            target.TeamProjects.Add(teamProject2.Object);

            // Act
            var actual = await target.CollectDeviations().ConfigureAwait(false);

            // Assert
            teamProject1.VerifyAll();
            teamProject2.VerifyAll();

            actual.Should()
                  .Contain(deviation1)
                  .And
                  .Contain(deviation2);

            await Task.CompletedTask.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CollectDeviations_MissingProjects()
        {
            // Arrange
            var teamProject1 = new TeamProject { Id = Guid.NewGuid(), Name = "Expected TeamProject" };
            var teamProject2 = new TeamProject { Id = Guid.NewGuid(), Name = "Extra TeamProject" };

            var projectService = new Mock<IProjectService>();

            projectService.Setup(s => s.GetProjects()).ReturnsAsync(new List<TeamProject> { teamProject2 });

            var target = new Organization(projectService.Object);
            target.TeamProjects.Add(teamProject1);

            // Act
            var actual = await target.CollectDeviations().ConfigureAwait(false);

            // Assert
            actual
                .Should()
                .SatisfyRespectively(
                    first =>
                    {
                        first.Should().BeOfType<TeamProjectDeviation>();
                        ((TeamProjectDeviation)first).TeamProject.Should().Be(teamProject1);
                        ((TeamProjectDeviation)first).Type.Should().Be(DeviationType.Missing);
                    },
                    second =>
                    {
                        second.Should().BeOfType<TeamProjectDeviation>();
                        ((TeamProjectDeviation)second).TeamProject.Should().Be(teamProject2);
                        ((TeamProjectDeviation)second).Type.Should().Be(DeviationType.Obsolete);
                    });

            await Task.CompletedTask.ConfigureAwait(false);
        }

        [TestMethod]
        public async Task CollectDeviations_MisconfiguredProjects()
        {
            // Arrange
            var teamProjectId = Guid.NewGuid();
            var teamProject1 = new TeamProject { Id = teamProjectId, Name = "Expected name" };
            var teamProject2 = new TeamProject { Id = teamProjectId, Name = "Actual name" };

            var projectService = new Mock<IProjectService>();

            projectService.Setup(s => s.GetProjects()).ReturnsAsync(new List<TeamProject> { teamProject2 });

            var target = new Organization(projectService.Object);
            target.TeamProjects.Add(teamProject1);

            // Act
            var actual = await target.CollectDeviations().ConfigureAwait(false);

            // Assert
            actual
                .Should()
                .SatisfyRespectively(
                    first =>
                    {
                        first.Should().BeOfType<TeamProjectDeviation>();
                        ((TeamProjectDeviation)first).TeamProject.Should().Be(teamProject1);
                        ((TeamProjectDeviation)first).Type.Should().Be(DeviationType.Incorrect);
                    });

            await Task.CompletedTask.ConfigureAwait(false);
        }

        [TestMethod]
        public void Expand()
        {
            var graphService = new Mock<IGraphService>();

            var applicationGroup = new ApplicationGroup { Name = "[{teamProject.Name}]\\Project Administrators" };
            var pattern = new SecurityPattern(graphService.Object) { Name = "Test" };
            pattern.ApplicationGroups.Add(applicationGroup);

            var teamProject = new TeamProject { Name = "Test", Key = "1" };
            teamProject.Patterns.Add(new SecurityPattern(graphService.Object) { Name = "Test" });

            var target = new Organization(Mock.Of<IProjectService>());
            target.Mappings.Add("1", Guid.NewGuid());
            target.Patterns.Add(pattern);
            target.TeamProjects.Add(teamProject);

            target.Expand();

            ((SecurityPattern)teamProject.Patterns[0]).ApplicationGroups[0].Name.Should().Be("[Test]\\Project Administrators");
        }
    }
}
