using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using FlowTomator.Common;
using Renci.SshNet;

namespace FlowTomator.Ssh
{
    [Node("Ssh command", "Ssh", "Dumps the specified data into the specified file")]
    public class SshCommand : Task
    {
        public enum AuthenticationType
        {
            Password,
            Certificate
        }

        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return host;
                yield return port;
                yield return authentication;
                if (authentication.Value == AuthenticationType.Password)
                {
                    yield return user;
                    yield return password;
                }
                else if (authentication.Value == AuthenticationType.Certificate)
                { 
                    yield return certificate;
                    yield return passphrase;
                }
                yield return command;
            }
        }

        private Variable<string> host = new Variable<string>("Host", "127.0.0.1", "The host on which to run specified command");
        private Variable<ushort> port = new Variable<ushort>("Port", 22, "The port used to connect to the specified host");
        private Variable<string> user = new Variable<string>("User", "root", "The user used to log in the specified host");
        private Variable<string> password = new Variable<string>("Password", "", "The password used to authenticate the specified user");
        private Variable<string> command = new Variable<string>("Command", "pwd", "The command to be executed on the remote host");
        private Variable<AuthenticationType> authentication = new Variable<AuthenticationType>("Authentication", AuthenticationType.Password);
        private Variable<FileInfo> certificate = new Variable<FileInfo>("SSL Certificate", null, "SSL Certificate to be used for the connection.");
        private Variable<string> passphrase = new Variable<string>("Passphrase","","The certificate passphrase");

        public override NodeResult Run()
        {
            if (authentication.Value == AuthenticationType.Password && (string.IsNullOrEmpty(host.Value) || string.IsNullOrEmpty(user.Value) || string.IsNullOrEmpty(command.Value)))
                return NodeResult.Fail;
            if(authentication.Value == AuthenticationType.Certificate && (string.IsNullOrEmpty(host.Value) || string.IsNullOrEmpty(command.Value) || certificate.Value == null))
                return NodeResult.Fail;

            try
            {
                SshClient sshClient;
                if (authentication.Value == AuthenticationType.Certificate && string.IsNullOrEmpty(passphrase.Value))
                    sshClient = new SshClient(host.Value, port.Value, user.Value, new PrivateKeyFile(certificate.Value.FullName));
                else if (authentication.Value == AuthenticationType.Certificate && !string.IsNullOrEmpty(passphrase.Value))
                    sshClient = new SshClient(host.Value, port.Value, user.Value, new PrivateKeyFile(certificate.Value.FullName, passphrase.Value));
                else
                    sshClient = new SshClient(host.Value, port.Value, user.Value, password.Value);

                sshClient.Connect();

                Renci.SshNet.SshCommand sshCommand = sshClient.RunCommand(command.Value);
                Log.Info(sshCommand.CommandText);

                if (!string.IsNullOrWhiteSpace(sshCommand.Result))
                    Log.Warning(sshCommand.Result);
                if (!string.IsNullOrWhiteSpace(sshCommand.Error))
                    Log.Error(sshCommand.Error);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return NodeResult.Fail;
            }

            return NodeResult.Success;
        }
    }
}