using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace _929Bot
{
    class Commands : BaseCommandModule
    {
        static Dictionary<ulong, LeaderboardMessage> leaderboards = new Dictionary<ulong, LeaderboardMessage>();

        [Command("leaderboard")]
        [Description("Displays the leaderboard of 929s.")]
        public async Task Leaderboard(CommandContext ctx)
        {
            string ldbmsg = "";
            var input = ctx.Client.GetInteractivity();
            var users = Bot929.nine29ers.Find("{}").Sort("{ points: -1 }").ToList();

            int position = 0;
            int stop = (users.Count >= 10) ? 10 : users.Count;
            for (int i = 0; i < stop; i++)
            {
                try
                {
                    DiscordMember user = await Bot929.GetMemberById(ctx.Guild, users[i].id);
                    string name = (user.Nickname == null) ? user.Username : user.Nickname;
                    ldbmsg += $"{i + 1}. {SanitizeString(name)}: {users[i].points}\n";
                }
                catch (Exception exc)
                {
                    users.RemoveAt(i);
                    i--;
                }
            }

            var builder = new DiscordMessageBuilder();
            var embed = new DiscordEmbedBuilder() { Title = "Leaderboard", Color = DiscordColor.DarkRed, Description = ldbmsg };
            builder.WithEmbed(embed);

            var btn1 = new DiscordButtonComponent(ButtonStyle.Primary, "prev", "", emoji: new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":arrow_left:")));
            var btn2 = new DiscordButtonComponent(ButtonStyle.Primary, "next", "", emoji: new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":arrow_right:")));

            builder.AddComponents(btn1, btn2);

            var msg = await builder.SendAsync(ctx.Channel);

            while (true) 
            {
                var res = await input.WaitForButtonAsync(msg);

                if(res.TimedOut)
                {
                    builder = new DiscordMessageBuilder();
                    embed = new DiscordEmbedBuilder() { Title = "Leaderboard", Color = DiscordColor.DarkRed, Description = ldbmsg };
                    builder.WithEmbed(embed);

                    await msg.ModifyAsync(builder);
                    return;
                }

                await res.Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredMessageUpdate);

                if (res.Result.Id == "next")
                {
                    if (position + 10 < users.Count)
                        position += 10;
                }
                else if (res.Result.Id == "prev")
                {
                    if (position > 0)
                        position -= 10;
                }

                ldbmsg = "";
                stop = (users.Count >= position + 10) ? position + 10 : users.Count;
                for (int i = position; i < stop; i++)
                {
                    try
                    {
                        DiscordMember user = await Bot929.GetMemberById(ctx.Guild, users[i].id);
                        string name = (user.Nickname == null) ? user.Username : user.Nickname;
                        ldbmsg += $"{i + 1}. {SanitizeString(name)}: {users[i].points}\n";
                    }
                    catch (Exception exc)
                    {
                        Bot929.LogMessage("Exception: " + exc.Message, LogLevel.Error);
                        users.RemoveAt(i);
                        i--;
                    }
                }

                builder = new DiscordMessageBuilder();
                embed = new DiscordEmbedBuilder() { Title = "Leaderboard", Color = DiscordColor.DarkRed, Description = ldbmsg };
                builder.WithEmbed(embed);
                builder.AddComponents(btn1, btn2);

                await msg.ModifyAsync(builder);
            }
        }

        [Command("profile")]
        [Description("Displays your or another user's 929 profile.")]
        public async Task Profile(CommandContext ctx, DiscordMember member = null)
        {
            IFindFluent<nine29er, nine29er> find;
            if (member != null)
            {
                find = Bot929.nine29ers.Find($"{{_id: {ctx.Message.MentionedUsers[0].Id}}}");
            }
            else
            {
                member = ctx.Member;
                find = Bot929.nine29ers.Find($"{{_id: {ctx.Member.Id}}}");
            }


            if (find.CountDocuments() < 1)
            {
                await ctx.Channel.SendMessageAsync("You have not participated in a 9:29 yet!");
            }
            else
            {
                var user = find.First();

                string name = (member.Nickname == null) ? SanitizeString(member.Username) : SanitizeString(member.Nickname);
                var embed = new DiscordEmbedBuilder().WithTitle($"Profile for {name}");

                embed.AddField("Current Streak", user.currentstreak.ToString());
                embed.AddField("Longest Streak", user.maxstreak.ToString());
                embed.AddField("Total Points", user.points.ToString());
                embed.AddField("Total 929s", user.count.ToString());

                await ctx.Channel.SendMessageAsync(embed: embed.Build());
            }
        }

        private static string SanitizeString(string input)
        {
            return input.Replace("|", "\\|");
        }
    }
}
