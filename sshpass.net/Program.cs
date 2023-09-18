/*  This file is part of "sshpass.net", a dotnet tool for batch running
 *  password ssh authentication
 *  
 *  Author:
 *    Chris Burke <chris@computercodeblue.com>
 *    
 *  Copyright (C) 2023 Computer Code Blue LLC
 *  
 *  Based on "sshpass"
 *  Copyright (C) 2006 Lingnu Open Source Consulting Ltd.
 *  Copyright (C) 2015-2016, 2021 Shachar Shemesh
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>
 *  
 *  This program uses the Mono.Options library for command line parsing and
 *  the Renci.SshNet library for sending authentication and commands over ssh.
 *  
 */

using Mono.Options;
using Renci.SshNet;
using System.Reflection;

namespace sshpass.net
{
    public class Program
    {
        static void Main(string[] args)
        {
            int errors = 0;
            Arguments arguments = new Arguments();
            var p = new OptionSet()
            {
                {
                    "f|filename=", "Take password to use from file.", v =>
                    {
                        arguments.FileName = v;
                        arguments.PasswordType = PasswordType.File;
                    }
                },
                {
                    "k|key:", "Use RSA private key file. Defaults to current user's id_rsa file.", v =>
                    {
                        arguments.FileName = v;
                        arguments.PasswordType = PasswordType.Key;
                    }
                },
                {
                    "p|password=", "Provide password as argument (security unwise).", v =>
                    {
                        arguments.Password = v;
                        arguments.PasswordType = PasswordType.Password;
                    }
                },
                {
                    "e|envvar:", "Password is passed as env-var if given, \"SSHPASS\" otherwise.", v =>
                    {
                        if (string.IsNullOrEmpty(v)) v = "SSHPASS";
                        arguments.Password = Environment.GetEnvironmentVariable(v) ?? string.Empty;
                        arguments.PasswordType = PasswordType.Password;
                        if (string.IsNullOrEmpty(arguments.Password))
                        {
                            Console.WriteLine($"sshpass: -e options given but {v} environment variable is not set.");
                            errors++;
                            return;
                        }
                    }
                },
                {
                    "h|host=", "User and host to ssh to, formatted as user@host. Can be omitted if user@host is specified as first command parameter", v =>
                    {
                        string[]? userhost = GetUserHost(v);
                        
                        if (userhost == null)
                        {
                            errors++;
                        }
                        else
                        {
                            arguments.User = userhost[0];
                            arguments.Host = userhost[1];
                        }
                    }
                },
                {
                    "q|quiet", "Suppress password prompt on STDIN.", v =>
                    {
                        arguments.Quiet = !string.IsNullOrEmpty(v);
                    }
                },
                {
                    "v|verbose", "Be verbose about what you're doing.", v =>
                    {
                        arguments.Verbose = !string.IsNullOrEmpty(v);
                    }
                },
                {
                    "?|help", "Show help (this message).", v =>
                    {
                        arguments.ShowHelp = !string.IsNullOrEmpty(v);
                    }
                },
                {
                    "V|version", "Show version invormation.", v =>
                    {
                        arguments.ShowVersion = !string.IsNullOrEmpty(v);
                    }
                }
            };

            try
            {
                arguments.Commands = p.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Write("sshpass: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `sshpass.net --help` for more information.");
                return;
            }

            if (arguments.ShowHelp)
            {
                ShowHelp(p);
                return;
            }

            if (arguments.ShowVersion || arguments.Verbose)
            {
                Console.WriteLine("sshpass.net:");
                Console.WriteLine($"  Version {Assembly.GetEntryAssembly()?.GetName().Version}");
                Console.WriteLine("  (C) 2023 Computer Code Blue LLC");
                Console.WriteLine("Based on sshpass");
                Console.WriteLine("  (C) 2006-2011 Lingnu Open Source Consulting Ltd.");
                Console.WriteLine("  (C) 2015-2016, 2021-2022 Shachar Shemesh");
                Console.WriteLine("This program is free software, and can be distributed under the terms of the GPL.");
                Console.WriteLine("See the LICENSE file for more information.");
                if (arguments.ShowVersion) return;
            }

            if (errors == 0)
            {
                if (arguments.Commands.Count > 0)
                {
                    if (string.IsNullOrEmpty(arguments.User))
                    {
                        var userhost = GetUserHost(arguments.Commands.FirstOrDefault());
                        if (userhost != null)
                        {
                            arguments.User = userhost[0];
                            arguments.Host = userhost[1];
                            arguments.Commands.RemoveAt(0);
                        }
                    }
                    RunProgram(arguments);
                }
                else
                {
                    Console.WriteLine("sshpass.net: No commands were passed. Connection aborted.");
                }
            }
            return;
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: sshpass.net [OPTIONS]+ command parameters");
            Console.WriteLine("Pass a password to ssh for automation.");
            Console.WriteLine("Basic usage: sshpass.net user@host command. Password is accepted via STDIN.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
            Console.WriteLine("At most one of -f, -k, -p, or -e should be used.");
        }

        static string[]? GetUserHost(string? host)
        {
            if (host != null)
            {
                string[] userhost = host.Split('@');
                if (userhost.Length != 2)
                {
                    Console.WriteLine($"sshpass.net: {host} is not the correct format. Host should be formatted as user@host.");
                    return null;
                }
                return userhost;
            }
            else
            {
                Console.WriteLine($"sshpass.net: No host was passed. Host should be formatted as user@host.");
                return null;
            }
        }

        static void RunProgram(Arguments arguments)
        {
            List<AuthenticationMethod> methods = new List<AuthenticationMethod>();

            switch (arguments.PasswordType)
            {
                case PasswordType.Stdin:
                    if (!arguments.Quiet) Console.Write($"{arguments.User}@{arguments.Host}'s password: ");
                    arguments.Password = Console.ReadLine() ?? string.Empty;
                    methods.Add(new PasswordAuthenticationMethod(arguments.User, arguments.Password));
                    if (arguments.Verbose) Console.WriteLine("Password authentication type added from stdin.");
                    break;
                case PasswordType.Key:
                    if (string.IsNullOrWhiteSpace(arguments.FileName))
                    {
                        arguments.FileName = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.ssh/id_rsa";
                    }
                    try
                    {
                        var keyFile = new PrivateKeyFile(arguments.FileName);
                        methods.Add(new PrivateKeyAuthenticationMethod(arguments.User, keyFile));
                        if (arguments.Verbose) Console.WriteLine("Key authentication type added.");
                    }
                    catch (Exception ex)
                    {
                        Console.Write("sshpass: ");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Try `sshpass.net --help` for more information.");
                        return;
                    }
                    break;
                case PasswordType.Password:
                    methods.Add(new PasswordAuthenticationMethod(arguments.User, arguments.Password));
                    if (arguments.Verbose) Console.WriteLine("Password authentication type added from envvar or command line.");
                    break;
                case PasswordType.File:
                    try
                    {
                        string[] lines = File.ReadAllLines(arguments.FileName);
                        if (lines.Length > 0)
                        {
                            arguments.Password = lines[0];
                            methods.Add(new PasswordAuthenticationMethod(arguments.User, arguments.Password));
                            if (arguments.Verbose) Console.WriteLine("Password authentication type added from file.");
                        }
                        else
                        {
                            Console.WriteLine($"sshpass.net: File {arguments.FileName} had no data.");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Write("sshpass: ");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Try `sshpass.net --help` for more information.");
                        return;
                    }
                    break;
            }

            try
            {
                var connectionInfo = new ConnectionInfo(arguments.Host, arguments.User, methods.ToArray());

                // Mono.Options splits all the remaining entries, so we put them back together to run a single command.
                string command = string.Empty;
                for (int i = 0; i < arguments.Commands.Count; i++)
                {
                    command += arguments.Commands[i] + (i < arguments.Commands.Count - 1 ? " " : string.Empty);
                }

                // If the user enclosed the command in single quotes, remove them.
                if (command[0] == '\'' && command[command.Length - 1] == '\'')
                {
                    command = command.Substring(1, command.Length - 2);
                }

                using (var client = new SshClient(connectionInfo))
                {
                    client.Connect();
                    var result = client.RunCommand(command);
                    Console.Write(result.Result);

                }
            }
            catch (Exception ex)
            {
                Console.Write("sshpass.net: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try `sshpass --help` for more information.");
                return;
            }
        }
    }
}

