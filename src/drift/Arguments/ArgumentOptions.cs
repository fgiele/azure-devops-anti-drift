﻿// -----------------------------------------------------------------------
// <copyright file="ArgumentOptions.cs" company="ALM | DevOps Rangers">
//    This code is licensed under the MIT License.
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
//    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
//    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR
//    A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// </copyright>
// -----------------------------------------------------------------------

namespace Rangers.Antidrift.Drift.Arguments
{
    using System;
    using System.IO;
    using CommandLine;

    public class ArgumentOptions
    {
        private string configurationFilePath;
        private string serviceUrl;

        [Option(
            'c',
            "config-file",
            Required = true,
            HelpText = "Path to the configuration file.")]
        public string ConfigurationFilePath
        {
            get
            {
                return this.configurationFilePath;
            }

            set
            {
                string fullpath = Path.GetFullPath(value);

                if (!File.Exists(fullpath))
                {
                    throw new FileNotFoundException($"Configuration file was not found at '{fullpath}' ", fullpath);
                }

                this.configurationFilePath = fullpath;
            }
        }

        [Option(
            's',
            "service-url",
            Required = true,
            HelpText = "URL to the service you will connect to, e.g. https://youraccount.visualstudio.com/DefaultCollection")]
        public string ServiceUrl
        {
            get
            {
                return this.serviceUrl;
            }

            set
            {
                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                {
                    throw new ArgumentException($"URL '{value}' is not a valid URL.");
                }

                this.serviceUrl = value;
            }
        }

        [Option(
            'a',
            "auth-type",
            Default = AuthType.Pat,
            HelpText = "Method of authentication")]
        public AuthType AuthType { get; set; }

        [Option(
            't',
            "token",
            Required = true,
            SetName = "pat",
            HelpText = "Personal access token. Valid only if method of authentication is set to PAT.")]
        public string Token { get; set; }

        [Option(
            'u',
            "username",
            Required = true,
            SetName = "basic",
            HelpText = "Username to use for basic authentication. Valid only if method of authentication is set to Basic.")]
        public string Username { get; set; }

        [Option(
            'p',
            "password",
            Required = true,
            SetName = "basic",
            HelpText = "Password to use for basic authentication. Valid only if method of authentication is set to Basic.")]
        public string Password { get; set; }
    }
}
