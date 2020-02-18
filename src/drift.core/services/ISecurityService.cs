// -----------------------------------------------------------------------
// <copyright file="IGraphService.cs" company="ALM | DevOps Rangers">
//    This code is licensed under the MIT License.
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
//    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR
//    A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// </copyright>
// -----------------------------------------------------------------------

namespace Rangers.Antidrift.Drift.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISecurityService
    {
        Task<IEnumerable<Namespace>> GetNamespaces(TeamProject teamProject, string applicationGroupDescriptor);
    }
}