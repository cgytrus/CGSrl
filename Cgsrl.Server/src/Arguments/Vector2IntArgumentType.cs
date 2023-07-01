using System.Collections.ObjectModel;
using System.Net.Mime;

using Cgsrl.Shared.Environment;

using NBrigadier;
using NBrigadier.Arguments;
using NBrigadier.CommandSuggestion;
using NBrigadier.Context;
using NBrigadier.Exceptions;

using PER.Util;

using StringReader = NBrigadier.StringReader;

namespace Cgsrl.Server.Arguments;

public class Vector2IntArgumentType : IArgumentType<IPosArgument> {
    private static readonly SimpleCommandExceptionType incompleteException =
        new(new LiteralMessage("argument.pos2d.incomplete"));

    public ICollection<string> Examples => examples;

    private static readonly ICollection<string> examples = new string[] { "0 0", "~ ~", "3 -5", "~1 ~-2" };

    public static Vector2Int GetVector2Int(CommandContext<PlayerObject?> context, string name) =>
        context.GetArgument<IPosArgument>(name).ToAbsolutePos(context.Source);

    public IPosArgument Parse(StringReader reader) {
        int i = reader.Cursor;
        if(!reader.CanRead())
            throw incompleteException.CreateWithContext(reader);
        CoordinateArgument coordinateArgument = CoordinateArgument.Parse(reader);
        if(!reader.CanRead() || reader.Peek() != ' ') {
            reader.Cursor = i;
            throw incompleteException.CreateWithContext(reader);
        }
        reader.Skip();
        CoordinateArgument coordinateArgument2 = CoordinateArgument.Parse(reader);
        return new DefaultPosArgument(coordinateArgument, coordinateArgument2);
    }

    // TODO
    public Func<Suggestions> ListSuggestions<TS>(CommandContext<TS> context, SuggestionsBuilder builder) =>
        Suggestions.Empty();
}
