# sshpass.net

`sshpass.net` is a dotnet tool that allows you to automate commands sent via ssh with a password.

Contents
========

 * [Why?](#why)
 * [Installation](#installation)
 * [Usage](#usage)
 * [Security Considerations](#security)
 * [Examples](#examples)

### Why?

I needed a tool to automate sending a command to a remote server via ssh, and public key authentication, while better (see [Security Considerations](#security) below), was an inconvenient option. The original `sshpass` is a C program written for POSIX operating systems and doesn't work for Windows and I needed this specifically for a Windows client PC. As it turns out, using the SSH.NET and Mono.Options libraries made writing a dotnet version relatively simple.

### Installation
---

#### Method 1: [`dotnet tool`](https://www.nuget.org/packages/SSH.NET)

```cmd
dotnet tool install sshpass.net -g
```

#### Method 2: Install From Source

```cmd
git clone https://github.com/computercodeblue/sshpass.net.git
cd sshpass.net
dotnet pack
dotnet tool install --global --add-source ./sshpass.net/nupkg sshpass.net
```

### Usage
---

Note that `sshpass.net` is not designed to be interactive, it is designed to pass a password to a remote ssh server as part of a script. That said, you can simply run `sshpass-net user@host [remote command]` and get a password prompt, like you would if you used `ssh`. Public key authentication is also supported, like with `ssh`.

```shell
Usage: sshpass-net [OPTIONS]+ command parameters
Pass a password to ssh for automation.
Basic usage: sshpass.net user@host command. Password is accepted via STDIN.

Options:
  -f, --filename=VALUE       Take password to use from file.
  -k, --key[=VALUE]          Use RSA private key file. Defaults to current user'
                               s id_rsa file.
  -p, --password=VALUE       Provide password as argument (security unwise).
  -e, --envvar[=VALUE]       Password is passed as env-var if given, "SSHPASS"
                               otherwise.
  -h, --host=VALUE           User and host to ssh to, formatted as user@host.
                               Can be omitted if user@host is specified as
                               first command parameter
  -q, --quiet                Suppress password prompt on STDIN.
  -v, --verbose              Be verbose about what you're doing.
  -?, --help                 Show help (this message).
  -V, --version              Show version invormation.
At most one of -f, -k, -p, or -e should be used.
```

### Security
---

Users should realize that there are good reasons for ssh insisting on getting passwords interactively. It is close to impossible to securely store the password, and users of sshpass.net should consider whether public key authentication used by ssh or sshpass.net provides the same end-user experience, while involving less hassle and being more secure.

The `-p` option should be considered the least secure of all of sshpass's options. All system users can see the password in the command line with a simple "ps" command. Users of sshpass.net are encouraged to use one of the other password passing techniques, which are all more secure.

### Examples
---

#### Execute `uptime` Via Password Stored in Text File

```cmd
sshpass-net -f pw.txt user@host.example.com uptime
``` 

sshpass.net can also read from STDIN. The `-q` switch it optional and will suppress the password prompt. You can use either a redirect or pipe.

```cmd
sshpass-net -q user@host.example.com uptime < pw.txt
```

```cmd
cat pw.txt | sshpass.net -q user@host.example.com uptime
```

#### Execute `uptime` Via Password Stored in Environment Variable

```cmd
set SSHPASS=password
sshpass-net -e user@host.example.com uptime
``` 

#### Execute `uptime` Via Password Stored in a Key File

```cmd
sshpass-net -k ./private.key user@host.example.com uptime
``` 
