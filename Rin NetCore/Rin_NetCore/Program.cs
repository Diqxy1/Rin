using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Rin_NetCore.Resources.Settings;
using Rin_NetCore.Resources.Datatypes;

namespace Rin_NetCore
{
    class Program
    {
        public DiscordSocketClient Client;
        public CommandService Commands;

        static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            string JSON = "";
            string SettingsLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\config", @"Data\Settings.json");
            using (var Stream = new FileStream(SettingsLocation, FileMode.Open, FileAccess.Read))
            using (var ReadSettings = new StreamReader(Stream))
            {
                JSON = ReadSettings.ReadToEnd();
            }

            Setting Settings = JsonConvert.DeserializeObject<Setting>(JSON);
            ESettings.Banned = Settings.banned;
            ESettings.Log = Settings.log;
            ESettings.Owner = Settings.owner;
            ESettings.Token = Settings.token;
            ESettings.Version = Settings.version;

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            //help help HELP heLp
            Commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async,
                LogLevel = LogSeverity.Debug
            });

            Client.MessageReceived += Client_MessageReceived;
            await Commands.AddModulesAsync(Assembly.GetEntryAssembly());

            Client.Ready += Client_Ready;
            Client.Log += Client_Log;

            await Client.LoginAsync(TokenType.Bot, ESettings.Token);
            await Client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task Client_Log(LogMessage Message)
        {
            Console.WriteLine($"{DateTime.Now} at {Message.Source}] {Message.Message}");
            try
            {
                SocketGuild Guild = Client.Guilds.Where(x => x.Id == ESettings.Log[0]).FirstOrDefault();
                SocketTextChannel Channel = Guild.Channels.Where(x => x.Id == ESettings.Log[1]).FirstOrDefault() as SocketTextChannel;
                await Channel.SendMessageAsync($"{DateTime.Now} at {Message.Source}] {Message.Message}");
            }
            catch { }
        }

        private async Task Client_Ready()
        {
            await Client.SetGameAsync("Kiritsugu esta me atualizando <3");
        }

        private async Task Client_MessageReceived(SocketMessage MessageParam)
        {
            var Message = MessageParam as SocketUserMessage;
            var Context = new SocketCommandContext(Client, Message);

            if (Context.Message == null || Context.Message.Content == "") return;
            if (Context.User.IsBot) return;

            int ArgPos = 0;
            if (!(Message.HasStringPrefix("$", ref ArgPos) || Message.HasMentionPrefix(Client.CurrentUser, ref ArgPos))) return;

            var Result = await Commands.ExecuteAsync(Context, ArgPos);
            if (!Result.IsSuccess)
                Console.WriteLine($"{DateTime.Now} at Commands] Algo deu errado com a execução de um comando. Text: {Context.Message.Content} | Error: {Result.ErrorReason}");
        }
    }
    
}
