namespace sshpass.net
{
    public class Arguments
    {
        public PasswordType PasswordType { get; set; } = PasswordType.Stdin;
        public string Password { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public bool Quiet { get; set; } = false;
        public bool Verbose { get; set; } = false;
        public bool ShowHelp { get; set; } = false;
        public bool ShowVersion { get; set; } = false;
        public List<string> Commands { get; set; } = new List<string>();
    }

    public enum PasswordType
    {
        Stdin,
        Key,
        File,
        Password
    }
}
