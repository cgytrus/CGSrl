using System.Globalization;
using System.Net;

using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking;

using NBrigadier;
using NBrigadier.Arguments;
using NBrigadier.Builder;
using NBrigadier.Context;
using NBrigadier.Tree;

using PER.Util;

namespace Cgsrl.Server.Networking;

public class Commands {
    public CommandDispatcher<PlayerObject?> dispatcher { get; } = new();

    private readonly Dictionary<CommandNode<PlayerObject?>, string> _descriptions = new();

    private readonly GameServer _server;
    private readonly SyncedLevel _level;

    public Commands(GameServer server, SyncedLevel level) {
        _server = server;
        _level = level;

        _descriptions.Add(dispatcher.Register(LiteralArgumentBuilder<PlayerObject?>.Literal("help")
            .Then(RequiredArgumentBuilder<PlayerObject?, string>.Argument("command", StringArgumentType.Word())
                .Executes(context => {
                    HelpCommand(StringArgumentType.GetString(context, "command"), context);
                    return 1;
                }))
            .Executes(context => {
                HelpCommand(context);
                return 1;
            })), "Prints this message.");

        _descriptions.Add(dispatcher.Register(LiteralArgumentBuilder<PlayerObject?>.Literal("stop")
            .Executes(context => {
                if(!CheckServerPlayer(context))
                    return 1;
                Core.engine.renderer.Close();
                return 1;
            })), "Stops the server.");

        _descriptions.Add(dispatcher.Register(LiteralArgumentBuilder<PlayerObject?>.Literal("info")
            .Executes(context => {
                if(context.Source is null)
                    return 1;

                string commandCount = dispatcher.Root.Children.Count.ToString(CultureInfo.InvariantCulture);
                string playerCount = _level.objects.Values.Count(o => o is PlayerObject)
                    .ToString(CultureInfo.InvariantCulture);
                string uptime = _server.uptime.ToString("%d'd '%h'h '%m'm '%s's'", CultureInfo.InvariantCulture);
                string ping = ((long)TimeSpan.FromSeconds(context.Source?.ping ?? 0f).TotalMilliseconds)
                    .ToString(CultureInfo.InvariantCulture);

                _server.SendChatMessage(null, context.Source, $"\fbINFO:\f\0 v{Core.version}");
                _server.SendChatMessage(null, context.Source, $"- \fb{commandCount}\f\0 commands available");
                _server.SendChatMessage(null, context.Source, $"- \fb{playerCount}\f\0 players online");
                _server.SendChatMessage(null, context.Source, $"- Running for \fb{uptime}\f\0");
                _server.SendChatMessage(null, context.Source, $"- Pinging \fb{ping}ms\f\0 to you");
                return 1;
            })), "Prints some info about the server.");

        _descriptions.Add(dispatcher.Register(LiteralArgumentBuilder<PlayerObject?>.Literal("save")
            .Executes(context => {
                if(!CheckServerPlayer(context))
                    return 1;
                if(Core.engine.game is Game game)
                    game.SaveLevel();
                return 1;
            })), "Saves the level.");

        _descriptions.Add(dispatcher.Register(LiteralArgumentBuilder<PlayerObject?>.Literal("players")
            .Then(RequiredArgumentBuilder<PlayerObject?, bool>.Argument("printIp", BoolArgumentType.Bool())
                .Executes(context => {
                    PlayersCommand(BoolArgumentType.GetBool(context, "printIp"), context);
                    return 1;
                }))
            .Executes(context => {
                PlayersCommand(false, context);
                return 1;
            })), "Prints online players.");

        _descriptions.Add(dispatcher.Register(LiteralArgumentBuilder<PlayerObject?>.Literal("locate")
            .Then(RequiredArgumentBuilder<PlayerObject?, string>.Argument("username", StringArgumentType.String())
                .Executes(context => {
                    LocateCommand(context, StringArgumentType.GetString(context, "username"));
                    return 1;
                }))
            .Executes(context => {
                if(context.Source is not null)
                    LocateCommand(context, context.Source);
                return 1;
            })), "Prints the location of a player.");

        _descriptions.Add(dispatcher.Register(LiteralArgumentBuilder<PlayerObject?>.Literal("say")
            .Then(RequiredArgumentBuilder<PlayerObject?, string>.Argument("message", StringArgumentType.String())
                .Executes(context => {
                    _server.SendChatMessage(context.Source, null, StringArgumentType.GetString(context, "message"));
                    return 1;
                }))
            .Then(RequiredArgumentBuilder<PlayerObject?, string>.Argument("username", StringArgumentType.String())
                .Then(RequiredArgumentBuilder<PlayerObject?, string>.Argument("message", StringArgumentType.String())
                    .Executes(context => {
                        SayCommand(context, StringArgumentType.GetString(context, "username"),
                            StringArgumentType.GetString(context, "message"));
                        return 1;
                    })))), "Sends a message in the chat.");

        _descriptions.Add(dispatcher.Register(LiteralArgumentBuilder<PlayerObject?>.Literal("teleport")
            .Then(RequiredArgumentBuilder<PlayerObject?, string>.Argument("username", StringArgumentType.String())
                .Executes(context => {
                    TeleportCommand(context, StringArgumentType.GetString(context, "username"));
                    return 1;
                }))
            .Then(RequiredArgumentBuilder<PlayerObject?, int>.Argument("x", IntegerArgumentType.Integer())
                .Then(RequiredArgumentBuilder<PlayerObject?, int>.Argument("y", IntegerArgumentType.Integer())
                    .Executes(context => {
                        TeleportCommand(context,
                            new Vector2Int(IntegerArgumentType.GetInteger(context, "x"),
                                IntegerArgumentType.GetInteger(context, "y")));
                        return 1;
                    })))
        ), "Teleports you to the specified position.");

        _descriptions.Add(dispatcher.Register(LiteralArgumentBuilder<PlayerObject?>.Literal("kick")
            .Then(RequiredArgumentBuilder<PlayerObject?, string>.Argument("username", StringArgumentType.String())
                .Then(RequiredArgumentBuilder<PlayerObject?, string>.Argument("reason", StringArgumentType.String())
                    .Executes(context => {
                        KickCommand(context, StringArgumentType.GetString(context, "username"),
                            StringArgumentType.GetString(context, "reason"));
                        return 1;
                    }))
                .Executes(context => {
                    KickCommand(context, StringArgumentType.GetString(context, "username"));
                    return 1;
                }))), "Kicks the specified player.");
    }

