using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Emzi0767.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace _929Bot
{
    class Bot929
    {
        public IConfiguration Configuration { get; }

        static InteractivityExtension InteractivityService;
        static CommandsNextExtension CommandsNextService;

        static IReadOnlyCollection<DiscordMember> members;

        public static DiscordClient client;

        static MongoClient mongo;
        static IMongoDatabase database;

        public static IMongoCollection<nine29er> nine29ers;
        public static IMongoCollection<object> pastlist;

        static bool ready = false;

        static string first = null;

        static List<pastuser> did929 = new List<pastuser>();

        static DiscordChannel channel929;

        static void Main(string[] args)
        {
            RunBotAsync();
            Console.ReadLine();
        }

        static int[] triggertime = { 9, 29 };

        private static void Check929()
        {
            while (true)
            {
                if (!((DateTimeNowEST().Hour == triggertime[0] || DateTimeNowEST().Hour == triggertime[0] + 12) && DateTimeNowEST().Minute == triggertime[1]))
                {
                    Thread.Sleep(100);
                    continue;
                }

                LogMessage("It is 929!");

                while ((DateTimeNowEST().Hour == triggertime[0] || DateTimeNowEST().Hour == triggertime[0] + 12) && DateTimeNowEST().Minute == triggertime[1])
                    Thread.Sleep(100);

                LogMessage("It is no longer 929!");

                savePastlist(did929);
                did929.Clear();

                if(first != null)
                    channel929.SendMessageAsync(SanitizeString(first) + " was first!");

                first = null;

                Thread.Sleep(TimeSpan.FromSeconds(11*60*60 + 58*60 + 30));
            }
        }

        private static void savePastlist(List<pastuser> pl)
        {
            pastlist.DeleteMany("{}");
            if(pl.Count > 0)
                pastlist.InsertMany(pl);
        }

        private static async void RunBotAsync()
        {
            var Config = new ConfigurationBuilder()
                .AddUserSecrets<Bot929>()
                .Build();

            mongo = new MongoClient(Config["MongoConnStr"]);
            database = mongo.GetDatabase("bot929");

            client = new DiscordClient(new DiscordConfiguration() { 
                Token = Config["DiscordBotToken"], 
                TokenType = TokenType.Bot, 
                AutoReconnect = true, 
                MinimumLogLevel = LogLevel.Information,
                Intents = DiscordIntents.AllUnprivileged
            });

            var depco = new ServiceCollection();

            // commandsnext config and the commandsnext service itself
            var cncfg = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { "!" },
                EnableDms = false,
                EnableMentionPrefix = true,
                CaseSensitive = false,
                Services = depco.BuildServiceProvider(true),
                IgnoreExtraArguments = false,
                UseDefaultCommandHandler = true,
            };
            CommandsNextService = client.UseCommandsNext(cncfg);
            CommandsNextService.CommandErrored += CommandsNextService_CommandErrored;
            CommandsNextService.CommandExecuted += CommandsNextService_CommandExecuted;
            CommandsNextService.RegisterCommands(typeof(Commands));

            var icfg = new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromDays(1),
                ResponseBehavior = InteractionResponseBehavior.Ack,
                ResponseMessage = "That's not a valid button"
            };

            InteractivityService = client.UseInteractivity(icfg);

            client.Ready += Client_Ready;
            client.MessageCreated += Client_MessageCreated;

            await client.ConnectAsync();
        }

        private static Task CommandsNextService_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            return Task.CompletedTask;
        }

        private static Task CommandsNextService_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            return Task.CompletedTask;
        }

        private static async Task Client_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            string msg = e.Message.Content.ToLower();
            
            if(msg.Contains("929") && did929.FindAll(user => user.id == e.Author.Id).Count < 1)
            {
                var time = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(e.Message.CreationTimestamp.DateTime, TimeZoneInfo.Local.Id, "EST5EDT");

                if ((time.Hour == triggertime[0] || time.Hour == triggertime[0] + 12) && time.Minute == triggertime[1])
                {
                    var member = await GetMemberById(e.Guild, e.Author.Id);
                    string name = (member.Nickname == null) ? member.Username : member.Nickname;

                    var pl = pastlist.Find($"{{_id: {e.Author.Id}}}");
                    var find = nine29ers.Find($"{{_id: {e.Author.Id}}}");

                    nine29er user;
                    if (find.CountDocuments() < 1)
                    {
                        user = new nine29er(e.Author.Id);
                    }
                    else
                        user = find.First();

                    int currentstreak;
                    if (pl.CountDocuments() == 1)
                        currentstreak = user.currentstreak + 1;
                    else
                        currentstreak = 1;

                    UpdateDefinition<nine29er> update = Builders<nine29er>.Update.Set("currentstreak", currentstreak);

                    if (currentstreak > user.maxstreak)
                        update = update.Set("maxstreak", currentstreak);

                    if (first == null)
                    {
                        first = name;
                        update = update.Set("points", user.points + (1.5 * (1 + (int)(user.currentstreak / 5)))); 
                    }
                    else
                    {
                        update = update.Set("points", user.points + (1 + (int)(user.currentstreak / 5)));
                    }

                    update = update.Set("count", user.count + 1);

                    try
                    {
                        nine29ers.UpdateOne(n29er => (n29er.id == e.Author.Id), update, new UpdateOptions { IsUpsert = true });
                    } 
                    catch(Exception exc)
                    {
                        LogMessage(exc.Message + "\n" + exc.StackTrace, LogLevel.Error);
                    }

                    did929.Add(new pastuser(e.Author.Id));
                    LogMessage($"{name} did 929!");
                }
            } 
        }

        private static string SanitizeString(string input)
        {
            return input.Replace("|", "\\|");
        }

        public static async Task<DiscordMember> GetMemberById(DiscordGuild guild, ulong id)
        {
            try
            {
                return (from user in members where user.Id == id select user).First();
            } 
            catch(Exception e)
            {
                return await guild.GetMemberAsync(id);
            }
        }
        
        private static async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            if (!ready)
            {
                nine29ers = database.GetCollection<nine29er>("nine29ers");
                pastlist = database.GetCollection<object>("pastlist");
                members = await (await client.GetGuildAsync(377637608848883723)).GetAllMembersAsync();
                channel929 = await client.GetChannelAsync(619704668590833692);
                new Thread(Check929).Start();
                LogMessage("Client is ready!");
                ready = true;
            }
            else
            {
                LogMessage("Client has reconnected.");
            }
        }

        public static void LogMessage(string message, LogLevel logLevel = LogLevel.Information)
        {
            client.Logger.Log(logLevel, message);
        }

        private static DateTime DateTimeNowEST()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, TimeZoneInfo.Local.Id, "EST5EDT");
            } 
            catch(Exception exc)
            {
                return DateTime.Now;
            }
        }
    }

    class LeaderboardMessage
    {
        public List<nine29er> nine29ers;
        public DiscordMessage message;
        public int position;

        public LeaderboardMessage(DiscordMessage message, List<nine29er> nine29ers)
        {
            this.nine29ers = nine29ers;
            this.message = message;
            this.position = 0;
        }
    }

    class nine29er
    {
        [BsonElement("_id")]
        public ulong id;

        [BsonElement("currentstreak")]
        public int currentstreak;

        [BsonElement("points")]
        public double points;

        [BsonElement("maxstreak")]
        public int maxstreak;

        [BsonElement("count")]
        public int count;

        public nine29er(ulong id) {
            this.id = id;
        }
    }

    class pastuser
    {
        [BsonElement("_id")]
        public ulong id;

        public pastuser(ulong id)
        {
            this.id = id;
        }
    }
}
