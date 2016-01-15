using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using FlowTomator.Common;
using Renci.SshNet;

namespace FlowTomator.Sftp
{
    [Node(nameof(SshCommandTask), "Ssh", "Dumps the specified data into the specified file")]
    public class SshCommandTask : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return host;
                yield return port;
                yield return user;
                yield return password;
                yield return command;
            }
        }

        private Variable<string> host = new Variable<string>("Host", "127.0.0.1", "The host on which to run specified command");
        private Variable<ushort> port = new Variable<ushort>("Port", 22, "The port used to connect to the specified host");
        private Variable<string> user = new Variable<string>("User", "root", "The user used to log in the specified host");
        private Variable<string> password = new Variable<string>("Password", "", "The password used to authenticate the specified user");
        private Variable<string> command = new Variable<string>("Command", "pwd", "The command to be executed on the remote host");

        public override NodeResult Run()
        {
            if (string.IsNullOrEmpty(host.Value) || string.IsNullOrEmpty(user.Value) || string.IsNullOrEmpty(command.Value))
                return NodeResult.Fail;

            try
            {
                SshClient sshClient = new SshClient(host.Value, port.Value, user.Value, password.Value);
                sshClient.Connect();

                SshCommand sshCommand = sshClient.RunCommand(command.Value);
                Log.Info(sshCommand.CommandText);

                if (sshCommand.ExitStatus != 0)
                    Log.Warning(sshCommand.Result);
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