    private bool CheckServerPlayer(CommandContext<PlayerObject?> context) {
        // server console or local player
        if(context.Source?.connection is null ||
            context.Source.connection.RemoteEndPoint.Address.Equals(IPAddress.Loopback))
            return true;
        _server.SendChatMessage(null, context.Source, GameServer.ErrorMessage("Not enough permissions"));
        return false;
    }

    private void HelpCommand(CommandContext<PlayerObject?> context) {
        IDictionary<CommandNode<PlayerObject?>, string> usages = dispatcher.GetSmartUsage(dispatcher.Root, context.Source);

        foreach((CommandNode<PlayerObject?> node, string usage) in usages)
            _server.SendChatMessage(null, context.Source, _descriptions.TryGetValue(node, out string? description) ?
                $"{node.Name} - {description} Usage: {usage}" :
                $"{node.Name} {usage}");
    }

    private void HelpCommand(string command, CommandContext<PlayerObject?> context) {
        CommandNode<PlayerObject?> node = dispatcher.FindNode(new string[] { command });
        IDictionary<CommandNode<PlayerObject?>, string> usages = dispatcher.GetSmartUsage(node, context.Source);

        foreach((CommandNode<PlayerObject?> _, string usage) in usages)
            _server.SendChatMessage(null, context.Source, _descriptions.TryGetValue(node, out string? description) ?
                $"{command} - {description} Usage: {usage}" : $"{command} {usage}");
    }

    private void PlayersCommand(bool printIp, CommandContext<PlayerObject?> context) {
        if(!CheckServerPlayer(context))
            return;

        foreach(PlayerObject player in _level.objects.Values.OfType<PlayerObject>()) {
            string displayName = $"{player.username} ";
            string username = printIp && player.connection is not null ? $"({player.connection.RemoteEndPoint}) " : "";

            _server.SendChatMessage(null, context.Source, $"{displayName}{username}");
        }
    }

    private void LocateCommand(CommandContext<PlayerObject?> context, string username) {
        PlayerObject? player = _level.objects.Values.OfType<PlayerObject>()
            .FirstOrDefault(ply => ply.username == username);
        if(player is null) {
            _server.SendChatMessage(null, context.Source,
                $"Invalid argument \fb0\f\0 (player \fb{username}\f\0 not found)");
            return;
        }

        LocateCommand(context, player);
    }

    private void LocateCommand(CommandContext<PlayerObject?> context, PlayerObject toLocate) {
        _server.SendChatMessage(null, context.Source, $"Position: {toLocate.position.x}, {toLocate.position.y}");
    }

    private void SayCommand(CommandContext<PlayerObject?> context, string username, string message) {
        PlayerObject? player = _level.objects.Values.OfType<PlayerObject>()
            .FirstOrDefault(ply => ply.username == username);
        if(player is null) {
            _server.SendChatMessage(null, context.Source,
                $"Invalid argument \fb0\f\0 (player \fb{username}\f\0 not found)");
            return;
        }

        _server.SendChatMessage(context.Source, player, message);
    }

    private void TeleportCommand(CommandContext<PlayerObject?> context, string username) {
        if(context.Source is null) {
            _server.SendChatMessage(null, null, "Teleport can only be executed on the client");
            return;
        }

        PlayerObject? player = _level.objects.Values.OfType<PlayerObject>()
            .FirstOrDefault(ply => ply.username == username);
        if(player is null) {
            _server.SendChatMessage(null, context.Source,
                $"Invalid argument \fb0\f\0 (player \fb{username}\f\0 not found)");
            return;
        }

        context.Source.position = player.position;
        _server.SendChatMessage(null, context.Source,
            $"Teleported \fb{context.Source.displayName}\f\0 to \fb{player.displayName}");
    }

    private void TeleportCommand(CommandContext<PlayerObject?> context, Vector2Int position) {
        if(context.Source is null) {
            _server.SendChatMessage(null, null, "Teleport can only be executed on the client");
            return;
        }

        context.Source.position = position;
        _server.SendChatMessage(null, context.Source,
            $"Teleported \fb{context.Source.displayName}\f\0 to \fb{position.x}, {position.y}");
    }

    private void KickCommand(CommandContext<PlayerObject?> context, string username,
        string reason = "Kicked by server owner") {
        if(!CheckServerPlayer(context))
            return;

        PlayerObject? player = _level.objects.Values.OfType<PlayerObject>()
            .FirstOrDefault(ply => ply.username == username);
        if(player?.connection is null) {
            _server.SendChatMessage(null, context.Source,
                $"Invalid argument \fb0\f\0 (player \fb{username}\f\0 not found)");
            return;
        }

        player.connection.Disconnect(reason);
        _server.SendChatMessage(null, context.Source, $"Kicked \fb{player.displayName}");
    }
}
